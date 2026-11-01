# Deck builder

Rebuilds the project's PowerPoint decks from code, using the current screenshots in the repository:

- `Project-Overview.pptx`
- `Run-the-XR-Examples.pptx`
- `multi-player/Multiplayer-WebXR-Examples.pptx`

The decks use one theme (dark navy with teal, purple, orange, and yellow accents), two layouts (title and content), named placeholders, and sections, so they restyle and edit cleanly in PowerPoint.

## Build

```powershell
npm install
$env:PPTX_SKILL_DIR = "<folder containing scripts/apply_theme.js>"
npm run build                  # all decks
node build.mjs overview run    # or choose decks: overview, run, multiplayer
```

`apply_theme.js` writes the theme colors and fonts into each finished deck (pptxgenjs cannot write theme colors itself). It comes from the PowerPoint skill used to author these decks; point `PPTX_SKILL_DIR` at the folder that contains `scripts/apply_theme.js`. Without it the decks still build, but scheme colors fall back to Office defaults, so run the theme step.

Screenshots are read from the paths in the deck sources (`overview.mjs`, `run-guide.mjs`, `multiplayer.mjs`), so regenerate the screenshots first (see [tools/vr-capture](../vr-capture/README.md)) and then rebuild the decks.

## Editing content

Slide text lives in the three `.mjs` files next to the helper library `lib.mjs`. Change the text or the screenshot path, rebuild, and check the result in PowerPoint.
