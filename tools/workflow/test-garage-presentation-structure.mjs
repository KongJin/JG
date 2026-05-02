import fs from "node:fs";
import path from "node:path";

const repoRoot = path.resolve(process.argv[2] ?? process.cwd());
const featuresRoot = path.join(repoRoot, "Assets", "Scripts", "Features");
const allowedOwnerFolders = new Set([
  "Camera",
  "Collision",
  "Diagnostics",
  "Effects",
  "Formatting",
  "Input",
  "Page",
  "Placement",
  "Ports",
  "Preview",
  "Slots",
  "Summon",
  "Theme",
  "UI",
  "Uitk",
  "ViewModels",
  "Views",
]);

const issues = [];

function repoRelative(filePath) {
  return path.relative(repoRoot, filePath).replaceAll(path.sep, "/");
}

function listFilesRecursive(directory, predicate) {
  const results = [];
  if (!fs.existsSync(directory)) return results;

  for (const entry of fs.readdirSync(directory, { withFileTypes: true })) {
    const entryPath = path.join(directory, entry.name);
    if (entry.isDirectory()) {
      results.push(...listFilesRecursive(entryPath, predicate));
    } else if (!predicate || predicate(entryPath)) {
      results.push(entryPath);
    }
  }

  return results;
}

if (!fs.existsSync(featuresRoot)) {
  throw new Error(`Features root not found: ${featuresRoot}`);
}

const presentationRoots = listFilesRecursive(featuresRoot)
  .map((filePath) => {
    const parts = repoRelative(filePath).split("/");
    const presentationIndex = parts.indexOf("Presentation");
    if (presentationIndex < 0) return null;
    return path.join(repoRoot, ...parts.slice(0, presentationIndex + 1));
  })
  .filter(Boolean);
const uniquePresentationRoots = [...new Set(presentationRoots)];

for (const presentationRoot of uniquePresentationRoots) {
  for (const entry of fs.readdirSync(presentationRoot, { withFileTypes: true })) {
    const entryPath = path.join(presentationRoot, entry.name);
    if (entry.isFile() && entry.name.endsWith(".cs")) {
      issues.push(`Unexpected root Presentation script: ${repoRelative(entryPath)}`);
    }

    if (entry.isDirectory() && !allowedOwnerFolders.has(entry.name)) {
      issues.push(`Unexpected Presentation owner folder: ${repoRelative(entryPath)}`);
    }
  }
}

const projectPath = path.join(repoRoot, "Assembly-CSharp.csproj");
if (fs.existsSync(projectPath)) {
  const projectContent = fs.readFileSync(projectPath, "utf8");
  const presentationScripts = listFilesRecursive(featuresRoot, (filePath) =>
    repoRelative(filePath).includes("/Presentation/") && filePath.endsWith(".cs"),
  );

  for (const scriptPath of presentationScripts) {
    const relative = repoRelative(scriptPath);
    const expectedInclude = relative.replaceAll("/", "\\");
    const parts = relative.split("/");
    const feature = parts[3];
    const staleRootInclude = `Assets\\Scripts\\Features\\${feature}\\Presentation\\${path.basename(scriptPath)}`;
    if (projectContent.includes(staleRootInclude)) {
      issues.push(`Stale Assembly-CSharp.csproj Presentation include: ${staleRootInclude}`);
    }

    if (!projectContent.includes(expectedInclude)) {
      issues.push(`Missing Assembly-CSharp.csproj Presentation include: ${expectedInclude}`);
    }
  }
}

for (const scriptPath of listFilesRecursive(featuresRoot, (filePath) => {
  return repoRelative(filePath).includes("/Presentation/") && filePath.endsWith(".cs");
})) {
  if (!fs.existsSync(`${scriptPath}.meta`)) {
    issues.push(`Missing Unity meta for Presentation script: ${repoRelative(scriptPath)}`);
  }
}

if (issues.length > 0) {
  console.error("Feature Presentation structure check failed:");
  for (const issue of issues) {
    console.error(`  - ${issue}`);
  }
  process.exit(1);
}

console.log("Feature Presentation structure check passed.");
