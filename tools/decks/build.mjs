// Builds the decks. Usage: PPTX_SKILL_DIR=<pptx skill folder> node build.mjs [overview] [run] [multiplayer] [repository] [labs]
import { join } from "node:path";
import { repo } from "./lib.mjs";

const wanted = process.argv.slice(2);
const decks = {
  overview: async () => (await import("./overview.mjs")).buildOverview(join(repo, "Project-Overview.pptx")),
  run: async () => (await import("./run-guide.mjs")).buildRunGuide(join(repo, "Run-the-XR-Examples.pptx")),
  repository: async () => (await import("./repository.mjs")).buildRepository(join(repo, "Repository-Guide.pptx")),
  labs: async () => (await import("./labs.mjs")).buildLabs(join(repo, "Labs-and-Hub.pptx")),
  walkthrough: async () => (await import("./walkthrough.mjs")).buildWalkthrough(join(repo, "Scenario-Walkthrough.pptx")),
  multiplayer: async () => (await import("./multiplayer.mjs")).buildMultiplayer(join(repo, "multi-player", "Multiplayer-WebXR-Examples.pptx")),
};
for (const name of wanted.length ? wanted : Object.keys(decks)) {
  console.log(`Building ${name}...`);
  await decks[name]();
}
console.log("Done.");
