import { createReadStream, existsSync, readFileSync, statSync } from "node:fs";
import { createServer } from "node:http";
import { createServer as createHttpsServer } from "node:https";
import { dirname, extname, join, relative, resolve, sep } from "node:path";
import { fileURLToPath } from "node:url";
import { randomUUID } from "node:crypto";
import { WebSocket, WebSocketServer } from "ws";

const ROOT = dirname(fileURLToPath(import.meta.url));
const ROOM_PATTERN = /^[a-z0-9][a-z0-9-]{0,23}$/i;
const PART_TYPES = new Set(["tank", "valve", "pipe", "pump"]);
const MAX_CLIENTS = 24;
const MAX_ROOMS = 64;
const MAX_OBJECTS = 250;
const MAX_MESSAGE_BYTES = 16 * 1024;
const SITE_ROOT = resolve(ROOT, "..");
const CONTENT_TYPES = new Map([
  [".css", "text/css; charset=utf-8"],
  [".html", "text/html; charset=utf-8"],
  [".js", "text/javascript; charset=utf-8"],
  [".json", "application/json; charset=utf-8"],
  [".md", "text/markdown; charset=utf-8"],
  [".png", "image/png"],
  [".svg", "image/svg+xml"],
]);

export function validRoom(value) {
  return typeof value === "string" && ROOM_PATTERN.test(value);
}

function isVector(value, limit, exactLength = 3) {
  return Array.isArray(value)
    && value.length === exactLength
    && value.every((coordinate) => Number.isFinite(coordinate) && Math.abs(coordinate) <= limit);
}

function cleanName(value) {
  if (typeof value !== "string") return "Guest";
  const name = value.replace(/[\u0000-\u001f<>]/g, "").trim().slice(0, 20);
  return name || "Guest";
}

function clientProfile(peer) {
  return { id: peer.id, name: peer.name, mode: peer.mode, cursor: peer.cursor };
}

