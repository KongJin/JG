import fs from "node:fs";
import path from "node:path";

const repoRoot = path.resolve(process.argv[2] ?? process.cwd());
const featuresRoot = path.join(repoRoot, "Assets", "Scripts", "Features");

// Layer 검사 매트릭스: 각 layer 폴더가 import 금지하는 inner-feature 레이어 목록.
// 허용 방향: Presentation -> Application -> Domain, Infrastructure -> {Application, Domain}.
const forbiddenByLayer = {
  Domain: ["Application", "Presentation", "Infrastructure"],
  Application: ["Presentation", "Infrastructure"],
  Presentation: ["Infrastructure"],
  Infrastructure: ["Presentation"],
};

const layerNames = Object.keys(forbiddenByLayer);

// 자동 예외 폴더 (Presentation 내부 진단용 owner). repo-relative path 부분 매칭.
const autoExemptFolderFragments = ["/Presentation/Diagnostics/"];

function repoRelative(filePath) {
  return path.relative(repoRoot, filePath).replaceAll(path.sep, "/");
}

function listCsFilesRecursive(directory) {
  const results = [];
  if (!fs.existsSync(directory)) return results;
  for (const entry of fs.readdirSync(directory, { withFileTypes: true })) {
    const entryPath = path.join(directory, entry.name);
    if (entry.isDirectory()) {
      results.push(...listCsFilesRecursive(entryPath));
    } else if (entry.isFile() && entry.name.endsWith(".cs")) {
      results.push(entryPath);
    }
  }
  return results;
}

function classifyFile(relativePath) {
  // 기대 형태: Assets/Scripts/Features/<Feature>/<Layer>/...
  const parts = relativePath.split("/");
  if (parts.length < 5) return null;
  if (parts[0] !== "Assets" || parts[1] !== "Scripts" || parts[2] !== "Features") return null;

  const feature = parts[3];
  const layerCandidate = parts[4];

  // 피처 루트 직속 파일 (Assets/Scripts/Features/<Feature>/<file>.cs).
  // 컴포지션 루트 (*Setup.cs, *SceneRoot.cs, *Bootstrap*.cs 등) 자동 예외.
  if (parts.length === 5) {
    return { feature, layer: null, isCompositionRoot: true };
  }

  if (!layerNames.includes(layerCandidate)) {
    return { feature, layer: null, isCompositionRoot: false };
  }

  return { feature, layer: layerCandidate, isCompositionRoot: false };
}

function isAutoExempt(relativePath) {
  for (const fragment of autoExemptFolderFragments) {
    if (relativePath.includes(fragment)) return true;
  }
  return false;
}

function extractFeatureLayerImports(text) {
  const usingRegex = /^using\s+Features\.(\w+)\.(Application|Presentation|Infrastructure|Domain)(?:\.[\w.]+)?\s*;/gm;
  const found = [];
  let match;
  while ((match = usingRegex.exec(text)) !== null) {
    found.push({
      raw: match[0],
      feature: match[1],
      layer: match[2],
      lineIndex: text.slice(0, match.index).split(/\r?\n/).length,
    });
  }
  return found;
}

const issues = [];

if (!fs.existsSync(featuresRoot)) {
  console.error(`Features root not found: ${featuresRoot}`);
  process.exit(1);
}

const csFiles = listCsFilesRecursive(featuresRoot);
let inspected = 0;

for (const filePath of csFiles) {
  const relative = repoRelative(filePath);
  if (isAutoExempt(relative)) continue;

  const classification = classifyFile(relative);
  if (!classification) continue;
  if (classification.isCompositionRoot) continue;
  if (classification.layer === null) continue;

  const text = fs.readFileSync(filePath, "utf8");
  const imports = extractFeatureLayerImports(text);
  const forbidden = forbiddenByLayer[classification.layer];
  inspected += 1;

  for (const imp of imports) {
    if (forbidden.includes(imp.layer)) {
      issues.push(
        `${relative}: ${classification.layer} cannot depend on Features.${imp.feature}.${imp.layer} (line ${imp.lineIndex})`,
      );
    }
  }
}

if (issues.length > 0) {
  console.error("Feature layer dependency check failed:");
  for (const issue of issues) {
    console.error(`  - ${issue}`);
  }
  console.error("");
  console.error("Allowed direction: Presentation -> Application -> Domain; Infrastructure -> {Application, Domain}.");
  console.error("Auto-exempt: feature root files (Assets/Scripts/Features/<Feature>/<file>.cs) and Presentation/Diagnostics/**.");
  process.exit(1);
}

console.log(`Feature layer dependency check passed. Inspected ${inspected} file(s).`);
