import fs from "node:fs/promises";
import path from "node:path";
import process from "node:process";

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
const DEPRECATED_REPO_SKILL_INLINE_PREFIXES = [
  ".stitch/designs/",
  ".stitch/handoff/",
];
const DOC_ID_REFERENCE_PATTERN =
  /^(repo|docs|ops|design|plans|playtest|discussions|tools|skill|historical)\.[a-z0-9]+(?:[-.][a-z0-9]+)*$/;
const PLAN_MODE_PATTERNS = [/\bPlan Mode\b/u, /Plan 모드/u];
const RULE_OPERATIONS_PATTERNS = [/\brule-operations\b/u];
const COHESION_COUPLING_OWNER_PATTERNS = [/\bops\.cohesion-coupling-policy\b/u];
const MUTATION_FORBIDDEN_PATTERNS = [
  /mutation\s*금지/u,
  /mutation을\s*금지/u,
  /Do not mutate/u,
  /must not mutate/u,
];
const INSPECTION_REFERENCE_PATTERNS = [
  /inspection\/reference/u,
  /inspection only/u,
  /reference only/u,
];
const MUTATING_REPO_SKILL_ENTRIES = new Set([
  ".codex/skills/jg-unity-workflow/SKILL.md",
  ".codex/skills/jg-stitch-workflow/SKILL.md",
  ".codex/skills/jg-stitch-unity-import/SKILL.md",
]);
export const RECURRENCE_CLOSEOUT_PATH = "artifacts/rules/issue-recurrence-closeout.json";
export const RECURRENCE_CHANGED_FILES_ENV = "RULES_LINT_CHANGED_FILES";

