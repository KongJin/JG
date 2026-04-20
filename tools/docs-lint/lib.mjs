import fs from "node:fs/promises";
import path from "node:path";

export const REQUIRED_METADATA_FIELDS = [
  "상태",
  "doc_id",
  "role",
  "owner_scope",
  "upstream",
  "artifacts",
];

export const VALID_STATUS_VALUES = new Set([
  "active",
  "draft",
  "reference",
  "historical",
  "paused",
]);

export const VALID_ROLE_VALUES = new Set([
  "entry",
  "skill-entry",
  "ssot",
  "plan",
  "reference",
  "historical",
]);

const INDEX_STATUS_LINE_PATTERN =
  /^-\s+`(active|draft|reference|historical|paused)`:\s+\[[^\]]+\]\(([^)]+)\)/;
const MARKDOWN_LINK_PATTERN = /!?\[[^\]]*]\(([^)]+)\)/g;
const INLINE_CODE_PATTERN = /`([^`\n]+)`/g;
const REPO_PATH_PREFIXES = [
  "AGENTS.md",
  "docs/",
  "tools/",
  ".agents/",
  "Assets/",
  "ProjectSettings/",
  "Packages/",
  "Tests/",
  "Build/",
  ".stitch/",
  ".codex/",
  "artifacts/",
  "plugins/",
];
const SKILL_LOCAL_PREFIXES = [
  "./",
  "../",
  "references/",
  "scripts/",
  "assets/",
  "agents/",
  "hooks/",
  "skills/",
  ".codex-plugin/",
  ".mcp.json",
  ".app.json",
];

export async function lintRepository(repoRoot) {
  const managedDocPaths = await discoverManagedDocs(repoRoot);
  const documents = [];
  const errors = [];

  for (const absolutePath of managedDocPaths) {
    const content = await fs.readFile(absolutePath, "utf8");
    const metadata = parseMetadata(content);
    const document = {
      absolutePath,
      repoRelativePath: toRepoRelative(repoRoot, absolutePath),
      content,
      metadata,
    };

    documents.push(document);
    errors.push(...validateMetadata(document));
    errors.push(...(await validateLinks(document)));
    errors.push(...(await validateSkillInlinePaths(document, repoRoot)));
  }

  errors.push(...validateUniqueDocIds(documents));
  errors.push(...validateIndexStatusLabels(documents, repoRoot));

  return {
    managedDocPaths: managedDocPaths.map((absolutePath) =>
      toRepoRelative(repoRoot, absolutePath),
    ),
    documents,
    errors: sortErrors(errors),
  };
}

export function formatLintReport(result) {
  if (result.errors.length === 0) {
    return `Docs lint passed. Checked ${result.managedDocPaths.length} managed document(s).`;
  }

  const lines = [
    `Docs lint failed with ${result.errors.length} issue(s) across ${result.managedDocPaths.length} managed document(s).`,
  ];

  for (const error of result.errors) {
    const location = error.line
      ? `${error.path}:${error.line}`
      : error.path;
    lines.push(`- [${error.code}] ${location} - ${error.message}`);
  }

  return lines.join("\n");
}

async function discoverManagedDocs(repoRoot) {
  const discovered = new Set();

  await addIfExists(discovered, path.join(repoRoot, "AGENTS.md"));

  for (const markdownFile of await walkMarkdownFiles(path.join(repoRoot, "docs"), repoRoot)) {
    discovered.add(markdownFile);
  }

  const toolsDir = path.join(repoRoot, "tools");
  if (await pathExists(toolsDir)) {
    const entries = await fs.readdir(toolsDir, { withFileTypes: true });
    for (const entry of entries) {
      if (!entry.isDirectory()) {
        continue;
      }

      await addIfExists(discovered, path.join(toolsDir, entry.name, "README.md"));
    }
  }

  const skillsDir = path.join(repoRoot, ".codex", "skills");
  if (await pathExists(skillsDir)) {
    const entries = await fs.readdir(skillsDir, { withFileTypes: true });
    for (const entry of entries) {
      if (entry.isDirectory() && entry.name.startsWith("jg-")) {
        const skillRoot = path.join(skillsDir, entry.name);
        await addIfExists(discovered, path.join(skillRoot, "SKILL.md"));

        const referencesDir = path.join(skillRoot, "references");
        for (const markdownFile of await walkMarkdownFiles(referencesDir, repoRoot)) {
          discovered.add(markdownFile);
        }
      }

      if (entry.isDirectory() && entry.name === ".system") {
        const systemEntries = await fs.readdir(path.join(skillsDir, entry.name), {
          withFileTypes: true,
        });

        for (const systemEntry of systemEntries) {
          if (!systemEntry.isDirectory()) {
            continue;
          }

          await addIfExists(
            discovered,
            path.join(skillsDir, entry.name, systemEntry.name, "SKILL.md"),
          );
        }
      }
    }
  }

  return [...discovered].sort((left, right) => left.localeCompare(right));
}

async function walkMarkdownFiles(rootDir, repoRoot) {
  if (!(await pathExists(rootDir))) {
    return [];
  }

  const collected = [];
  const entries = await fs.readdir(rootDir, { withFileTypes: true });

  for (const entry of entries) {
    const absolutePath = path.join(rootDir, entry.name);
    const repoRelativePath = toRepoRelative(repoRoot, absolutePath);

    if (repoRelativePath.startsWith("node_modules/")) {
      continue;
    }

    if (repoRelativePath.startsWith("Library/") || repoRelativePath.startsWith("Temp/")) {
      continue;
    }

    if (/^tools\/.+\/tests\/fixtures\//.test(repoRelativePath)) {
      continue;
    }

    if (entry.isDirectory()) {
      collected.push(...(await walkMarkdownFiles(absolutePath, repoRoot)));
      continue;
    }

    if (entry.isFile() && entry.name.endsWith(".md")) {
      collected.push(absolutePath);
    }
  }

  return collected;
}

function parseMetadata(content) {
  const metadata = new Map();
  const lines = content.split(/\r?\n/).slice(0, 40);

  for (const line of lines) {
    const match = line.match(/^>\s*([^:]+):\s*(.*)$/);
    if (!match) {
      continue;
    }

    metadata.set(match[1].trim(), match[2].trim());
  }

  return metadata;
}

function validateMetadata(document) {
  const errors = [];
  if (getDocumentKind(document.repoRelativePath) === "system-skill") {
    return errors;
  }

  for (const field of REQUIRED_METADATA_FIELDS) {
    if (!document.metadata.has(field)) {
      errors.push(
        createError(
          "missing-meta",
          document.repoRelativePath,
          `Missing required metadata field \`${field}\`.`,
        ),
      );
    }
  }

  const status = document.metadata.get("상태");
  if (status && !VALID_STATUS_VALUES.has(status)) {
    errors.push(
      createError(
        "invalid-status",
        document.repoRelativePath,
        `Invalid 상태 value \`${status}\`. Expected one of: ${[...VALID_STATUS_VALUES].join(", ")}.`,
      ),
    );
  }

  const role = document.metadata.get("role");
  if (role && !VALID_ROLE_VALUES.has(role)) {
    errors.push(
      createError(
        "invalid-role",
        document.repoRelativePath,
        `Invalid role value \`${role}\`. Expected one of: ${[...VALID_ROLE_VALUES].join(", ")}.`,
      ),
    );
  }

  if (isRepoLocalSkillEntry(document.repoRelativePath) && role && role !== "skill-entry") {
    errors.push(
      createError(
        "path-role-mismatch",
        document.repoRelativePath,
        `Repo-local SKILL.md must use \`role: skill-entry\`, found \`${role}\`.`,
      ),
    );
  }

  return errors;
}

