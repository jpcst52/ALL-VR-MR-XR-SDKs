// The "Guides, status and decks" page: links to every document and deck in launcher/examples.json (data.guides).
// Markdown files open in the in-menu viewer (doc.html); other files (decks, pages) open directly. Offline, no libraries.
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

  function render(data) {
    var host = document.getElementById("tools");
    host.appendChild(el("h2", { id: "tools-h", text: "Guides, status and decks" }));
    var groups = el("div", { class: "groups" });
    data.guides.forEach(function (g) {
      var ul = el("ul");
      g.links.forEach(function (t) {
        var href = /\.md$/i.test(t.url) ? "doc.html?f=" + encodeURIComponent(t.url) : "../" + t.url;
        var a = el("a", { href: href, id: "guide-" + t.id, "data-search": [t.title, t.note || "", g.title, t.id].join(" ").toLowerCase() }, [document.createTextNode(t.title), t.note ? el("small", { text: t.note }) : null]);
        ul.appendChild(el("li", {}, [a]));
      });
      groups.appendChild(el("section", { class: "group", "aria-labelledby": "g-" + g.id }, [el("h3", { id: "g-" + g.id, text: g.title }), ul]));
    });
    host.appendChild(groups);
  }

  function setupFinder() {
    var q = document.getElementById("q"), clear = document.getElementById("clear"), count = document.getElementById("count");
    var links = [].slice.call(document.querySelectorAll("#tools .group a"));
    q.disabled = false;
    function apply() {
      var terms = q.value.toLowerCase().split(/\s+/).filter(Boolean), shown = 0;
      links.forEach(function (a) {
        var ok = terms.every(function (t) { return a.getAttribute("data-search").indexOf(t) >= 0; });
        a.parentNode.hidden = !ok;
        if (ok) shown++;
      });
      [].forEach.call(document.querySelectorAll(".group"), function (g) { g.hidden = !!terms.length && !g.querySelector("li:not([hidden])"); });
      clear.hidden = !terms.length;
      count.textContent = terms.length ? (shown ? shown + " match" + (shown === 1 ? "" : "es") + "." : "Nothing matches. Try a shorter word.") : "";
    }
    q.addEventListener("input", apply);
    clear.addEventListener("click", function () { q.value = ""; apply(); q.focus(); });
    document.addEventListener("keydown", function (e) {
      var tag = (e.target.tagName || "").toLowerCase();
      if (e.key === "/" && tag !== "input") { e.preventDefault(); q.focus(); }
      else if (e.key === "Escape" && q.value) { q.value = ""; apply(); }
    });
  }

  fetch("examples.json").then(function (r) {
    if (!r.ok) throw new Error("HTTP " + r.status);
    return r.json();
  }).then(function (data) { render(data); setupFinder(); }).catch(function (err) {
    document.getElementById("tools").appendChild(el("p", { class: "noscript", text: "Could not load examples.json (" + err.message + "). Serve the repository over http, for example python -m http.server." }));
  });
})();
