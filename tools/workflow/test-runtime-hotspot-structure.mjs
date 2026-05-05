import fs from "node:fs";
import path from "node:path";

const repoRoot = path.resolve(process.argv[2] ?? process.cwd());
const issues = [];

const hotspotBudgets = [
  {
    path: "Assets/Scripts/Features/Garage/Presentation/Uitk/GarageSetBUitkRuntimeAdapter.cs",
    maxLines: 380,
    owner: "Garage runtime adapter",
  },
  {
    path: "Assets/Scripts/Features/Lobby/Presentation/Uitk/LobbyUitkRuntimeAdapter.cs",
    maxLines: 580,
    owner: "Lobby runtime adapter",
  },
  {
    path: "Assets/Scripts/Features/Player/BattleSceneRoot.cs",
    maxLines: 300,
    owner: "BattleSceneRoot",
  },
];

const extractedOwners = [
  {
    path: "Assets/Scripts/Features/Lobby/Presentation/Uitk/LobbyOperationMemoryRenderer.cs",
    requiredFragments: ["RenderLatestOperation", "memory-stat-grid", "memory-sitrep"],
  },
  {
    path: "Assets/Scripts/Features/Player/BattleSceneLocalPlayerInitializationFlow.cs",
    requiredFragments: [
      "InitialEnergyValidator",
      "InitializeBattleEntity(",
      "EnergyAdapterInstance.GetCurrentEnergy",
    ],
  },
];

const authoredLayoutContracts = [
  {
    path: "Assets/UI/UIToolkit/GarageSetB/GarageSetBWorkspace.uxml",
    requiredFragments: [
      'name="SlotStrip"',
      'name="PreviewCard"',
      'name="PartFocusBar"',
      'name="PartSelectionPane"',
      'name="SaveDock"',
    ],
  },
  {
    path: "Assets/UI/UIToolkit/GarageSetB/GarageSetBWorkspace.uss",
    requiredFragments: [
      ".preview-card",
      "height: 156px;",
      ".unit-diagram",
      "width: 108px;",
      ".stat-radar-graph",
      ".part-list-rows",
      "height: 252px;",
      ".save-dock",
      "position: relative;",
    ],
  },
];

const forbiddenFiles = [
  "Assets/Scripts/Features/Garage/Presentation/Uitk/GarageSetBUitkLayoutController.cs",
];

const reabsorptionChecks = [
  {
    path: "Assets/Scripts/Features/Garage/Presentation/Uitk/GarageSetBUitkRuntimeAdapter.cs",
    message: "Garage adapter must not reabsorb layout geometry or pointer-scroll handling.",
    blockedFragments: ["GeometryChangedEvent", "PointerMoveEvent", "worldBound"],
  },
  {
    path: "Assets/Scripts/Features/Lobby/Presentation/Uitk/LobbyUitkRuntimeAdapter.cs",
    message: "Lobby adapter must not reabsorb operation-memory rendering details.",
    blockedFragments: ["RenderLatestOperation", "memory-stat-grid", "memory-sitrep"],
  },
  {
    path: "Assets/Scripts/Features/Player/BattleSceneRoot.cs",
    message: "BattleSceneRoot must not reabsorb local-player battle initialization details.",
    blockedFragments: [
      "InitialEnergyValidator",
      "InitializeBattleEntity(",
      "EnergyAdapterInstance.GetCurrentEnergy",
    ],
  },
];

function absolute(relativePath) {
  return path.join(repoRoot, ...relativePath.split("/"));
}

function readText(relativePath) {
  const filePath = absolute(relativePath);
  if (!fs.existsSync(filePath)) {
    issues.push(`Missing expected runtime hotspot file: ${relativePath}`);
    return "";
  }

  return fs.readFileSync(filePath, "utf8");
}

function countLines(text) {
  if (text.length === 0) return 0;
  return text.split(/\r?\n/).length;
}

function assertOrdered(text, relativePath, names) {
  let previousIndex = -1;
  for (const name of names) {
    const index = text.indexOf(`name="${name}"`);
    if (index < 0) {
      issues.push(`Authored UXML element missing: ${relativePath} name=${name}`);
      continue;
    }

    if (index <= previousIndex) {
      issues.push(`Authored UXML order mismatch: ${relativePath} name=${name}`);
    }

    previousIndex = index;
  }
}