async function validateLinks(document) {
  const errors = [];
  const lines = stripFencedCodeBlocks(document.content).split(/\r?\n/);

  for (let index = 0; index < lines.length; index += 1) {
    const line = lines[index];
    for (const target of extractRelativeMarkdownTargets(line)) {
      const resolvedPath = path.resolve(path.dirname(document.absolutePath), target);
      if (!(await pathExists(resolvedPath))) {
        errors.push(
          createError(
            "broken-relative-link",
            document.repoRelativePath,
            `Relative link target \`${target}\` does not exist.`,
            index + 1,
          ),
        );
      }
    }
  }

  return errors;
}

async function validateSkillInlinePaths(document, repoRoot) {
  const documentKind = getDocumentKind(document.repoRelativePath);
  if (documentKind !== "repo-skill" && documentKind !== "system-skill") {
    return [];
  }

  const errors = [];
  const lines = stripFencedCodeBlocks(document.content).split(/\r?\n/);

  for (let index = 0; index < lines.length; index += 1) {
    const line = lines[index];
    for (const token of extractInlineCodeTokens(line)) {
      if (documentKind === "repo-skill" && /^(\/)?agent\//.test(token)) {
        errors.push(
          createError(
            "legacy-owner-path",
            document.repoRelativePath,
            `Repo-local skill must route through \`docs/index.md\` and owner docs instead of legacy path \`${token}\`.`,
            index + 1,
          ),
        );
        continue;
      }

      const resolved = resolveInlinePathToken(token, document, repoRoot);
      if (!resolved) {
        continue;
      }

      if (!(await pathExists(resolved.absolutePath))) {
        errors.push(
          createError(
            "missing-inline-path",
            document.repoRelativePath,
            `Inline path token \`${token}\` does not resolve to an existing path.`,
            index + 1,
          ),
        );
      }
    }
  }

  return errors;
}

