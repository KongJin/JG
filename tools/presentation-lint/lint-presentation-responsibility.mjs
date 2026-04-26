import fs from "node:fs/promises";
import path from "node:path";
import process from "node:process";
import { fileURLToPath } from "node:url";

const repoRoot = path.resolve(fileURLToPath(new URL("../..", import.meta.url)));
const featuresRoot = path.join(repoRoot, "Assets", "Scripts", "Features");
const filePaths = await walkPresentationCsFiles(featuresRoot);
const findings = [];

for (const filePath of filePaths) {
  const content = await fs.readFile(filePath, "utf8");
  const classMatch = content.match(/\bclass\s+([A-Za-z_]\w*)\b/u);
  if (!classMatch) {
    continue;
  }

  const className = classMatch[1];
  if (!className.endsWith("PageController")) {
    continue;
  }

  const repoRelativePath = toRepoRelative(repoRoot, filePath);
  const lineCount = content.split(/\r?\n/u).length;
  const methodCount = countMethods(content);
  const serializedFieldCount = countSerializedFields(content);
  const useCaseFieldCount = countUseCaseFields(content);

  if (lineCount > 500) {
    findings.push({
      code: "page-controller-line-count",
      filePath: repoRelativePath,
      message: `Presentation PageController exceeds 500 lines (${lineCount}). Split orchestration, chrome, or adapter responsibilities out.`,
    });
  }

  if (methodCount > 28) {
    findings.push({
      code: "page-controller-method-count",
      filePath: repoRelativePath,
      message: `Presentation PageController exceeds 28 methods (${methodCount}). Move smoke, chrome, save, or input responsibilities to dedicated collaborators.`,
    });
  }

  if (serializedFieldCount > 24) {
    findings.push({
      code: "page-controller-serialized-fields",
      filePath: repoRelativePath,
      message: `Presentation PageController exceeds 24 serialized fields (${serializedFieldCount}). Group page chrome or move bindings behind dedicated views.`,
    });
  }

  if (useCaseFieldCount > 6) {
    findings.push({
      code: "page-controller-usecase-fields",
      filePath: repoRelativePath,
      message: `Presentation PageController exceeds 6 UseCase dependencies (${useCaseFieldCount}). Route orchestration through a dedicated coordinator or presenter-facing service.`,
    });
  }

  if (/\bWebglSmoke[A-Za-z0-9_]*\s*\(/u.test(content)) {
    findings.push({
      code: "page-controller-smoke-host",
      filePath: repoRelativePath,
      message: "Production PageController must not host WebGL/dev smoke entrypoints. Move them to a dedicated smoke bridge/driver.",
    });
  }

  if (/\bThemeColors\./u.test(content) || /\bButtonStyles\./u.test(content)) {
    findings.push({
      code: "page-controller-style-ownership",
      filePath: repoRelativePath,
      message: "Production PageController must not own theme/style rendering details. Move chrome styling to a dedicated view/controller.",
    });
  }

  for (const forbiddenType of extractForbiddenDependencyTypes(content)) {
    findings.push({
      code: "page-controller-forbidden-dependency",
      filePath: repoRelativePath,
      message: `Presentation PageController must not depend directly on ${forbiddenType}. Keep setup/adapter/port ownership out of page controllers.`,
    });
  }
}

if (findings.length === 0) {
  console.log(
    `Presentation responsibility lint passed. Checked ${filePaths.length} presentation C# file(s).`,
  );
  process.exit(0);
}

console.log(
  `Presentation responsibility lint failed with ${findings.length} issue(s) across ${filePaths.length} presentation C# file(s).`,
);
for (const finding of findings) {
  console.log(`- [${finding.code}] ${finding.filePath} - ${finding.message}`);
}

process.exit(1);

async function walkPresentationCsFiles(rootDir) {
  const entries = await fs.readdir(rootDir, { withFileTypes: true });
  const collected = [];

  for (const entry of entries) {
    const absolutePath = path.join(rootDir, entry.name);
    const repoRelativePath = toRepoRelative(repoRoot, absolutePath);

    if (entry.isDirectory()) {
      collected.push(...(await walkPresentationCsFiles(absolutePath)));
      continue;
    }

    if (!entry.isFile() || !entry.name.endsWith(".cs")) {
      continue;
    }

    if (!repoRelativePath.includes("/Presentation/")) {
      continue;
    }

    collected.push(absolutePath);
  }

  return collected.sort((left, right) => left.localeCompare(right));
}

function countMethods(content) {
  const matches = content.match(
    /^\s*(?:public|private|protected|internal)\s+(?:static\s+)?(?:async\s+)?(?:[A-Za-z0-9_<>\[\],\.\?]+\s+)+[A-Za-z_]\w*\s*\([^;\n]*\)\s*(?:\{|=>)/gmu,
  );
  return matches ? matches.length : 0;
}

function countSerializedFields(content) {
  const fieldMatches = content.matchAll(
    /(?:^\s*\[[^\]]*\]\s*)+(?:private|protected|public|internal)\s+[^;{}]+;/gmu,
  );
  let count = 0;

  for (const match of fieldMatches) {
    if (/\bSerializeField\b/u.test(match[0])) {
      count++;
    }
  }

  return count;
}

function countUseCaseFields(content) {
  const matches = content.match(/^\s*private\s+[A-Za-z0-9_<>\.\?]*UseCase\s+_[A-Za-z0-9_]+\s*;/gmu);
  return matches ? matches.length : 0;
}

function extractForbiddenDependencyTypes(content) {
  const matches = content.matchAll(
    /^\s*private\s+([A-Za-z0-9_<>\.\?]+)\s+_[A-Za-z0-9_]+\s*;/gmu,
  );
  const forbidden = new Set();

  for (const match of matches) {
    const typeName = match[1];
    if (/(?:Setup|Adapter|Port)$/.test(typeName)) {
      forbidden.add(typeName);
    }
  }

  return [...forbidden].sort((left, right) => left.localeCompare(right));
}

function toRepoRelative(repoRootPath, absolutePath) {
  return path.relative(repoRootPath, absolutePath).split(path.sep).join("/");
}
