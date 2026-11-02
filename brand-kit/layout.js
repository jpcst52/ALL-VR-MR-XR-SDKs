// Pure text-layout helpers for in-world panels (no DOM, no Three.js), so they can be unit-tested in Node.
// `measure(text, size)` returns the drawn width of `text` at font size `size` (pixels).

export function wrapWords(text, measure, size, maxWidth) {
  const words = String(text ?? "").split(/\s+/).filter(Boolean);
  const lines = [];
  let line = "";
  for (const word of words) {
    const next = line ? `${line} ${word}` : word;
    if (line && measure(next, size) > maxWidth) {
      lines.push(line);
      line = word;
    } else {
      line = next;
    }
  }
  if (line) lines.push(line);
  return lines;
}

// Largest size (from `size` down to `minSize`) at which the wrapped text fits in maxWidth x maxHeight.
// If even `minSize` overflows, the text is cut to the lines that fit and the last line ends with an ellipsis,
// so a panel never draws text outside its border.
export function fitText(text, { measure, maxWidth, maxHeight, size = 40, minSize = 18, lineHeight = 1.24 }) {
  let current = size;
  for (;;) {
    const lines = wrapWords(text, measure, current, maxWidth);
    const tooWide = lines.some((line) => measure(line, current) > maxWidth);
    const height = lines.length * current * lineHeight;
    if ((!tooWide && height <= maxHeight) || current <= minSize) {
      return clip(lines, { measure, size: current, maxWidth, maxLines: Math.max(1, Math.floor(maxHeight / (current * lineHeight))) });
    }
    current = Math.max(minSize, Math.floor(current * 0.92));
  }
}

function clip(lines, { measure, size, maxWidth, maxLines }) {
  const truncated = lines.length > maxLines;
  const kept = lines.slice(0, maxLines);
  let last = kept[kept.length - 1] ?? "";
  if (truncated || measure(last, size) > maxWidth) {
    while (last && measure(`${last}…`, size) > maxWidth) last = last.slice(0, -1).trimEnd();
    kept[kept.length - 1] = `${last}…`;
  }
  return { size, lines: kept, truncated };
}