function validateUniqueDocIds(documents) {
  const docIdMap = new Map();
  const errors = [];

  for (const document of documents) {
    const docId = document.metadata.get("doc_id");
    if (!docId) {
      continue;
    }

    const owners = docIdMap.get(docId) || [];
    owners.push(document.repoRelativePath);
    docIdMap.set(docId, owners);
  }

  for (const [docId, owners] of docIdMap.entries()) {
    if (owners.length < 2) {
      continue;
    }

    const message = `Duplicate doc_id \`${docId}\` is used by: ${owners.join(", ")}.`;
    for (const owner of owners) {
      errors.push(createError("duplicate-doc-id", owner, message));
    }
  }

  return errors;
}

function validateIndexStatusLabels(documents, repoRoot) {
  const indexDocument = documents.find(
    (document) => document.repoRelativePath === "docs/index.md",
  );
  if (!indexDocument) {
    return [];
  }

  const metadataByAbsolutePath = new Map(
    documents.map((document) => [document.absolutePath, document.metadata]),
  );
  const errors = [];
  const lines = indexDocument.content.split(/\r?\n/);

  for (let index = 0; index < lines.length; index += 1) {
    const line = lines[index];
    const match = line.match(INDEX_STATUS_LINE_PATTERN);
    if (!match) {
      continue;
    }

    const expectedStatus = match[1];
    const target = normalizeMarkdownTarget(match[2]);
    if (!target || !isRelativeTarget(target)) {
      continue;
    }

    const resolvedPath = path.resolve(path.dirname(indexDocument.absolutePath), target);
    const targetMetadata = metadataByAbsolutePath.get(resolvedPath);
    if (!targetMetadata) {
      continue;
    }

    const actualStatus = targetMetadata.get("상태");
    if (actualStatus !== expectedStatus) {
      errors.push(
        createError(
          "index-status-mismatch",
          indexDocument.repoRelativePath,
          `Index status label \`${expectedStatus}\` for \`${toRepoRelative(repoRoot, resolvedPath)}\` does not match document 상태 \`${actualStatus || "missing"}\`.`,
          index + 1,
        ),
      );
    }
  }

  return errors;
}

function extractRelativeMarkdownTargets(line) {
  const targets = [];
  for (const match of line.matchAll(MARKDOWN_LINK_PATTERN)) {
    const target = normalizeMarkdownTarget(match[1]);
    if (!target || !isRelativeTarget(target)) {
      continue;
    }

    targets.push(target);
  }

  return targets;
}

