import fs from "node:fs/promises";
import path from "node:path";
import process from "node:process";
import { fileURLToPath } from "node:url";

const repoRoot = path.resolve(fileURLToPath(new URL("../..", import.meta.url)));
const stitchUnityRoot = path.join(repoRoot, "tools", "stitch-unity");
const screenContractsRoot = path.join(repoRoot, ".stitch", "contracts", "screens");

const filePaths = await walkPs1Files(stitchUnityRoot);
const manifestPaths = await walkJsonFiles(screenContractsRoot);
const findings = [];

const checks = [
  {
    code: "stitch-script-fallback",
    message:
      "Stitch-driven scripts must not use script-side fallback defaults via `-Default`.",
    pattern:
      /\b(?:Get-OptionalProperty|Get-StitchUnityOptionalPropertyValue)\b[\s\S]{0,160}?\s-Default\b/gu,
  },
  {
    code: "stitch-script-literal-text",
    message:
      "Stitch-driven scripts must not hardcode UI text literals in MCP helper calls.",
    pattern:
      /\b(?:New-McpButton|New-McpText|Set-McpTmpStyle)\b[\s\S]{0,240}?\s-Text\s+"/gu,
  },
  {
    code: "stitch-script-literal-color",
    message:
      "Stitch-driven scripts must not hardcode UI color literals in MCP helper calls.",
    pattern:
      /\b(?:Set-McpImageColor|Set-McpRawImageColor|Set-McpTmpStyle)\b[\s\S]{0,240}?\s-Color\s+"(?:#[0-9A-Fa-f]{6,8}|[A-Za-z])/gu,
  },
  {
    code: "stitch-script-literal-size",
    message:
      "Stitch-driven scripts must not hardcode width, height, or font size literals in MCP helper calls.",
    pattern:
      /\b(?:New-McpPanel|New-McpRawImage|New-McpText|Set-McpTmpStyle)\b[\s\S]{0,240}?\s-(?:Width|Height|FontSize)\s+\d/gu,
  },
  {
    code: "stitch-script-literal-rect",
    message:
      "Stitch-driven scripts must not hardcode RectTransform literals in MCP helper calls.",
    pattern:
      /\bSet-McpRectTransform\b[\s\S]{0,240}?\s-(?:AnchorMin|AnchorMax|Pivot|AnchoredPosition|SizeDelta)\s+"/gu,
  },
];

for (const filePath of filePaths) {
  const content = await fs.readFile(filePath, "utf8");

  for (const check of checks) {
    for (const match of content.matchAll(check.pattern)) {
      findings.push({
        code: check.code,
        filePath: toRepoRelative(repoRoot, filePath),
        line: getLineNumber(content, match.index ?? 0),
        message: check.message,
        source: firstLine(match[0]),
      });
    }
  }
}

for (const manifestPath of manifestPaths) {
  if (path.basename(manifestPath) === "screen-manifest.template.json") {
    continue;
  }

  const manifest = JSON.parse(await fs.readFile(manifestPath, "utf8"));
  const manifestFindings = [];

  if (Object.hasOwn(manifest, "targets")) {
    manifestFindings.push({
      code: "stitch-manifest-forbidden-targets",
      message:
        "Screen manifests must not own runtime target paths. Keep target asset and host path data in unity-map.",
      path: "targets",
    });
  }

  collectForbiddenManifestKeys(manifest, [], manifestFindings, {
    unityTargetPath:
      "Screen manifests must not embed Unity target paths. Use unity-map hostPath bindings instead.",
    layout:
      "Screen manifests must not embed layout constants. Keep layout values out of Stitch-driven active inputs.",
    label:
      "Screen manifests must not embed presentation text labels in CTA entries.",
    frame:
      "Screen manifests must not embed fixed frame literals in validation.",
    smokeScripts:
      "Screen manifests must not embed smoke script path lists in validation.",
  });

  const requiredChecks = Array.isArray(manifest?.validation?.requiredChecks)
    ? manifest.validation.requiredChecks
    : [];
  for (let index = 0; index < requiredChecks.length; index += 1) {
    const value = requiredChecks[index];
    if (typeof value !== "string" || !/^[a-z0-9]+(?:-[a-z0-9]+)*$/u.test(value)) {
      manifestFindings.push({
        code: "stitch-manifest-required-check-id",
        message:
          "Screen manifest validation.requiredChecks entries must use semantic kebab-case ids.",
        path: `validation.requiredChecks[${index}]`,
      });
    }
  }

  for (const finding of manifestFindings) {
    findings.push({
      code: finding.code,
      filePath: toRepoRelative(repoRoot, manifestPath),
      line: 1,
      message: finding.message,
      source: finding.path,
    });
  }
}