function getLastUssRuleBlock(text, selector) {
  const escaped = selector.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
  const regex = new RegExp(`${escaped}\\s*\\{([^}]*)\\}`, "g");
  let match = null;
  let last = null;
  while ((match = regex.exec(text)) !== null) {
    last = match[1];
  }

  return last;
}

function assertUssRuleContains(text, relativePath, selector, declarations) {
  const block = getLastUssRuleBlock(text, selector);
  if (block == null) {
    issues.push(`Authored USS selector missing: ${relativePath} selector=${selector}`);
    return;
  }

  for (const declaration of declarations) {
    if (!block.includes(declaration)) {
      issues.push(`Authored USS declaration missing: ${relativePath} selector=${selector} declaration=${declaration}`);
    }
  }
}

for (const budget of hotspotBudgets) {
  const text = readText(budget.path);
  const lineCount = countLines(text);
  if (lineCount > budget.maxLines) {
    issues.push(
      `${budget.owner} exceeded responsibility budget: ${budget.path} lines=${lineCount} budget=${budget.maxLines}`,
    );
  }
}

for (const contract of authoredLayoutContracts) {
  const text = readText(contract.path);
  for (const fragment of contract.requiredFragments) {
    if (!text.includes(fragment)) {
      issues.push(`Authored layout contract missing marker: ${contract.path} fragment=${fragment}`);
    }
  }
}

for (const relativePath of forbiddenFiles) {
  if (fs.existsSync(absolute(relativePath))) {
    issues.push(`Forbidden runtime layout owner restored: ${relativePath}`);
  }
}

const garageUxmlPath = "Assets/UI/UIToolkit/GarageSetB/GarageSetBWorkspace.uxml";
const garageUxml = readText(garageUxmlPath);
assertOrdered(garageUxml, garageUxmlPath, [
  "SlotStrip",
  "PreviewCard",
  "PartFocusBar",
  "PartSelectionPane",
  "SaveDock",
]);

const garageUssPath = "Assets/UI/UIToolkit/GarageSetB/GarageSetBWorkspace.uss";
const garageUss = readText(garageUssPath);
assertUssRuleContains(garageUss, garageUssPath, ".preview-card", [
  "height: 156px;",
  "min-height: 156px;",
  "max-height: 156px;",
]);
assertUssRuleContains(garageUss, garageUssPath, ".unit-diagram", [
  "width: 108px;",
  "height: 108px;",
  "margin-left: 24px;",
  "margin-top: 22px;",
]);
assertUssRuleContains(garageUss, garageUssPath, ".stat-radar-graph", [
  "position: absolute;",
  "width: 108px;",
  "height: 108px;",
]);
assertUssRuleContains(garageUss, garageUssPath, ".part-list-rows", [
  "height: 252px;",
  "min-height: 104px;",
  "flex-shrink: 0;",
]);
assertUssRuleContains(garageUss, garageUssPath, ".save-dock", [
  "position: relative;",
  "margin-top: 6px;",
  "flex-shrink: 0;",
]);

for (const owner of extractedOwners) {
  const text = readText(owner.path);
  for (const fragment of owner.requiredFragments) {
    if (!text.includes(fragment)) {
      issues.push(`Extracted owner lost responsibility marker: ${owner.path} fragment=${fragment}`);
    }
  }

  if (!fs.existsSync(`${absolute(owner.path)}.meta`)) {
    issues.push(`Missing Unity meta for extracted owner: ${owner.path}`);
  }
}

for (const check of reabsorptionChecks) {
  const text = readText(check.path);
  for (const fragment of check.blockedFragments) {
    if (text.includes(fragment)) {
      issues.push(`${check.path}: ${check.message} fragment=${fragment}`);
    }
  }
}

if (issues.length > 0) {
  console.error("Runtime hotspot structure check failed:");
  for (const issue of issues) {
    console.error(`  - ${issue}`);
  }
  process.exit(1);
}

console.log("Runtime hotspot structure check passed.");
