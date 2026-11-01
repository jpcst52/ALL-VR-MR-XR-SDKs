import { industrialScenarios } from "./industrial-scenarios.js";
import { coreTasks } from "./core-tasks.js";

const coreScenarios = [
  {
    id: "customer-welcome",
    category: "business",
    title: "Welcome a walk-in",
    duration: "3 min",
    setting: "A neighborhood service counter",
    description: "Listen first, protect customer privacy, and agree on a useful next step.",
    object: "counter",
    steps: coreTasks["customer-welcome"],
  },
  {
    id: "workplace-spill",
    category: "business",
    title: "Make a safe work area",
    duration: "3 min",
    setting: "A shared shop or stockroom",
    description: "Recognize a hazard, keep others safe, and report it using the local procedure.",
    object: "safety",
    steps: coreTasks["workplace-spill"],
  },
  {
    id: "delivery-check",
    category: "business",
    title: "Check an incoming delivery",
    duration: "3 min",
    setting: "A small business receiving area",
    description: "Match a delivery to its order, flag damage, and keep a clear record.",
    object: "delivery",
    steps: coreTasks["delivery-check"],
  },
  {
    id: "informed-consent",
    category: "research",
    title: "Invite a participant",
    duration: "3 min",
    setting: "A community research project",
    description: "Explain a study clearly, support voluntary choice, and respect a participant's decision.",
    object: "consent",
    steps: coreTasks["informed-consent"],
  },
  {
    id: "sample-custody",
    category: "research",
    title: "Track a research sample",
    duration: "3 min",
    setting: "A supervised field research station",
    description: "Use approved labels, document custody, and pause when sample identity is uncertain.",
    object: "sample",
    steps: coreTasks["sample-custody"],
  },
  {
    id: "field-observation",
    category: "research",
    title: "Record a field observation",
    duration: "3 min",
    setting: "A local environmental observation route",
    description: "Collect consistent measurements and preserve unusual results without changing the data.",
    object: "field",
    steps: coreTasks["field-observation"],
  },
];

export const scenarios = [...coreScenarios, ...industrialScenarios];

export const categories = [
  { id: "business", label: "Local business essentials", note: "Practical skills for shops, service teams, and small workplaces.", accent: "#ffb36b", heading: "LOCAL BUSINESS ESSENTIALS" },
  { id: "research", label: "Research practice", note: "Ethical, careful habits for community and field research.", accent: "#73e7d4", heading: "RESEARCH PRACTICE" },
  { id: "industrial", label: "Industrial and high-risk work", note: "Refinery, height, hot-work, and electrical safety awareness. Practice only; follow your site's permits.", accent: "#f2c94c", heading: "INDUSTRIAL SAFETY PRACTICE" },
];