export async function lintRepository(repoRoot, options = {}) {
  const includeGeneralChecks = options.includeGeneralChecks ?? true;
  const includePolicyChecks = options.includePolicyChecks ?? true;
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
    if (includeGeneralChecks) {
      errors.push(...validateMetadata(document));
      errors.push(...(await validateLinks(document)));
      errors.push(...(await validateSkillInlinePaths(document, repoRoot)));
      errors.push(...(await validateContractArtifactReferences(document, repoRoot)));
      errors.push(...validateActiveDocHistoricalLinks(document));
      errors.push(...validateRepoSkillHistoricalMentions(document));
      errors.push(...validateDocIdPathPrefix(document));
      errors.push(...validateCompletedDraftPlan(document));
    }
    if (includePolicyChecks) {
      errors.push(...validatePlanModeRouting(document));
    }
  }

  if (includeGeneralChecks) {
    errors.push(...validateUniqueDocIds(documents));
    errors.push(...validateKnownDocIdReferences(documents));
    errors.push(...validateIndexCoverage(documents, repoRoot));
    errors.push(...validateIndexStatusLabels(documents, repoRoot));
  }

  if (includePolicyChecks) {
    errors.push(...(await validateRulesOnlyRecurrenceCloseout(repoRoot, options)));
  }

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
  if (documentKind !== "repo-skill") {
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

      if (
        documentKind === "repo-skill" &&
        DEPRECATED_REPO_SKILL_INLINE_PREFIXES.some((prefix) => token.startsWith(prefix))
      ) {
        errors.push(
          createError(
            "deprecated-skill-inline-path",
            document.repoRelativePath,
            `Repo-local skill must not reference deprecated historical path \`${token}\`. Route through active owner docs and \`.stitch/contracts/*.json\` instead.`,
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

async function validateContractArtifactReferences(document, repoRoot) {
  const errors = [];
  const lines = stripFencedCodeBlocks(document.content).split(/\r?\n/);

  for (let index = 0; index < lines.length; index += 1) {
    const line = lines[index];

    for (const target of extractRelativeMarkdownTargets(line)) {
      const normalizedTarget = target.replace(/\\/g, "/");
      if (!isConcreteContractArtifactPath(normalizedTarget)) {
        continue;
      }

      const resolvedPath = path.resolve(path.dirname(document.absolutePath), normalizedTarget);
      if (!(await pathExists(resolvedPath))) {
        errors.push(
          createError(
            "missing-contract-artifact",
            document.repoRelativePath,
            `Concrete contract artifact \`${normalizedTarget}\` does not exist.`,
            index + 1,
          ),
        );
      }
    }

    for (const token of extractInlineCodeTokens(line)) {
      const normalizedToken = token.replace(/\\/g, "/");
      if (!isConcreteContractArtifactPath(normalizedToken)) {
        continue;
      }

      const resolved = resolveInlinePathToken(normalizedToken, document, repoRoot);
      if (!resolved || !(await pathExists(resolved.absolutePath))) {
        errors.push(
          createError(
            "missing-contract-artifact",
            document.repoRelativePath,
            `Concrete contract artifact \`${normalizedToken}\` does not exist.`,
            index + 1,
          ),
        );
      }
    }
  }

  return errors;
}

function validateActiveDocHistoricalLinks(document) {
  const status = document.metadata.get("상태");
  if (status !== "active") {
    return [];
  }

  const errors = [];
  const lines = stripFencedCodeBlocks(document.content).split(/\r?\n/);

  for (let index = 0; index < lines.length; index += 1) {
    const line = lines[index];
    for (const target of extractRelativeMarkdownTargets(line)) {
      const normalizedTarget = target.replace(/\\/g, "/");
      if (
        normalizedTarget.includes(".stitch/handoff/") ||
        normalizedTarget.includes(".stitch/designs/")
      ) {
        errors.push(
          createError(
            "historical-link-in-active-doc",
            document.repoRelativePath,
            `Active document must not link directly to historical Stitch artifact \`${normalizedTarget}\`. Route through active owner docs or \`.stitch/contracts/*.json\` instead.`,
            index + 1,
          ),
        );
      }
    }
  }

  return errors;
}

function validateRepoSkillHistoricalMentions(document) {
  if (getDocumentKind(document.repoRelativePath) !== "repo-skill") {
    return [];
  }

  const errors = [];
  const lines = stripFencedCodeBlocks(document.content).split(/\r?\n/);

  for (let index = 0; index < lines.length; index += 1) {
    const line = lines[index];
    if (line.includes(".stitch/designs/") || line.includes(".stitch/handoff/")) {
      errors.push(
        createError(
          "deprecated-skill-path-mention",
          document.repoRelativePath,
          "Repo-local skill must not restate deprecated historical Stitch paths. Keep those details in owner docs only.",
          index + 1,
        ),
      );
    }
  }

  return errors;
}

function validateDocIdPathPrefix(document) {
  const docId = document.metadata.get("doc_id");
  if (!docId) {
    return [];
  }

  const expectedPrefix = getExpectedDocIdPrefix(document.repoRelativePath);
  if (!expectedPrefix || docId.startsWith(expectedPrefix)) {
    return [];
  }

  return [
    createError(
      "doc-id-path-prefix-mismatch",
      document.repoRelativePath,
      `Document path expects doc_id prefix \`${expectedPrefix}\`, found \`${docId}\`.`,
    ),
  ];
}

function validateCompletedDraftPlan(document) {
  const status = document.metadata.get("상태");
  const role = document.metadata.get("role");
  if (status !== "draft" || role !== "plan") {
    return [];
  }

  const content = stripFencedCodeBlocks(document.content);
  const completionPatterns = [
    /^## 최종 결정$/mu,
    /^## Rereview$/mu,
    /^-\s*closeout:\s*완료\s*$/mu,
    /^상태:\s*완료\s*$/mu,
    /plan rereview:\s*clean/u,
  ];

  if (!completionPatterns.some((pattern) => pattern.test(content))) {
    return [];
  }

  return [
    createError(
      "completed-draft-plan",
      document.repoRelativePath,
      "Draft plan contains closeout/final-decision wording. Move it to `reference`, `historical`, or remove the completion wording.",
    ),
  ];
}

function validatePlanModeRouting(document) {
  if (document.repoRelativePath === "AGENTS.md") {
    return validateDocumentContainsAll(
      document,
      [
        { patterns: PLAN_MODE_PATTERNS, label: "Plan Mode wording" },
        { patterns: RULE_OPERATIONS_PATTERNS, label: "`rule-operations` routing" },
        { patterns: MUTATION_FORBIDDEN_PATTERNS, label: "explicit mutation prohibition" },
      ],
      "missing-plan-mode-routing",
      "AGENTS.md must route Plan Mode or Codex operations through `docs/index.md` and `rule-operations`, and explicitly forbid mutation in that lane.",
    );
  }

  if (document.repoRelativePath === "docs/index.md") {
    return validateDocumentContainsAll(
      document,
      [
        { patterns: PLAN_MODE_PATTERNS, label: "Plan Mode wording" },
        { patterns: RULE_OPERATIONS_PATTERNS, label: "`rule-operations` owner route" },
      ],
      "missing-plan-mode-owner-route",
      "docs/index.md must expose a current route for Plan Mode or Codex operations through the `rule-operations` owner docs.",
    );
  }

  if (MUTATING_REPO_SKILL_ENTRIES.has(document.repoRelativePath)) {
    return [
      ...validateDocumentContainsAll(
        document,
        [
          {
            patterns: COHESION_COUPLING_OWNER_PATTERNS,
            label: "`ops.cohesion-coupling-policy` owner route",
          },
        ],
        "missing-skill-owner-route",
        "Repo-local skill-entry must route responsibility, cohesion, and coupling judgments through `ops.cohesion-coupling-policy` before lane-specific owner docs.",
      ),
      ...validateDocumentContainsAll(
      document,
      [
        { patterns: PLAN_MODE_PATTERNS, label: "Plan Mode wording" },
        { patterns: INSPECTION_REFERENCE_PATTERNS, label: "inspection/reference clause" },
        { patterns: MUTATION_FORBIDDEN_PATTERNS, label: "explicit mutation prohibition" },
      ],
      "missing-skill-inspection-clause",
      "Repo-local mutating skill-entry must state that Plan Mode is inspection/reference only and mutation is forbidden from that lane.",
      ),
    ];
  }

  return [];
}

async function validateRulesOnlyRecurrenceCloseout(repoRoot, options) {
  const changedFiles = getRecurrenceChangedFiles(options);
  const relevantChangedFiles = changedFiles.filter(
    (filePath) => isRulesOnlyRecurrenceTarget(filePath) && filePath !== RECURRENCE_CLOSEOUT_PATH,
  );

  if (relevantChangedFiles.length === 0) {
    return [];
  }

  const errors = [];
  const artifactAbsolutePath = path.join(repoRoot, RECURRENCE_CLOSEOUT_PATH);

  if (!(await pathExists(artifactAbsolutePath))) {
    return [
      createError(
        "missing-recurrence-closeout-artifact",
        RECURRENCE_CLOSEOUT_PATH,
        `Rules-only changed files require tracked closeout artifact \`${RECURRENCE_CLOSEOUT_PATH}\`.`,
      ),
    ];
  }

  let payload;
  try {
    payload = JSON.parse(await fs.readFile(artifactAbsolutePath, "utf8"));
  } catch (error) {
    return [
      createError(
        "invalid-recurrence-closeout-json",
        RECURRENCE_CLOSEOUT_PATH,
        `Failed to parse recurrence closeout artifact JSON. ${error.message}`,
      ),
    ];
  }

  if (!changedFiles.includes(RECURRENCE_CLOSEOUT_PATH)) {
    errors.push(
      createError(
        "missing-recurrence-closeout-update",
        RECURRENCE_CLOSEOUT_PATH,
        `Rules-only changed files must update \`${RECURRENCE_CLOSEOUT_PATH}\` in the same change.`,
      ),
    );
  }

  errors.push(...validateRecurrenceCloseoutPayload(payload, relevantChangedFiles));
  return errors;
}

function validateRecurrenceCloseoutPayload(payload, relevantChangedFiles) {
  const errors = [];

  if (!payload || typeof payload !== "object" || Array.isArray(payload)) {
    return [
      createError(
        "invalid-recurrence-closeout-field",
        RECURRENCE_CLOSEOUT_PATH,
        "Recurrence closeout artifact must be a JSON object.",
      ),
    ];
  }

  if (!Number.isInteger(payload.schemaVersion) || payload.schemaVersion < 1) {
    errors.push(
      createError(
        "invalid-recurrence-closeout-field",
        RECURRENCE_CLOSEOUT_PATH,
        "`schemaVersion` must be an integer >= 1.",
      ),
    );
  }

  if (payload.scope !== "rules-only") {
    errors.push(
      createError(
        "invalid-recurrence-closeout-field",
        RECURRENCE_CLOSEOUT_PATH,
        "`scope` must be `rules-only`.",
      ),
    );
  }

  if (typeof payload.issueDetected !== "boolean") {
    errors.push(
      createError(
        "invalid-recurrence-closeout-field",
        RECURRENCE_CLOSEOUT_PATH,
        "`issueDetected` must be a boolean.",
      ),
    );
  }

  const stringFields = [
    "updatedAt",
    "declaredLane",
    "observedMutationClass",
    "acceptanceEvidenceClass",
    "rootCause",
    "prevention",
    "verification",
    "blockedReason",
  ];
  for (const field of stringFields) {
    if (typeof payload[field] !== "string") {
      errors.push(
        createError(
          "invalid-recurrence-closeout-field",
          RECURRENCE_CLOSEOUT_PATH,
          `\`${field}\` must be a string.`,
        ),
      );
    }
  }

  if (typeof payload.escalationRequired !== "boolean") {
    errors.push(
      createError(
        "invalid-recurrence-closeout-field",
        RECURRENCE_CLOSEOUT_PATH,
        "`escalationRequired` must be a boolean.",
      ),
    );
  }

  if (!Array.isArray(payload.changedPaths) || payload.changedPaths.some((value) => typeof value !== "string")) {
    errors.push(
      createError(
        "invalid-recurrence-closeout-field",
        RECURRENCE_CLOSEOUT_PATH,
        "`changedPaths` must be an array of strings.",
      ),
    );
  }

  if (typeof payload.verification === "string" && payload.verification.trim() === "") {
    errors.push(
      createError(
        "invalid-recurrence-closeout-field",
        RECURRENCE_CLOSEOUT_PATH,
        "`verification` must not be empty for rules-only closeout artifacts.",
      ),
    );
  }

  if (payload.issueDetected === true) {
    for (const field of [
      "declaredLane",
      "observedMutationClass",
      "acceptanceEvidenceClass",
      "rootCause",
      "prevention",
      "verification",
    ]) {
      if (typeof payload[field] === "string" && payload[field].trim() === "") {
        errors.push(
          createError(
            "missing-recurrence-closeout-field",
            RECURRENCE_CLOSEOUT_PATH,
            `\`${field}\` must not be empty when \`issueDetected = true\`.`,
          ),
        );
      }
    }
  }

  if (payload.escalationRequired === true && typeof payload.blockedReason === "string" && payload.blockedReason.trim() === "") {
    errors.push(
      createError(
        "missing-recurrence-closeout-field",
        RECURRENCE_CLOSEOUT_PATH,
        "`blockedReason` must not be empty when `escalationRequired = true`.",
      ),
    );
  }

  const artifactChangedPaths = Array.isArray(payload.changedPaths)
    ? new Set(payload.changedPaths.map((entry) => normalizeRepoRelativePath(entry)))
    : new Set();

  for (const changedFile of relevantChangedFiles.map((entry) => normalizeRepoRelativePath(entry))) {
    if (!artifactChangedPaths.has(changedFile)) {
      errors.push(
        createError(
          "recurrence-closeout-missing-changed-path",
          RECURRENCE_CLOSEOUT_PATH,
          `\`changedPaths\` must include rules-only changed file \`${changedFile}\`.`,
        ),
      );
    }
  }

  return errors;
}

function getRecurrenceChangedFiles(options) {
  if (Array.isArray(options.changedFiles)) {
    return options.changedFiles
      .map((entry) => normalizeRepoRelativePath(entry))
      .filter(Boolean);
  }

  const raw = process.env[RECURRENCE_CHANGED_FILES_ENV];
  if (!raw) {
    return [];
  }

  return raw
    .split(/\r?\n/u)
    .map((entry) => normalizeRepoRelativePath(entry))
    .filter(Boolean);
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

function validateKnownDocIdReferences(documents) {
  const knownDocIds = new Set(
    documents
      .map((document) => document.metadata.get("doc_id"))
      .filter((docId) => Boolean(docId)),
  );
  const errors = [];

  for (const document of documents) {
    const lines = stripFencedCodeBlocks(document.content).split(/\r?\n/);
    for (let index = 0; index < lines.length; index += 1) {
      const line = lines[index];
      for (const token of extractInlineCodeTokens(line)) {
        if (!DOC_ID_REFERENCE_PATTERN.test(token) || knownDocIds.has(token)) {
          continue;
        }

        errors.push(
          createError(
            "missing-doc-id-reference",
            document.repoRelativePath,
            `Inline owner doc_id reference \`${token}\` does not match any managed document.`,
            index + 1,
          ),
        );
      }
    }
  }

  return errors;
}

function validateIndexCoverage(documents, repoRoot) {
  const indexDocument = documents.find(
    (document) => document.repoRelativePath === "docs/index.md",
  );
  if (!indexDocument) {
    return [];
  }

  const indexedDocPaths = new Set();
  const lines = indexDocument.content.split(/\r?\n/);
  for (const line of lines) {
    const match = line.match(INDEX_STATUS_LINE_PATTERN);
    if (!match) {
      continue;
    }

    const target = normalizeMarkdownTarget(match[2]);
    if (!target || !isRelativeTarget(target)) {
      continue;
    }

    const resolvedPath = path.resolve(path.dirname(indexDocument.absolutePath), target);
    indexedDocPaths.add(toRepoRelative(repoRoot, resolvedPath));
  }

  const errors = [];
  for (const document of documents) {
    if (
      !document.repoRelativePath.startsWith("docs/") ||
      document.repoRelativePath === "docs/index.md"
    ) {
      continue;
    }

    if (indexedDocPaths.has(document.repoRelativePath)) {
      continue;
    }

    errors.push(
      createError(
        "index-missing-entry",
        "docs/index.md",
        `docs/index.md must register \`${document.repoRelativePath}\` with a status label entry.`,
      ),
    );
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

function isConcreteContractArtifactPath(target) {
  return /^(\.\.\/|\.\/)?\.stitch\/contracts\/.+\.json$/u.test(target)
    || /^\.stitch\/contracts\/.+\.json$/u.test(target);
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

function getExpectedDocIdPrefix(repoRelativePath) {
  if (repoRelativePath === "AGENTS.md") {
    return "repo.";
  }

  if (repoRelativePath === "docs/index.md") {
    return "docs.";
  }

  if (repoRelativePath.startsWith("docs/design/")) {
    return "design.";
  }

  if (repoRelativePath.startsWith("docs/discussions/")) {
    return "discussions.";
  }

  if (repoRelativePath.startsWith("docs/ops/")) {
    return "ops.";
  }

  if (repoRelativePath.startsWith("docs/plans/")) {
    return "plans.";
  }

  if (repoRelativePath.startsWith("docs/playtest/")) {
    return "playtest.";
  }

  if (repoRelativePath.startsWith("tools/")) {
    return "tools.";
  }

  if (repoRelativePath.startsWith(".codex/skills/jg-")) {
    return "skill.";
  }

  return null;
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

function validateDocumentContainsAll(document, requirements, errorCode, message) {
  const missingLabels = requirements
    .filter((requirement) => !documentHasAnyPattern(document.content, requirement.patterns))
    .map((requirement) => requirement.label);

  if (missingLabels.length === 0) {
    return [];
  }

  return [
    createError(
      errorCode,
      document.repoRelativePath,
      `${message} Missing: ${missingLabels.join(", ")}.`,
    ),
  ];
}

function documentHasAnyPattern(content, patterns) {
  return patterns.some((pattern) => pattern.test(content));
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

export function normalizeRepoRelativePath(repoRelativePath) {
  if (typeof repoRelativePath !== "string") {
    return "";
  }

  return repoRelativePath.replace(/\\/g, "/").trim();
}

export function isRulesOnlyRecurrenceTarget(repoRelativePath) {
  const normalized = normalizeRepoRelativePath(repoRelativePath);
  if (!normalized) {
    return false;
  }

  if (
    normalized === "AGENTS.md" ||
    normalized.startsWith("docs/") ||
    /^\.codex\/skills\/jg-[^/]+\//u.test(normalized) ||
    normalized.startsWith(".githooks/") ||
    normalized.startsWith("tools/docs-lint/") ||
    normalized.startsWith("tools/rule-harness/") ||
    normalized === ".github/workflows/docs-lint.yml" ||
    normalized === "package.json" ||
    normalized === "package-lock.json" ||
    normalized === RECURRENCE_CLOSEOUT_PATH
  ) {
    return true;
  }

  if (/^tools\/.+\/README\.md$/u.test(normalized)) {
    return true;
  }

  return /^tools\/.+\.(ps1|mjs|js|py)$/u.test(normalized);
}
