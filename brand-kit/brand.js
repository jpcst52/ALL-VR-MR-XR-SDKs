// University of Pittsburgh at Bradford + CIST brand lock-up for every web page in this repository.
//
// The official logo lives in branding/logo.png, which is git-ignored (public repository, permission not yet
// confirmed) and must never be copied into a tracked folder. This module only *references* it: if the file is
// present on the lab PC it is shown; otherwise a text wordmark is shown. The <img> is inserted only after the
// file has loaded, so a missing logo can never appear as a broken image.
//
// Usage: <div data-pitt-brand></div> (add data-pitt-brand="compact" for a smaller bar), plus
//        <link rel="stylesheet" href=".../brand-kit/brand.css"> and <script type="module" src=".../brand-kit/brand.js">.

export const logoUrl = new URL("../branding/logo.png", import.meta.url).href;
export const brandName = "University of Pittsburgh at Bradford";
export const deptShort = "CIST";
export const deptName = "Computer Information Systems & Technology";

let logoPromise = null;

// Resolves to a loaded HTMLImageElement, or null when the git-ignored logo is not on this machine.
export function loadLogo() {
  if (!logoPromise) {
    logoPromise = new Promise((resolve) => {
      if (typeof Image === "undefined") return resolve(null);
      const image = new Image();
      image.decoding = "async";
      image.onload = () => resolve(image.naturalWidth > 0 ? image : null);
      image.onerror = () => resolve(null);
      image.src = logoUrl;
    });
  }
  return logoPromise;
}

function span(className, text) {
  const node = document.createElement("span");
  node.className = className;
  if (text) node.textContent = text;
  return node;
}

export function mountBrand(host) {
  if (host.dataset.brandMounted) return host;
  host.dataset.brandMounted = "1";
  host.classList.add("pitt-brand");
  if (host.dataset.pittBrand === "compact") host.classList.add("pitt-brand--compact");
  host.dataset.logo = "pending";
  host.setAttribute("role", "img");
  host.setAttribute("aria-label", `${brandName}, ${deptName} (${deptShort})`);

  const mark = span("pitt-brand__mark");
  const wordmark = span("pitt-brand__wordmark");
  wordmark.append(span("pitt-brand__univ", "University of Pittsburgh"), span("pitt-brand__campus", "Bradford"));
  mark.append(wordmark);
  const dept = span("pitt-brand__dept");
  dept.append(span("pitt-brand__cist", deptShort), span("pitt-brand__deptname", deptName));
  host.replaceChildren(mark, span("pitt-brand__rule"), dept);

  loadLogo().then((image) => {
    if (!image) {
      host.dataset.logo = "off";
      return;
    }
    const logo = document.createElement("img");
    logo.className = "pitt-brand__logo";
    logo.src = logoUrl;
    logo.alt = "";
    mark.prepend(logo);
    host.dataset.logo = "on";
  });
  return host;
}

if (typeof document !== "undefined") {
  const run = () => document.querySelectorAll("[data-pitt-brand]").forEach(mountBrand);
  if (document.readyState === "loading") document.addEventListener("DOMContentLoaded", run);
  else run();
}
