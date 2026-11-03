// Shows a repository Markdown file inside the launcher, offline (no library, no CDN).
//   doc.html?f=docs/HOW-TO-RUN.md#section
// Only .md files inside the repository are shown. Links between guides stay in the viewer; other links open as they are.
(function () {
  "use strict";

  var root = new URL("../", location.href); // repository root as served

  function esc(s) { return s.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;"); }
  function slug(s) { return s.toLowerCase().replace(/<[^>]+>/g, "").replace(/[^\w\- ]+/g, "").trim().replace(/ /g, "-"); }

  var baseDir = ""; // directory of the open guide, relative to the repository root

  function resolve(href) {
    if (/^[a-z][a-z0-9+.-]*:/i.test(href) || href.charAt(0) === "#") return href; // external, mailto, in-page
    var hash = "", i = href.indexOf("#");
    if (i >= 0) { hash = href.slice(i); href = href.slice(0, i); }
    var url = new URL(href, new URL(baseDir, root));
    var rel = decodeURIComponent(url.pathname.slice(root.pathname.length));
    if (/\.md$/i.test(rel)) return "doc.html?f=" + encodeURIComponent(rel) + hash;
    return new URL("../" + rel, location.href).pathname + hash;
  }

  function inline(s) {
    var codes = [];
    s = s.replace(/`([^`]+)`/g, function (_, c) { codes.push(c); return "\u0001" + (codes.length - 1) + "\u0001"; });
    s = esc(s);
    s = s.replace(/!\[([^\]]*)\]\(([^)\s]+)(?:\s+"[^"]*")?\)/g, function (_, alt, src) { return '<img src="' + resolve(src) + '" alt="' + alt + '" loading="lazy">'; });
    s = s.replace(/\[([^\]]+)\]\(([^)\s]+)(?:\s+&quot;[^)]*&quot;)?\)/g, function (_, t, href) {
      var r = resolve(href), ext = /^https?:/i.test(r);
      return '<a href="' + r + '"' + (ext ? ' rel="noopener"' : "") + ">" + t + "</a>";
    });
    s = s.replace(/\*\*([^*]+)\*\*/g, "<strong>$1</strong>").replace(/(^|[^*\w])\*([^*\s][^*]*)\*(?!\w)/g, "$1<em>$2</em>");
    s = s.replace(/\u0001(\d+)\u0001/g, function (_, n) { return "<code>" + esc(codes[+n]) + "</code>"; });
    return s;
  }

  function cells(line) { return line.replace(/^\s*\|/, "").replace(/\|\s*$/, "").split(/(?<!\\)\|/).map(function (c) { return c.trim().replace(/\\\|/g, "|"); }); }

  function render(md) {
    var lines = md.replace(/\r\n?/g, "\n").split("\n"), out = [], i = 0, used = {};
    while (i < lines.length) {
      var ln = lines[i], m;
      if ((m = /^```(\w*)/.exec(ln))) {
        var code = []; i++;
        while (i < lines.length && !/^```/.test(lines[i])) code.push(lines[i++]);
        i++; out.push("<pre><code>" + esc(code.join("\n")) + "</code></pre>"); continue;
      }
      if ((m = /^(#{1,6})\s+(.*?)\s*#*$/.exec(ln))) {
        var id = slug(m[2]); if (used[id]) id += "-" + used[id]++; else used[id] = 1;
        out.push("<h" + m[1].length + ' id="' + id + '">' + inline(m[2]) + "</h" + m[1].length + ">"); i++; continue;
      }
      if (/^\s*(-{3,}|\*{3,})\s*$/.test(ln)) { out.push("<hr>"); i++; continue; }
      if (/^\s*\|/.test(ln) && i + 1 < lines.length && /^\s*\|?\s*:?-{2,}/.test(lines[i + 1])) {
        var head = cells(ln); i += 2; var rows = [];
        while (i < lines.length && /^\s*\|/.test(lines[i])) rows.push(cells(lines[i++]));
        out.push('<div class="tw"><table><thead><tr>' + head.map(function (c) { return "<th>" + inline(c) + "</th>"; }).join("") + "</tr></thead><tbody>" +
          rows.map(function (r) { return "<tr>" + r.map(function (c) { return "<td>" + inline(c) + "</td>"; }).join("") + "</tr>"; }).join("") + "</tbody></table></div>");
        continue;
      }
      if (/^\s*>/.test(ln)) {
        var q = []; while (i < lines.length && /^\s*>/.test(lines[i])) q.push(lines[i++].replace(/^\s*>\s?/, ""));
        out.push("<blockquote>" + inline(q.join(" ")) + "</blockquote>"); continue;
      }
      if (/^\s*([-*+]|\d+\.)\s+/.test(ln)) {
        var items = [], ordered = /^\s*\d+\./.test(ln);
        while (i < lines.length && (/^\s*([-*+]|\d+\.)\s+/.test(lines[i]) || (/^\s{2,}\S/.test(lines[i]) && items.length))) {
          var t = lines[i++];
          if (/^\s*([-*+]|\d+\.)\s+/.test(t)) items.push({ indent: /^\s*/.exec(t)[0].length, text: t.replace(/^\s*([-*+]|\d+\.)\s+/, "") });
          else items[items.length - 1].text += " " + t.trim();
        }
        out.push(list(items, ordered)); continue;
      }
      if (/^\s*$/.test(ln)) { i++; continue; }
      var p = [];
      while (i < lines.length && !/^\s*$/.test(lines[i]) && !/^(#{1,6}\s|```|\s*>|\s*\|)/.test(lines[i]) && !/^\s*([-*+]|\d+\.)\s+/.test(lines[i])) p.push(lines[i++].trim());
      out.push("<p>" + inline(p.join(" ")) + "</p>");
    }
    return out.join("\n");
  }

  function list(items, ordered) {
    var html = "", stack = [], tag = ordered ? "ol" : "ul";
    items.forEach(function (it) {
      while (stack.length && it.indent < stack[stack.length - 1]) { html += "</li></" + tag + ">"; stack.pop(); }
      if (!stack.length || it.indent > stack[stack.length - 1]) { html += "<" + tag + ">"; stack.push(it.indent); }
      else html += "</li>";
      html += "<li>" + inline(it.text);
    });
    while (stack.length) { html += "</li></" + tag + ">"; stack.pop(); }
    return html;
  }

  var f = new URLSearchParams(location.search).get("f") || "";
  var host = document.getElementById("doc");
  if (!/^[\w .\-\/]+\.md$/.test(f) || f.indexOf("..") >= 0 || f.charAt(0) === "/") {
    host.innerHTML = "<h1>Pick a guide from the main menu</h1><p>This page shows repository guides. Go back to the <a href=\"../\">main menu</a> and choose one.</p>";
    return;
  }
  baseDir = f.indexOf("/") >= 0 ? f.slice(0, f.lastIndexOf("/") + 1) : "";
  document.getElementById("where").textContent = f;
  fetch(new URL(f, root)).then(function (r) {
    if (!r.ok) throw new Error("HTTP " + r.status);
    return r.text();
  }).then(function (md) {
    host.innerHTML = render(md);
    var h1 = host.querySelector("h1");
    document.title = (h1 ? h1.textContent : f) + " · Pitt-Bradford XR Training";
    if (location.hash) { var t = document.getElementById(decodeURIComponent(location.hash.slice(1))); if (t) t.scrollIntoView(); }
  }).catch(function (err) {
    host.innerHTML = "<h1>Could not open " + esc(f) + "</h1><p>" + esc(err.message) + ". Open this menu from the demo server (see the run guide) so the files are served.</p>";
  });
})();
