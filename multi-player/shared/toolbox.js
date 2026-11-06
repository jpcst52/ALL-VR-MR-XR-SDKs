import * as THREE from "three";
import { PITT, fontsReady } from "../../brand-kit/panels.js";

// The in-headset toolbox: one floating panel with a button for every part, the ready-made examples, and the scene actions
// (rotate, delete, open/close, reset). It is a flat canvas texture; pointing a controller ray at it gives the button under the ray.
// The page owns what the buttons do: `actions` has add(partId), example(index), rotate(), remove(), setState(on), reset().

const W = 1024;
const MARGIN = 28;
const GAP = 10;
const BUTTON_H = 84;
const SANS = "Roboto, 'Segoe UI', system-ui, sans-serif";
const DISPLAY = "'League Gothic', Impact, 'Arial Narrow', sans-serif";

export function createToolbox({ theme, actions, getState, width = 0.8 }) {
  const examples = theme.examples ?? [];
  const labels = theme.xrLabels ?? { off: "Off", on: "On" };

  // Layout: rows of buttons under small section labels. Heights are fixed so the panel is a known size.
  const buttons = [];
  let y = 104;
  const section = (title) => { const at = y; y += 52; return { title, at }; };
  const sections = [];
  const grid = (items, columns, kind) => {
    const buttonWidth = (W - MARGIN * 2 - GAP * (columns - 1)) / columns;
    items.forEach((item, index) => {
      buttons.push({
        ...item,
        kind,
        x: MARGIN + (index % columns) * (buttonWidth + GAP),
        y: y + Math.floor(index / columns) * (BUTTON_H + GAP),
        w: buttonWidth,
        h: BUTTON_H,
      });
    });
    y += Math.ceil(items.length / columns) * (BUTTON_H + GAP);
  };
  sections.push(section("ADD A PART"));
  grid(theme.parts.map((part) => ({ label: part.label, run: () => actions.add(part.id) })), 3, "part");
  if (examples.length) {
    sections.push(section("LOAD AN EXAMPLE"));
    grid(examples.map((item, index) => ({ label: item.label, run: () => actions.example(index) })), 2, "example");
  }
  sections.push(section("SELECTED PART"));
  grid([
    { label: labels.off, run: () => actions.setState(false), needs: "toggle" },
    { label: labels.on, run: () => actions.setState(true), needs: "toggle" },
    { label: "Rotate 45°", run: () => actions.rotate(), needs: "selection" },
    { label: "Delete", run: () => actions.remove(), needs: "selection" },
  ], 2, "action");
  grid([{ label: "Reset scene", run: () => actions.reset(), danger: true, confirm: true }], 1, "reset");
  const statusTop = y + 4;
  const H = statusTop + 214;

  const canvas = document.createElement("canvas");
  canvas.width = W;
  canvas.height = H;
  const context = canvas.getContext("2d");
  const texture = new THREE.CanvasTexture(canvas);
  texture.colorSpace = THREE.SRGBColorSpace;
  texture.anisotropy = 4;
  const height = width * (H / W);
  const panel = new THREE.Mesh(
    new THREE.PlaneGeometry(width, height),
    new THREE.MeshBasicMaterial({ map: texture, transparent: true, side: THREE.DoubleSide, toneMapped: false }),
  );
  panel.name = "toolbox";
  const group = new THREE.Group();
  group.add(panel);
  group.name = "toolbox-group";

  let hover = null;
  let flash = { button: null, until: 0 };
  let armedReset = { until: 0 };
  let lastKey = "";
  const raycaster = new THREE.Raycaster();

  const rounded = (x, yy, w, h, r) => {
    context.beginPath();
    context.roundRect(x, yy, w, h, r);
  };

  function wrap(text, maxWidth, maxLines) {
    const words = String(text).split(/\s+/);
    const lines = [];
    let line = "";
    for (const word of words) {
      const next = line ? `${line} ${word}` : word;
      if (context.measureText(next).width > maxWidth && line) { lines.push(line); line = word; } else line = next;
    }
    if (line) lines.push(line);
    if (lines.length > maxLines) { lines.length = maxLines; lines[maxLines - 1] = `${lines[maxLines - 1].replace(/\s*\S*$/, "")}…`; }
    return lines;
  }

  function draw(now) {
    const state = getState();
    context.clearRect(0, 0, W, H);
    const gradient = context.createLinearGradient(0, 0, 0, H);
    gradient.addColorStop(0, "#0d2a66");
    gradient.addColorStop(1, "#061a45");
    rounded(0, 0, W, H, 36);
    context.fillStyle = gradient;
    context.fill();
    context.lineWidth = 4;
    context.strokeStyle = "rgba(219,238,255,0.42)";
    context.stroke();
    context.fillStyle = PITT.gold;
    context.fillRect(MARGIN, 0, W - MARGIN * 2, 8);

    context.fillStyle = "#ffffff";
    context.font = `64px ${DISPLAY}`;
    context.textBaseline = "alphabetic";
    context.textAlign = "left";
    context.fillText("TOOLBOX", MARGIN, 76);
    context.font = `500 26px ${SANS}`;
    context.fillStyle = "#b9d4f2";
    context.textAlign = "right";
    context.fillText(state.connected ? theme.title : "Not in a room", W - MARGIN, 72);

    context.textAlign = "left";
    context.font = `700 24px ${SANS}`;
    context.fillStyle = PITT.gold;
    for (const item of sections) context.fillText(item.title, MARGIN, item.at + 34);

    context.textAlign = "center";
    context.textBaseline = "middle";
    for (const button of buttons) {
      const enabled = state.connected && (button.needs === "toggle" ? state.canToggle : button.needs === "selection" ? state.hasSelection : true);
      const armed = button.confirm && now < armedReset.until;
      const pressed = flash.button === button && now < flash.until;
      const hot = hover === button && enabled;
      let fill = "#16397f";
      if (button.kind === "example") fill = "#1b4a8f";
      if (button.danger) fill = armed ? "#d33a2c" : "#5b1e2a";
      if (!enabled) fill = "#16254a";
      if (hot) fill = button.danger ? "#b8323a" : "#2a62b8";
      if (pressed) fill = PITT.gold;
      rounded(button.x, button.y, button.w, button.h, 22);
      context.fillStyle = fill;
      context.fill();
      context.lineWidth = hot ? 6 : 3;
      context.strokeStyle = hot ? PITT.gold : "rgba(219,238,255,0.35)";
      context.stroke();
      context.fillStyle = pressed ? "#0b1f4d" : enabled ? "#ffffff" : "#6f82a8";
      context.font = `700 ${button.w < 300 ? 30 : 34}px ${SANS}`;
      const text = armed ? "Press again to reset" : button.label;
      context.fillText(text, button.x + button.w / 2, button.y + button.h / 2 + 2, button.w - 24);
    }

    // Status: the live summary (the HTML panel is not visible inside a headset), the selected part, and the button hints.
    context.textAlign = "left";
    context.textBaseline = "alphabetic";
    rounded(MARGIN, statusTop, W - MARGIN * 2, 196, 20);
    context.fillStyle = "rgba(0,0,0,0.32)";
    context.fill();
    context.font = `700 26px ${SANS}`;
    context.fillStyle = state.hasSelection ? PITT.gold : "#8fa6c8";
    context.fillText(state.selectedLabel, MARGIN + 22, statusTop + 40, W - MARGIN * 2 - 44);
    context.font = `500 25px ${SANS}`;
    context.fillStyle = "#e8f1fb";
    wrap(state.summary || "", W - MARGIN * 2 - 44, 3).forEach((line, index) => context.fillText(line, MARGIN + 22, statusTop + 80 + index * 32));
    context.font = `500 22px ${SANS}`;
    context.fillStyle = "#9fb8dc";
    context.fillText(`Controllers: trigger = grab or press · Y = ${labels.off.toLowerCase()} · B = ${labels.on.toLowerCase()} · X or A = rotate`, MARGIN + 22, statusTop + 184, W - MARGIN * 2 - 44);
    texture.needsUpdate = true;
  }

  const signature = (now) => JSON.stringify([getState(), hover && buttons.indexOf(hover), flash.button && now < flash.until, now < armedReset.until]);

  // Which button is under the ray, or null. `ray` is a THREE.Raycaster already aimed (from a controller or the pointer).
  function pick(ray) {
    if (!group.visible) return null;
    const hits = ray.intersectObject(panel, false);
    if (!hits.length || !hits[0].uv) return null;
    const px = hits[0].uv.x * W;
    const py = (1 - hits[0].uv.y) * H;
    return buttons.find((button) => px >= button.x && px <= button.x + button.w && py >= button.y && py <= button.y + button.h) ?? null;
  }
  // Distance from the ray origin to the panel (used to shorten a controller beam so it ends on the panel).
  function distance(ray) {
    if (!group.visible) return null;
    const hits = ray.intersectObject(panel, false);
    return hits.length ? hits[0].distance : null;
  }

  function press(button, now = performance.now()) {
    const state = getState();
    const enabled = state.connected && (button.needs === "toggle" ? state.canToggle : button.needs === "selection" ? state.hasSelection : true);
    if (!enabled) return false;
    if (button.confirm) {
      if (now > armedReset.until) { armedReset.until = now + 3500; return true; }
      armedReset.until = 0;
    }
    flash = { button, until: now + 220 };
    button.run();
    return true;
  }

  function update(now = performance.now()) {
    if (!group.visible) return;
    const key = signature(now);
    if (key === lastKey) return;
    lastKey = key;
    draw(now);
  }

  fontsReady.then(() => { lastKey = ""; });
  draw(0);

  return {
    group,
    panel,
    buttons,
    height,
    pick,
    distance,
    press,
    update,
    setHover(button) { hover = button; },
    // Test hook: where a button's centre is on the panel, in the group's local metres.
    buttonCentre(label) {
      const button = buttons.find((item) => item.label === label);
      if (!button) return null;
      return new THREE.Vector3(((button.x + button.w / 2) / W - 0.5) * width, (0.5 - (button.y + button.h / 2) / H) * height, 0);
    },
    raycaster,
  };
}
