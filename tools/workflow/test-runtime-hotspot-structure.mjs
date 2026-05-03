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
    maxLines: 460,
    owner: "Lobby runtime adapter",
  },
  {
    path: "Assets/Scripts/Features/Player/GameSceneRoot.cs",
    maxLines: 300,
    owner: "GameSceneRoot",
  },
];

const extractedOwners = [
  {
    path: "Assets/Scripts/Features/Garage/Presentation/Uitk/GarageSetBUitkLayoutController.cs",
    requiredFragments: ["GeometryChangedEvent", "PointerMoveEvent", "worldBound"],
  },
  {
    path: "Assets/Scripts/Features/Lobby/Presentation/Uitk/LobbyOperationMemoryRenderer.cs",
    requiredFragments: ["RenderLatestOperation", "memory-stat-grid", "memory-sitrep"],
  },
  {
    path: "Assets/Scripts/Features/Player/GameSceneLocalPlayerInitializationFlow.cs",
    requiredFragments: [
      "InitialEnergyValidator",
      "InitializeBattleEntity(",
      "EnergyAdapterInstance.GetCurrentEnergy",
    ],
  },
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
    path: "Assets/Scripts/Features/Player/GameSceneRoot.cs",
    message: "GameSceneRoot must not reabsorb local-player battle initialization details.",
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

for (const budget of hotspotBudgets) {
  const text = readText(budget.path);
  const lineCount = countLines(text);
  if (lineCount > budget.maxLines) {
    issues.push(
      `${budget.owner} exceeded responsibility budget: ${budget.path} lines=${lineCount} budget=${budget.maxLines}`,
    );
  }
}

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
