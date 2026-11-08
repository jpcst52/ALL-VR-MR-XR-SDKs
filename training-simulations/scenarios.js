import { industrialScenarios } from "./industrial-scenarios.js";
import { partnerScenarios } from "./partner-scenarios.js";
import { biologyScenarios } from "./biology-scenarios.js";
import { applyReasons } from "./reasons.js";

// "Why this matters" reasons (hotspot.why) and one reasoning question per scenario (scenario.rationale): see reasons.js.
export const scenarios = applyReasons([...industrialScenarios, ...partnerScenarios, ...biologyScenarios]);

export const categories = [
  { id: "industrial", label: "Industrial and high-risk work", note: "Refinery, height, hot-work, and electrical safety awareness. Practice only; follow your site's permits.", accent: "#f2c94c", heading: "INDUSTRIAL SAFETY PRACTICE" },
  { id: "manufacturing", label: "Manufacturing quality", note: "Sampling, pressure testing, welding, and electronics checks, inspired by local manufacturers. Practice only; not any company's real process.", accent: "#66B2E3", heading: "MANUFACTURING QUALITY PRACTICE" },
  { id: "biology", label: "Life science (draft for faculty review)", note: "Draft for faculty review. Educational general biology only; not clinical or medical guidance.", accent: "#ff7b8a", heading: "LIFE SCIENCE PRACTICE · DRAFT FOR FACULTY REVIEW" },
];
