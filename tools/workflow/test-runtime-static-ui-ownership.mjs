import fs from "node:fs";
import path from "node:path";

const repoRoot = path.resolve(process.argv[2] ?? process.cwd());
const featuresRoot = path.join(repoRoot, "Assets", "Scripts", "Features");
const issues = [];

const forbiddenGeometryProperties = [
  "paddingLeft",
  "paddingRight",
  "paddingTop",
  "paddingBottom",
  "marginLeft",
  "marginRight",
  "marginTop",
  "marginBottom",
  "left",
  "right",
  "top",
  "bottom",
  "width",
  "height",
  "minWidth",
  "maxWidth",
  "minHeight",
  "maxHeight",
  "position",
  "flexBasis",
  "flexGrow",
  "flexShrink",
];

const geometryWriteRegex = new RegExp(
  String.raw`\.style\.(?:${forbiddenGeometryProperties.join("|")})\s*=`,
  "u",
);

function repoRelative(filePath) {
  return path.relative(repoRoot, filePath).replaceAll(path.sep, "/");
}

function listRuntimeAdapterFiles(directory) {
  const results = [];
  if (!fs.existsSync(directory)) {
    return results;
  }

  for (const entry of fs.readdirSync(directory, { withFileTypes: true })) {
    const entryPath = path.join(directory, entry.name);
    if (entry.isDirectory()) {
      results.push(...listRuntimeAdapterFiles(entryPath));
      continue;
    }

    if (entry.isFile() && entry.name.endsWith("RuntimeAdapter.cs")) {
      results.push(entryPath);
    }
  }

  return results;
}

for (const filePath of listRuntimeAdapterFiles(featuresRoot)) {
  const relativePath = repoRelative(filePath);
  if (!relativePath.includes("/Presentation/")) {
    continue;
  }

  const lines = fs.readFileSync(filePath, "utf8").split(/\r?\n/u);
  for (let index = 0; index < lines.length; index += 1) {
    const line = lines[index];
    if (!geometryWriteRegex.test(line)) {
      continue;
    }

    issues.push(
      `${relativePath}:${index + 1} RuntimeAdapter must not write static geometry style '${line.trim()}'. Move the layout contract to authored UXML/USS or a narrower dynamic surface owner.`,
    );
  }
}

if (issues.length > 0) {
  console.error("Runtime static UI ownership check failed:");
  for (const issue of issues) {
    console.error(`  - ${issue}`);
  }
  process.exit(1);
}

console.log("Runtime static UI ownership check passed.");