const mapPaths = await walkJsonFiles(path.join(repoRoot, ".stitch", "contracts", "mappings"));
for (const mapPath of mapPaths) {
  const map = JSON.parse(await fs.readFile(mapPath, "utf8"));
  if (map?.translationStrategy === "unity-mcp-surface-generator-v1") {
    findings.push({
      code: "stitch-map-forbidden-legacy-strategy",
      filePath: toRepoRelative(repoRoot, mapPath),
      line: 1,
      message:
        "unity-mcp-surface-generator-v1 is forbidden. Use the contract-complete translator lane only.",
      source: "translationStrategy",
    });
  }
}

if (findings.length === 0) {
  console.log(
    `Stitch policy lint passed. Checked ${filePaths.length} PowerShell file(s), ${manifestPaths.length} manifest file(s), and ${mapPaths.length} map file(s).`,
  );
  process.exit(0);
}

console.log(
  `Stitch policy lint failed with ${findings.length} issue(s) across ${filePaths.length} PowerShell file(s), ${manifestPaths.length} manifest file(s), and ${mapPaths.length} map file(s).`,
);
for (const finding of findings) {
  console.log(
    `- [${finding.code}] ${finding.filePath}:${finding.line} - ${finding.message}`,
  );
}

process.exit(1);

async function walkPs1Files(rootDir) {
  const entries = await fs.readdir(rootDir, { withFileTypes: true });
  const collected = [];

  for (const entry of entries) {
    const absolutePath = path.join(rootDir, entry.name);
    if (entry.isDirectory()) {
      collected.push(...(await walkPs1Files(absolutePath)));
      continue;
    }

    if (entry.isFile() && entry.name.endsWith(".ps1")) {
      collected.push(absolutePath);
    }
  }

  return collected.sort((left, right) => left.localeCompare(right));
}

async function walkJsonFiles(rootDir) {
  const entries = await fs.readdir(rootDir, { withFileTypes: true });
  const collected = [];

  for (const entry of entries) {
    const absolutePath = path.join(rootDir, entry.name);
    if (entry.isDirectory()) {
      collected.push(...(await walkJsonFiles(absolutePath)));
      continue;
    }

    if (entry.isFile() && entry.name.endsWith(".json")) {
      collected.push(absolutePath);
    }
  }

  return collected.sort((left, right) => left.localeCompare(right));
}

function toRepoRelative(repoRootPath, absolutePath) {
  return path.relative(repoRootPath, absolutePath).split(path.sep).join("/");
}

function getLineNumber(content, index) {
  return content.slice(0, index).split(/\r?\n/).length;
}

function firstLine(value) {
  return value.split(/\r?\n/)[0].trim();
}

function collectForbiddenManifestKeys(value, segments, findings, forbiddenKeys) {
  if (Array.isArray(value)) {
    value.forEach((entry, index) => {
      collectForbiddenManifestKeys(entry, [...segments, `[${index}]`], findings, forbiddenKeys);
    });
    return;
  }

  if (!value || typeof value !== "object") {
    return;
  }

  for (const [key, entry] of Object.entries(value)) {
    const nextSegments = [...segments, key];
    if (Object.hasOwn(forbiddenKeys, key)) {
      findings.push({
        code: `stitch-manifest-forbidden-${key}`,
        message: forbiddenKeys[key],
        path: formatManifestPath(nextSegments),
      });
    }

    collectForbiddenManifestKeys(entry, nextSegments, findings, forbiddenKeys);
  }
}

function formatManifestPath(segments) {
  return segments.reduce((pathValue, segment) => {
    if (segment.startsWith("[")) {
      return `${pathValue}${segment}`;
    }

    return pathValue.length === 0 ? segment : `${pathValue}.${segment}`;
  }, "");
}