export function createRelay() {
  const rooms = new Map();
  const clients = new Set();
  const server = createServer((request, response) => serveStatic(request, response));
  const socketServer = new WebSocketServer({
    noServer: true,
    maxPayload: MAX_MESSAGE_BYTES,
    perMessageDeflate: false,
  });

  server.on("upgrade", (request, socket, head) => {
    const requestUrl = new URL(request.url ?? "/", `http://${request.headers.host}`);
    if (requestUrl.pathname !== "/ws") {
      socket.write("HTTP/1.1 404 Not Found\r\nConnection: close\r\n\r\n");
      socket.destroy();
      return;
    }

    socketServer.handleUpgrade(request, socket, head, (client) => {
      socketServer.emit("connection", client, request);
    });
  });

  socketServer.on("connection", (socket) => {
    const peer = {
      socket,
      id: randomUUID(),
      name: "Guest",
      roomName: null,
      mode: "web",
      cursor: [0, 0],
      joined: false,
      lastCursorAt: 0,
    };
    clients.add(peer);

    const joinTimeout = setTimeout(() => {
      if (!peer.joined) socket.close(1008, "Join a valid room to continue.");
    }, 5000);

    socket.on("message", (payload, isBinary) => {
      if (isBinary) {
        socket.close(1003, "Only JSON messages are supported.");
        return;
      }

      let message;
      try {
        message = JSON.parse(payload.toString());
      } catch {
        socket.close(1007, "Message must be valid JSON.");
        return;
      }

      if (!peer.joined) {
        if (message.type !== "join" || !validRoom(message.room)) {
          socket.close(1008, "Join a valid room first.");
          return;
        }

        const roomName = message.room.toLowerCase();
        let room = rooms.get(roomName);
        if (!room) {
          if (rooms.size >= MAX_ROOMS) {
            socket.close(1013, "The relay is at room capacity.");
            return;
          }
          room = { peers: new Map(), objects: new Map() };
          rooms.set(roomName, room);
        }
        if (room.peers.size >= MAX_CLIENTS) {
          socket.close(1013, "This room is full.");
          return;
        }

        peer.roomName = roomName;
        peer.name = cleanName(message.name);
        peer.mode = message.mode === "vr" || message.mode === "ar" ? message.mode : "web";
        peer.joined = true;
        room.peers.set(peer.id, peer);
        clearTimeout(joinTimeout);
        send(peer, {
          type: "welcome",
          selfId: peer.id,
          room: peer.roomName,
          peers: [...room.peers.values()].filter((item) => item !== peer).map(clientProfile),
          objects: [...room.objects.values()],
        });
        broadcast(room, { type: "peer-join", peer: clientProfile(peer) }, peer.id);
        return;
      }

      if (typeof message.type !== "string") return;
      const room = rooms.get(peer.roomName);
      if (!room) return;

      switch (message.type) {
        case "cursor": {
          const now = Date.now();
          if (now - peer.lastCursorAt < 30 || !isVector(message.position, 6, 2)) return;
          peer.cursor = message.position;
          peer.lastCursorAt = now;
          broadcast(room, {
            type: "peer-cursor",
            id: peer.id,
            position: peer.cursor,
            mode: peer.mode,
          }, peer.id);
          break;
        }
        case "place": {
          if (room.objects.size >= MAX_OBJECTS) {
            send(peer, { type: "room-limit", message: "This room has reached its shared-part limit." });
            return;
          }
          if (!PART_TYPES.has(message.part) || !isVector(message.position, 8)
            || !isVector(message.rotation, Math.PI * 4)) return;
          const object = {
            id: randomUUID(),
            part: message.part,
            position: message.position,
            rotation: message.rotation,
            createdBy: peer.id,
            heldBy: null,
            valveOpen: false,
          };
          room.objects.set(object.id, object);
          broadcast(room, { type: "object-added", object });
          break;
        }
        case "grab": {
          const object = getObject(room, message.id);
          if (!object) return;
          if (object.heldBy && object.heldBy !== peer.id) {
            send(peer, { type: "grab-denied", id: object.id, holder: object.heldBy });
            return;
          }
          object.heldBy = peer.id;
          broadcast(room, { type: "object-grabbed", id: object.id, holder: peer.id });
          break;
        }
        case "move": {
          const object = getObject(room, message.id);
          if (!object || object.heldBy !== peer.id
            || !isVector(message.position, 8)
            || !isVector(message.rotation, Math.PI * 4)) return;
          object.position = message.position;
          object.rotation = message.rotation;
          broadcast(room, {
            type: "object-moved",
            id: object.id,
            position: object.position,
            rotation: object.rotation,
            holder: peer.id,
          }, peer.id);
          break;
        }
        case "release": {
          const object = getObject(room, message.id);
          if (!object || object.heldBy !== peer.id) return;
          if (isVector(message.position, 8)) object.position = message.position;
          if (isVector(message.rotation, Math.PI * 4)) object.rotation = message.rotation;
          object.heldBy = null;
          broadcast(room, {
            type: "object-released",
            id: object.id,
            position: object.position,
            rotation: object.rotation,
          });
          break;
        }
        case "delete": {
          const object = getObject(room, message.id);
          if (!object || (object.heldBy && object.heldBy !== peer.id)) return;
          room.objects.delete(object.id);
          broadcast(room, { type: "object-removed", id: object.id });
          break;
        }
        case "valve": {
          const object = getObject(room, message.id);
          if (!object || object.part !== "valve") return;
          object.valveOpen = Boolean(message.open);
          broadcast(room, { type: "valve-state", id: object.id, open: object.valveOpen });
          break;
        }
        default:
          break;
      }
    });

    socket.on("close", () => {
      clearTimeout(joinTimeout);
      clients.delete(peer);
      if (!peer.joined) return;
      const room = rooms.get(peer.roomName);
      if (!room) return;
      room.peers.delete(peer.id);
      for (const object of room.objects.values()) {
        if (object.heldBy !== peer.id) continue;
        object.heldBy = null;
        broadcast(room, {
          type: "object-released",
          id: object.id,
          position: object.position,
          rotation: object.rotation,
        });
      }
      broadcast(room, { type: "peer-leave", id: peer.id });
      if (room.peers.size === 0) rooms.delete(peer.roomName);
    });
  });

  return {
    server,
    rooms,
    clients,
    async listen({ port = Number(process.env.PORT ?? 8443), host = process.env.HOST ?? "0.0.0.0" } = {}) {
      const keyPath = process.env.TLS_KEY;
      const certPath = process.env.TLS_CERT;
      const httpsRequested = keyPath || certPath;
      if (httpsRequested && (!keyPath || !certPath)) {
        throw new Error("Set both TLS_KEY and TLS_CERT to enable HTTPS.");
      }
      if (httpsRequested && (!existsSync(keyPath) || !existsSync(certPath))) {
        throw new Error("TLS_KEY or TLS_CERT does not point to an existing file.");
      }

      let activeServer = server;
      if (httpsRequested) {
        activeServer = createHttpsServer({
          key: readFileSync(keyPath),
          cert: readFileSync(certPath),
        }, (request, response) => serveStatic(request, response));
        activeServer.on("upgrade", server.listeners("upgrade")[0]);
        this.server = activeServer;
      }

      await new Promise((resolveListen, rejectListen) => {
        activeServer.once("error", rejectListen);
        activeServer.listen(port, host, resolveListen);
      });
      return activeServer.address();
    },
    async close() {
      for (const peer of clients) peer.socket.close();
      await new Promise((resolveClose, rejectClose) => {
        socketServer.close((error) => error ? rejectClose(error) : resolveClose());
      });
      if (server.listening) {
        await new Promise((resolveClose, rejectClose) => {
          server.close((error) => error ? rejectClose(error) : resolveClose());
        });
      } else if (this.server !== server && this.server?.listening) {
        await new Promise((resolveClose, rejectClose) => {
          this.server.close((error) => error ? rejectClose(error) : resolveClose());
        });
      }
    },
  };
}

