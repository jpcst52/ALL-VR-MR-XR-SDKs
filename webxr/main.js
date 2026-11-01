const enterButton = document.querySelector("#enter");
const status = document.querySelector("#status");
const preview = document.querySelector("#preview");
const previewContext = preview.getContext("2d");
const speedControl = document.querySelector("#speed");
const speedValue = document.querySelector("#speed-value");
const xrCanvas = document.createElement("canvas");

let activeSession = null;
let gl = null;
let xrLayer = null;
let referenceSpace = null;
const startTime = performance.now();

function drawPreview() {
  const t = ((performance.now() - startTime) / 1000) * Number(speedControl.value);
  const gradient = previewContext.createLinearGradient(0, 0, preview.width, preview.height);
  gradient.addColorStop(0, `hsl(${(190 + t * 18) % 360} 80% 45%)`);
  gradient.addColorStop(1, `hsl(${(285 + t * 18) % 360} 75% 28%)`);
  previewContext.fillStyle = gradient;
  previewContext.fillRect(0, 0, preview.width, preview.height);
  previewContext.fillStyle = "rgba(255,255,255,.8)";
  previewContext.beginPath();
  previewContext.arc(preview.width / 2, preview.height / 2, 42 + Math.sin(t) * 10, 0, Math.PI * 2);
  previewContext.fill();
  previewContext.strokeStyle = "rgba(255,255,255,.35)";
  previewContext.lineWidth = 2;
  for (let index = 0; index < 3; index += 1) {
    previewContext.beginPath();
    previewContext.ellipse(
      preview.width / 2,
      preview.height / 2,
      95 + index * 36,
      30 + index * 18,
      t * 0.15 + index * 0.65,
      0,
      Math.PI * 2,
    );
    previewContext.stroke();
  }
  previewContext.fillStyle = "#ffffff";
  for (let index = 0; index < 8; index += 1) {
    const angle = t * (0.25 + index * 0.025) + (index / 8) * Math.PI * 2;
    previewContext.beginPath();
    previewContext.arc(preview.width / 2 + Math.cos(angle) * (118 + (index % 3) * 34), preview.height / 2 + Math.sin(angle) * (42 + (index % 3) * 20), 3 + (index % 2), 0, Math.PI * 2);
    previewContext.fill();
  }
  requestAnimationFrame(drawPreview);
}

drawPreview();
speedControl.addEventListener("input", () => {
  speedValue.textContent = `${Number(speedControl.value).toFixed(1)}x`;
});

function drawXRFrame(_time, frame) {
  if (!activeSession || !gl || !xrLayer || !referenceSpace) return;
  activeSession.requestAnimationFrame(drawXRFrame);
  const pose = frame.getViewerPose(referenceSpace);
  if (!pose) return;

  gl.bindFramebuffer(gl.FRAMEBUFFER, xrLayer.framebuffer);
  const t = ((performance.now() - startTime) / 1000) * Number(speedControl.value);
  for (const view of pose.views) {
    const viewport = xrLayer.getViewport(view);
    gl.viewport(viewport.x, viewport.y, viewport.width, viewport.height);
    gl.clearColor(
      0.12 + 0.22 * (0.5 + 0.5 * Math.sin(t)),
      0.18 + 0.35 * (0.5 + 0.5 * Math.sin(t + 2)),
      0.42 + 0.35 * (0.5 + 0.5 * Math.sin(t + 4)),
      1,
    );
    gl.clear(gl.COLOR_BUFFER_BIT | gl.DEPTH_BUFFER_BIT);
  }
}

async function endSession() {
  if (activeSession) await activeSession.end();
}

async function enterVR() {
  enterButton.disabled = true;
  status.textContent = "Starting immersive VR…";
  try {
    gl = xrCanvas.getContext("webgl", { xrCompatible: true, alpha: false });
    if (!gl) throw new Error("This browser could not create a WebGL context.");
    const sessionPromise = navigator.xr.requestSession("immersive-vr");
    try {
      await gl.makeXRCompatible();
      activeSession = await sessionPromise;
    } catch (error) {
      sessionPromise.then((session) => session.end()).catch(() => {});
      throw error;
    }
    activeSession.addEventListener("end", () => {
      activeSession = null;
      xrLayer = null;
      referenceSpace = null;
      enterButton.disabled = false;
      enterButton.textContent = "Enter VR";
      status.textContent = "VR session ended. You can enter again.";
    }, { once: true });

    xrLayer = new XRWebGLLayer(activeSession, gl);
    await activeSession.updateRenderState({ baseLayer: xrLayer });
    referenceSpace = await activeSession.requestReferenceSpace("local");
    enterButton.textContent = "In VR";
    status.textContent = "VR session active. Use your headset controls to exit.";
    activeSession.requestAnimationFrame(drawXRFrame);
  } catch (error) {
    if (activeSession) {
      try { await endSession(); } catch { /* Session may already be ending. */ }
    }
    activeSession = null;
    enterButton.disabled = false;
    status.textContent = `Could not start VR: ${error.message}`;
  }
}

enterButton.addEventListener("click", enterVR);

if (!("xr" in navigator)) {
  enterButton.textContent = "WebXR unavailable";
  status.textContent = "This browser does not expose the WebXR Device API. The animated preview still works.";
} else {
  navigator.xr.isSessionSupported("immersive-vr").then((supported) => {
    enterButton.disabled = !supported;
    enterButton.textContent = supported ? "Enter VR" : "Immersive VR unavailable";
    status.textContent = supported
      ? "Immersive VR is supported. Click the button to request a session."
      : "This browser or device does not support immersive VR. The preview still works.";
  }).catch((error) => {
    enterButton.textContent = "WebXR support check failed";
    status.textContent = `Could not check WebXR support: ${error.message}`;
  });
}

window.addEventListener("beforeunload", () => {
  if (activeSession) activeSession.end();
});