function extractInlineCodeTokens(line) {
  const tokens = [];
  for (const match of line.matchAll(INLINE_CODE_PATTERN)) {
    const token = match[1].trim();
    if (
      !token ||
      token.includes(" ") ||
      token.includes("<") ||
      token.includes(">") ||
      token.includes("*") ||
      token.includes("{") ||
      token.includes("}")
    ) {
      continue;
    }

    tokens.push(token);
  }

  return tokens;
}

function normalizeMarkdownTarget(rawTarget) {
  let target = rawTarget.trim();
  if (!target) {
    return null;
  }

  if (target.startsWith("<") && target.endsWith(">")) {
    target = target.slice(1, -1).trim();
  }

  if (!target) {
    return null;
  }

  if (!target.startsWith("<") && /\s+['"]/.test(target)) {
    target = target.split(/\s+['"]/u)[0];
  }

  const hashIndex = target.indexOf("#");
  if (hashIndex >= 0) {
    target = target.slice(0, hashIndex);
  }

  const queryIndex = target.indexOf("?");
  if (queryIndex >= 0) {
    target = target.slice(0, queryIndex);
  }

  return target || null;
}

function stripFencedCodeBlocks(content) {
  const stripped = [];
  let insideFence = false;

  for (const line of content.split(/\r?\n/)) {
    if (line.trimStart().startsWith("```")) {
      insideFence = !insideFence;
      stripped.push("");
      continue;
    }

    stripped.push(insideFence ? "" : line);
  }

  return stripped.join("\n");
}

function isRelativeTarget(target) {
  if (!target || target.startsWith("#") || target.startsWith("/")) {
    return false;
  }

  return !/^[a-zA-Z][a-zA-Z0-9+.-]*:/.test(target);
}

function isRepoLocalSkillEntry(repoRelativePath) {
  return /^\.codex\/skills\/jg-[^/]+\/SKILL\.md$/.test(repoRelativePath);
}

function getDocumentKind(repoRelativePath) {
  if (/^\.codex\/skills\/jg-[^/]+\/SKILL\.md$/.test(repoRelativePath)) {
    return "repo-skill";
  }

  if (/^\.codex\/skills\/jg-[^/]+\/references\/.+\.md$/.test(repoRelativePath)) {
    return "repo-skill-reference";
  }

  if (/^\.codex\/skills\/\.system\/[^/]+\/SKILL\.md$/.test(repoRelativePath)) {
    return "system-skill";
  }

  return "managed-doc";
}

function resolveInlinePathToken(token, document, repoRoot) {
  const normalized = token.startsWith("/") ? token.slice(1) : token;
  const documentKind = getDocumentKind(document.repoRelativePath);

  if (
    REPO_PATH_PREFIXES.some(
      (prefix) => normalized === prefix || normalized.startsWith(prefix),
    )
  ) {
    return {
      absolutePath: path.resolve(repoRoot, normalized),
    };
  }

  if (documentKind === "system-skill") {
    return null;
  }

  if (
    SKILL_LOCAL_PREFIXES.some(
      (prefix) => token === prefix || token.startsWith(prefix),
    )
  ) {
    return {
      absolutePath: path.resolve(path.dirname(document.absolutePath), token),
    };
  }

  return null;
}

function createError(code, repoRelativePath, message, line = null) {
  return {
    code,
    line,
    message,
    path: repoRelativePath,
  };
}

function sortErrors(errors) {
  return [...errors].sort((left, right) => {
    const pathComparison = left.path.localeCompare(right.path);
    if (pathComparison !== 0) {
      return pathComparison;
    }

    const lineComparison = (left.line || 0) - (right.line || 0);
    if (lineComparison !== 0) {
      return lineComparison;
    }

    return left.code.localeCompare(right.code);
  });
}

async function addIfExists(collection, absolutePath) {
  if (await pathExists(absolutePath)) {
    collection.add(absolutePath);
  }
}

async function pathExists(absolutePath) {
  try {
    await fs.access(absolutePath);
    return true;
  } catch {
    return false;
  }
}

function toRepoRelative(repoRoot, absolutePath) {
  return toPosixPath(path.relative(repoRoot, absolutePath));
}

function toPosixPath(targetPath) {
  return targetPath.split(path.sep).join("/");
}
