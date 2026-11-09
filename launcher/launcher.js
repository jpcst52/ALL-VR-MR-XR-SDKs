// Builds the launcher from examples.json. All URLs are relative so the page works from
// `python -m http.server` and when the relay serves the repository root at "/".
// The same script builds the home page (index.html) and the sub pages (launcher/section.html?s=<section id>),
// which show the sections marked "subpage": true. Sub pages set data-root="../" on <body>.
(function () {
  "use strict";

  var ROOT = document.body.getAttribute("data-root") || "";
  var IS_SUBPAGE = document.body.getAttribute("data-page") === "section";

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

  function searchText(ex) { return [ex.title, ex.summary, ex.note, ex.status, ex.badges.join(" "), ex.id].join(" ").toLowerCase(); }
  function shortTitle(s) { return s.title.replace(/ \(.*\)/, ""); }

  function tile(ex) {
    var shot;
    if (ex.screenshot) {
      shot = el("div", { class: "shot" }, [
        el("img", { src: ROOT + ex.screenshot, alt: "" }),
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
    var a = el("a", { class: "tile", href: /\.md$/i.test(ex.url) ? ROOT + "launcher/doc.html?f=" + encodeURIComponent(ex.url) : ROOT + ex.url, id: "tile-" + ex.id }, [shot, body]);
    a.setAttribute("data-search", searchText(ex));
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
    var home = data.sections.filter(function (s) { return !s.subpage; });
    var sub = data.sections.filter(function (s) { return s.subpage; });
    home.forEach(function (s) { jump.appendChild(el("a", { href: "#sec-" + s.id, text: shortTitle(s) })); });
    jump.appendChild(el("a", { href: "launcher/guides.html", text: "Guides and decks" }));

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
    home.forEach(function (s) {
      var sec = el("section", { class: "block", id: "sec-" + s.id, "aria-labelledby": "h-" + s.id });
      sec.appendChild(el("h2", { id: "h-" + s.id, text: s.title }));
      sec.appendChild(el("p", { class: "intro", text: s.intro }));
      var grid = el("div", { class: "grid" });
      data.examples.filter(function (e) { return e.section === s.id; }).forEach(function (e) { grid.appendChild(tile(e)); });
      sec.appendChild(grid);
      host.appendChild(sec);
    });

    // sections that live on their own page: linked from the bottom
    var more = document.getElementById("more");
    if (sub.length) {
      more.appendChild(el("h2", { id: "more-h", text: "More examples" }));
      more.appendChild(el("p", { class: "intro", text: "These have their own page." }));
      var ul = el("ul");
      sub.forEach(function (s) {
        var list = data.examples.filter(function (e) { return e.section === s.id; });
        var a = el("a", { href: "launcher/section.html?s=" + encodeURIComponent(s.id), id: "more-" + s.id }, [
          document.createTextNode(shortTitle(s)),
          el("small", { text: list.length + (list.length === 1 ? " example" : " examples") })
        ]);
        a.setAttribute("data-search", [s.title, s.intro].concat(list.map(searchText)).join(" ").toLowerCase());
        ul.appendChild(el("li", {}, [a]));
      });
      more.appendChild(ul);
    } else more.hidden = true;

    // relay hint: multiplayer pages need the relay, which serves this same site on its own port
    var port = String(data.meta.relayPort);
    if (/github\.io$/i.test(location.hostname)) {
      // GitHub Pages: static files only, so the relay-based tiles cannot connect from here.
      var pn = document.getElementById("relay-note");
      pn.appendChild(document.createTextNode("This is the GitHub Pages copy (static files only). The multiplayer tiles load but cannot join a room, because the relay runs on the lab PC. Open the lab PC's own address (https, port " + port + ") for those. Everything else works here. "));
      pn.appendChild(el("a", { href: "launcher/doc.html?f=docs%2FGITHUB-PAGES.md", text: "What works on Pages" }));
      pn.hidden = false;
    } else if (location.protocol !== "file:") {
      // Ask this origin whether it is the relay (it answers /healthz). If so, rooms work from here, on whatever port it got.
      // If not (a plain static server), point to the relay's usual port.
      fetch("/healthz", { cache: "no-store" }).then(function (r) { return r.ok ? r.json() : null; }).catch(function () { return null; }).then(function (h) {
        if (h && h.relay) return;
        var url = location.protocol + "//" + location.hostname + ":" + port + "/";
        var note = document.getElementById("relay-note");
        note.appendChild(document.createTextNode("Multiplayer tiles need the relay. This page is not served by it, so open the launcher from "));
        note.appendChild(el("a", { href: url, text: url }));
        note.appendChild(document.createTextNode(" once the relay is running (scripts\start-demo.cmd). If that port was busy the relay prints the port it chose."));
        note.hidden = false;
      });
    }
  }

  // Search / filter: client-side only. Tiles and guide links carry a data-search string.
  function setupFinder() {
    var q = document.getElementById("q"), clear = document.getElementById("clear"), count = document.getElementById("count");
    var links = [].slice.call(document.querySelectorAll("#sections .tile"));
    var path = document.getElementById("path");
    var more = document.getElementById("more"), moreLinks = [].slice.call(more.querySelectorAll("a"));
    q.disabled = false;

    function apply() {
      var terms = q.value.toLowerCase().split(/\s+/).filter(Boolean), shown = 0;
      links.forEach(function (a) {
        var hay = a.getAttribute("data-search"), ok = terms.every(function (t) { return hay.indexOf(t) >= 0; });
        a.hidden = !ok;
        if (ok) shown++;
      });
      [].forEach.call(document.querySelectorAll("section.block[id^=sec-]"), function (sec) {
        sec.hidden = !!terms.length && !sec.querySelector(".tile:not([hidden])");
      });
      // sub pages: keep the bottom link when one of that page's examples matches
      var extra = 0;
      moreLinks.forEach(function (a) {
        var hay = a.getAttribute("data-search"), ok = terms.every(function (t) { return hay.indexOf(t) >= 0; });
        a.parentNode.hidden = !ok;
        if (ok) extra++;
      });
      more.hidden = !moreLinks.length || (!!terms.length && !extra);
      path.hidden = !!terms.length;
      clear.hidden = !terms.length;
      var said = "“" + q.value.trim() + "”";
      count.textContent = !terms.length ? "" :
        shown ? shown + " match" + (shown === 1 ? "" : "es") + " for " + said + "." + (extra ? " More on the pages linked at the bottom." : "") :
        extra ? "No tile on this page has " + said + ", but the pages linked at the bottom do." :
        "Nothing matches " + said + ". Try a shorter word.";
    }
    function visible() { return links.filter(function (a) { return !a.closest("[hidden]"); }); }

    q.addEventListener("input", apply);
    clear.addEventListener("click", function () { q.value = ""; apply(); q.focus(); });
    document.addEventListener("keydown", function (e) {
      var tag = (e.target.tagName || "").toLowerCase();
      if (e.key === "/" && tag !== "input" && !e.ctrlKey && !e.metaKey && !e.altKey) { e.preventDefault(); q.focus(); q.select(); return; }
      if (e.key === "Escape" && (q.value || document.activeElement === q)) { q.value = ""; apply(); q.blur(); return; }
      var list = visible(), i = list.indexOf(document.activeElement), k = e.key;
      if (e.target === q && k === "ArrowDown" && list.length) { e.preventDefault(); list[0].focus(); return; }
      if (i < 0) return;
      var next = k === "ArrowRight" || k === "ArrowDown" ? i + 1 : k === "ArrowLeft" || k === "ArrowUp" ? i - 1 : k === "Home" ? 0 : k === "End" ? list.length - 1 : null;
      if (next === null) return;
      e.preventDefault();
      if (next < 0) q.focus(); else if (list[next]) list[next].focus();
    });
  }

  // The Pitt-Bradford + CIST brand bar is built by brand-kit/brand.js (logo if the local file exists, else a wordmark).

  // A sub page: one section's tiles, with a link back to the main menu.
  function renderSection(data) {
    var id = new URLSearchParams(location.search).get("s");
    var s = data.sections.filter(function (x) { return x.id === id; })[0];
    var host = document.getElementById("sections");
    if (!s) {
      host.appendChild(el("p", { class: "noscript", text: "There is no section called “" + (id || "") + "”. Go back to the main menu." }));
      return;
    }
    document.title = shortTitle(s) + " · Pitt-Bradford XR Training";
    document.getElementById("title").textContent = shortTitle(s);
    document.getElementById("subtitle").textContent = s.intro;
    var grid = el("div", { class: "grid" });
    data.examples.filter(function (e) { return e.section === s.id; }).forEach(function (e) { grid.appendChild(tile(e)); });
    host.appendChild(el("section", { class: "block", id: "sec-" + s.id, "aria-label": s.title }, [grid]));
  }

  fetch(ROOT + "launcher/examples.json").then(function (r) {
    if (!r.ok) throw new Error("HTTP " + r.status);
    return r.json();
  }).then(function (data) {
    if (IS_SUBPAGE) renderSection(data); else { render(data); setupFinder(); }
  }).catch(function (err) {
    document.getElementById("sections").appendChild(el("p", { class: "noscript", text: "Could not load launcher/examples.json (" + err.message + "). Serve this folder over http, for example python -m http.server 8101." }));
  });
})();