function getObject(room, id) {
  return typeof id === "string" ? room.objects.get(id) : undefined;
}

function send(peer, message) {
  if (peer.socket.readyState === WebSocket.OPEN) peer.socket.send(JSON.stringify(message));
}

function broadcast(room, message, excludedId = null) {
  for (const peer of room.peers.values()) {
    if (peer.id !== excludedId) send(peer, message);
  }
}

function serveStatic(request, response) {
  if (request.method !== "GET" && request.method !== "HEAD") {
    response.writeHead(405, { Allow: "GET, HEAD" }).end("Method not allowed.");
    return;
  }

  let pathname;
  try {
    pathname = decodeURIComponent(new URL(request.url ?? "/", "http://localhost").pathname);
  } catch {
    response.writeHead(400).end("Bad request.");
    return;
  }

  const requestedPath = resolve(SITE_ROOT, `.${pathname}`);
  const relativePath = relative(SITE_ROOT, requestedPath);
  if (relativePath.startsWith(`..${sep}`) || relativePath === ".." || relativePath.startsWith("..")) {
    response.writeHead(403).end("Forbidden.");
    return;
  }

  let filePath = requestedPath;
  try {
    if (statSync(filePath).isDirectory()) filePath = join(filePath, "index.html");
    if (!statSync(filePath).isFile()) throw new Error("Not a file");
  } catch {
    response.writeHead(404).end("Not found.");
    return;
  }

  const contentType = CONTENT_TYPES.get(extname(filePath).toLowerCase()) ?? "application/octet-stream";
  response.writeHead(200, {
    "Content-Type": contentType,
    "X-Content-Type-Options": "nosniff",
    "Cache-Control": "no-store",
  });
  if (request.method === "HEAD") {
    response.end();
    return;
  }
  createReadStream(filePath).pipe(response);
}

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  const relay = createRelay();
  relay.listen().then(({ address, port }) => {
    const protocol = process.env.TLS_KEY ? "https" : "http";
    console.log(`XR multiplayer examples available at ${protocol}://${address}:${port}`);
    console.log("Add /multi-player/vr/ or /multi-player/ar/ and use the same room name on both clients.");
  }).catch((error) => {
    console.error("Could not start the XR multiplayer server.", error);
    process.exitCode = 1;
  });
}
