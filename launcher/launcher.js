// Builds the launcher from examples.json. All URLs are relative so the page works from
// `python -m http.server` and when the relay serves the repository root at "/".
(function () {
  "use strict";

  function el(tag, props, children) {
    var node = document.createElement(tag);
    Object.keys(props || {}).forEach(function (k) {
      if (k === "text") node.textContent = props[k];
      else if (k === "class") node.className = props[k];
      else node.setAttribute(k, props[k]);
    });
    (children || []).forEach(function (c) { if (c) node.appendChild(c); });
    return node;
  }

  function badgeClass(label) {
    if (/relay/i.test(label)) return "badge relay";
    if (/unity/i.test(label)) return "badge unity";
    return "badge";
  }

  function tile(ex) {
    var shot;
    if (ex.screenshot) {
      shot = el("div", { class: "shot" }, [
        el("img", { src: ex.screenshot, alt: "" }),
        ex.screenshotNote ? el("span", { class: "shotnote", text: ex.screenshotNote }) : null
      ]);
    } else {
      shot = el("div", { class: "shot ph", "aria-hidden": "true" }, [el("span", { text: ex.note || ex.badges[0] })]);
    }
    var badges = el("div", { class: "badges" }, ex.badges.map(function (b) { return el("span", { class: badgeClass(b), text: b }); }));
    badges.appendChild(el("span", { class: "badge status " + ex.statusKind, text: ex.status }));
    var body = el("div", { class: "body" }, [
      el("h3", { text: ex.title }),
      el("p", { text: ex.summary }),
      ex.note ? el("span", { class: "note", text: ex.note }) : null,
      badges,
      el("span", { class: "go", text: ex.webServed === false ? "Open the project notes" : "Launch" })
    ]);
    var a = el("a", { class: "tile", href: ex.url, id: "tile-" + ex.id }, [shot, body]);
    a.setAttribute("aria-label", ex.title + ". " + ex.status + ". " + ex.badges.join(", ") + (ex.note ? ". " + ex.note : ""));
    return a;
  }

  function render(data) {
    document.getElementById("subtitle").textContent = data.meta.subtitle;
    var byId = {};
    data.examples.forEach(function (e) { byId[e.id] = e; });

    // jump links
    var jump = document.getElementById("jump");
    jump.appendChild(el("a", { href: "#path", text: "Demo path" }));
    data.sections.forEach(function (s) { jump.appendChild(el("a", { href: "#sec-" + s.id, text: s.title.replace(/ \(.*\)/, "") })); });

    // demo path
    var path = document.getElementById("path");
    var total = data.demoPath.reduce(function (n, s) { return n + s.minutes; }, 0);
    path.appendChild(el("h2", { id: "path-h", text: "Recommended demo path" }));
    path.appendChild(el("p", { class: "intro", text: "Six steps, about " + total + " minutes. Each card opens what to show." }));
    var ol = el("ol", { class: "steps" });
    data.demoPath.forEach(function (s) {
      var inner = [
        el("span", { class: "num", text: String(s.step), "aria-hidden": "true" }),
        el("h3", { text: s.title + " (" + s.minutes + " min)" }),
        el("span", { class: "who", text: s.who }),
        el("span", { class: "what", text: s.what })
      ];
      var ref = s.ref && byId[s.ref];
      var content = ref ? el("a", { href: ref.url }, inner) : el("div", { class: "inner" }, inner);
      ol.appendChild(el("li", { class: "step" }, [content]));
    });
    path.appendChild(ol);

    // sections
    var host = document.getElementById("sections");
    data.sections.forEach(function (s) {
      var sec = el("section", { class: "block", id: "sec-" + s.id, "aria-labelledby": "h-" + s.id });
      sec.appendChild(el("h2", { id: "h-" + s.id, text: s.title }));
      sec.appendChild(el("p", { class: "intro", text: s.intro }));
      var grid = el("div", { class: "grid" });
      data.examples.filter(function (e) { return e.section === s.id; }).forEach(function (e) { grid.appendChild(tile(e)); });
      sec.appendChild(grid);
      host.appendChild(sec);
    });

    // tools
    var tools = document.getElementById("tools");
    tools.appendChild(el("h2", { id: "tools-h", text: "Presenter tools" }));
    var ul = el("ul");
    data.tools.forEach(function (t) {
      var a = el("a", { href: t.url }, [document.createTextNode(t.title), t.note ? el("small", { text: "(" + t.note + ")" }) : null]);
      ul.appendChild(el("li", {}, [a]));
    });
    tools.appendChild(ul);

    // relay hint: multiplayer pages need the relay, which serves this same site on its own port
    var port = String(data.meta.relayPort);
    if (location.protocol !== "file:" && location.port !== port) {
      var url = location.protocol + "//" + location.hostname + ":" + port + "/";
      var note = document.getElementById("relay-note");
      note.appendChild(document.createTextNode("Multiplayer tiles need the relay. This page is not served by it, so open the launcher from "));
      note.appendChild(el("a", { href: url, text: url }));
      note.appendChild(document.createTextNode(" once the relay is running (npm start in multi-player/)."));
      note.hidden = false;
    }
  }

  // logo: show only if the (local, uncommitted) file exists; otherwise the text wordmark stays
  var logo = document.getElementById("logo");
  function showLogo() { logo.hidden = false; document.getElementById("wordmark").hidden = true; }
  logo.addEventListener("load", showLogo);
  if (logo.complete && logo.naturalWidth > 0) showLogo();

  fetch("launcher/examples.json").then(function (r) {
    if (!r.ok) throw new Error("HTTP " + r.status);
    return r.json();
  }).then(render).catch(function (err) {
    document.getElementById("sections").appendChild(el("p", { class: "noscript", text: "Could not load launcher/examples.json (" + err.message + "). Serve this folder over http, for example python -m http.server 8101." }));
  });
})();
