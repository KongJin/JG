import fs from "node:fs/promises";
import { accessSync } from "node:fs";
import os from "node:os";
import path from "node:path";
import process from "node:process";
import { execFile } from "node:child_process";
import { promisify } from "node:util";

const execFileAsync = promisify(execFile);

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
const ACCEPTANCE_GUARDRAILS_PATH = "docs/ops/acceptance_reporting_guardrails.md";
const ISSUE_INVESTIGATION_SKILL_PATH = ".codex/skills/jg-issue-investigation/SKILL.md";
const UNITY_WORKFLOW_SKILL_PATH = ".codex/skills/jg-unity-workflow/SKILL.md";
const SKILL_ROUTING_REGISTRY_PATH = "docs/ops/skill_routing_registry.md";
const SKILL_TRIGGER_MATRIX_PATH = "docs/ops/skill_trigger_matrix.md";
const COHESION_COUPLING_OWNER_PATTERNS = [/\bops\.cohesion-coupling-policy\b/u];
const ACCEPTANCE_GUARDRAILS_OWNER_PATTERNS = [/\bops\.acceptance-reporting-guardrails\b/u];
const GLOBAL_RULE_SKILL_NAMES = new Set([
  "rule-architecture",
  "rule-context",
  "rule-operations",
  "rule-patterns",
  "rule-plan-authoring",
  "rule-unity",
  "rule-validation",
]);
const ROOT_CAUSE_INVESTIGATION_REQUIREMENTS = [
  { patterns: [/## Root Cause Investigation/u], label: "`Root Cause Investigation` section" },
  { patterns: [/확인된 사실/u], label: "`확인된 사실` wording" },
  { patterns: [/가설/u], label: "`가설` wording" },
  { patterns: [/검증 방법/u], label: "`검증 방법` wording" },
  { patterns: [/판정/u], label: "`판정` wording" },
  { patterns: [/rootCause/u], label: "`rootCause` wording" },
  { patterns: [/blockedReason/u], label: "`blockedReason` wording" },
];
const FRESH_EVIDENCE_DISCIPLINE_REQUIREMENTS = [
  { patterns: [/## Fresh Evidence Discipline/u], label: "`Fresh Evidence Discipline` section" },
  { patterns: [/blocked:\s*fresh evidence pending/u], label: "`blocked: fresh evidence pending` wording" },
  { patterns: [/최신 실행/u, /\blatest run\b/iu], label: "latest run wording" },
  { patterns: [/최신 캡쳐/u, /\bfresh capture\b/iu, /\blatest capture\b/iu], label: "latest/fresh capture wording" },
  { patterns: [/최신 artifact/u, /\bcurrent artifact\b/iu, /\blatest artifact\b/iu], label: "latest/current artifact wording" },
  { patterns: [/`old`와 `current`/u, /\bold\b.*\bcurrent\b/isu], label: "`old`/`current` path separation" },
];
const FRESH_EVIDENCE_SKILL_ROUTE_REQUIREMENTS = [
  {
    patterns: ACCEPTANCE_GUARDRAILS_OWNER_PATTERNS,
    label: "`ops.acceptance-reporting-guardrails` owner route",
  },
  { patterns: [/Fresh Evidence Discipline/u], label: "`Fresh Evidence Discipline` route" },
];
const UNCERTAIN_ROOT_CAUSE_PATTERN = /(?:아마|추정|가능성|보임|보인다|보여|듯|것 같|\bmaybe\b|\bprobably\b|\blikely\b|\bappears\b|\bseems\b)/iu;
const RECURRENCE_CLOSEOUT_COVERAGE_PATTERNS = [
  {
    pattern: /\bactive plan artifact owner shape drift\b/iu,
    label: "`active plan artifact owner shape drift`",
  },
  {
    pattern: /\bactive plan concrete artifact owner collisions?\b/iu,
    label: "`active plan concrete artifact owner collision`",
  },
  {
    pattern: /\bprogress evidence (?:artifact )?overload\b/iu,
    label: "`progress evidence overload`",
  },
];
const RECURRENCE_VERIFICATION_EVIDENCE_PATTERN =
  /(?:\bnode --test\b|\bnpm run\b|\brules:lint\b|\brules:sync-closeout\b|\brg\b|\bgit\b|\bpwsh\b|\bpowershell\b|\bUnity\b|\bartifacts?\/|\bdocs\/|\btools\/)/iu;
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
const ACTIVE_PLAN_BUDGET_EXCLUDING_PROGRESS = 5;
const PROGRESS_EVIDENCE_ARTIFACT_BUDGET = 2;
const ACTIVE_PLAN_STALE_DAYS = 14;
const ACTIVE_DOC_STALE_DAYS = 45;
const PROGRESS_NONBLANK_LINE_WARNING_BUDGET = 80;
const ACTIVE_PLAN_NONBLANK_LINE_WARNING_BUDGET = 120;
const AGENTS_NONBLANK_LINE_WARNING_BUDGET = 80;
const INDEX_NONBLANK_LINE_WARNING_BUDGET = 180;
const SKILL_ENTRY_NONBLANK_LINE_WARNING_BUDGET = 160;
const ENTRY_POLICY_BODY_PATHS = new Set(["AGENTS.md", "docs/index.md"]);
const ENTRY_POLICY_BODY_HEADINGS = new Set([
  "상위 원칙",
  "적용 범위",
  "자동 검증",
  "완료 plan lifecycle",
  "Root Cause Investigation",
  "Fresh Evidence Discipline",
  "Behavior-First Test Loop",
]);
const MODULE_DATA_STRUCTURE_PATH = "docs/design/module_data_structure.md";
const MODULE_DATA_STRUCTURE_UNIT_SECTION_START = "## ScriptableObject 데이터 정의";
const MODULE_DATA_STRUCTURE_UNIT_SECTION_END = "## 편성 데이터 (Garage Roster)";
const RULE_HARNESS_ADVISORY_MEMORY_PATH = "tools/rule-harness/memory/advisory-memory.json";
export const RECURRENCE_CLOSEOUT_PATH = "artifacts/rules/issue-recurrence-closeout.json";
export const RECURRENCE_CLOSEOUT_DIR = "artifacts/rules/issue-recurrence-closeout.d";
export const RECURRENCE_CHANGED_FILES_ENV = "RULES_LINT_CHANGED_FILES";

function validateRootCauseInvestigationContract(document) {
  if (document.repoRelativePath !== ACCEPTANCE_GUARDRAILS_PATH) {
    return [];
  }

  return validateDocumentContainsAll(
    document,
    ROOT_CAUSE_INVESTIGATION_REQUIREMENTS,
    "missing-root-cause-investigation-contract",
    `${ACCEPTANCE_GUARDRAILS_PATH} must own the root-cause investigation contract so unverified hypotheses are not reported as rootCause or success.`,
  );
}

function validateIssueInvestigationSkillRoute(document) {
  if (document.repoRelativePath !== ISSUE_INVESTIGATION_SKILL_PATH) {
    return [];
  }

  return validateDocumentContainsAll(
    document,
    [
      { patterns: [/docs\/index\.md/u], label: "`docs/index.md` route" },
      {
        patterns: ACCEPTANCE_GUARDRAILS_OWNER_PATTERNS,
        label: "`ops.acceptance-reporting-guardrails` owner route",
      },
    ],
    "missing-issue-investigation-owner-route",
    "JG issue investigation skill-entry must route root-cause and hypothesis verification reporting through `docs/index.md` and `ops.acceptance-reporting-guardrails`.",
  );
}

function validateFreshEvidenceDisciplineContract(document) {
  if (document.repoRelativePath !== ACCEPTANCE_GUARDRAILS_PATH) {
    return [];
  }

  return validateDocumentContainsAll(
    document,
    FRESH_EVIDENCE_DISCIPLINE_REQUIREMENTS,
    "missing-fresh-evidence-discipline-contract",
    `${ACCEPTANCE_GUARDRAILS_PATH} must own Fresh Evidence Discipline so visual/capture judgments cannot mix stale evidence with current acceptance.`,
  );
}

function validateFreshEvidenceSkillRoutes(document) {
  if (
    document.repoRelativePath !== ISSUE_INVESTIGATION_SKILL_PATH
    && document.repoRelativePath !== UNITY_WORKFLOW_SKILL_PATH
  ) {
    return [];
  }

  return validateDocumentContainsAll(
    document,
    FRESH_EVIDENCE_SKILL_ROUTE_REQUIREMENTS,
    "missing-fresh-evidence-skill-route",
    "Repo-local investigation/Unity skill entries must route visual/capture evidence judgments through Fresh Evidence Discipline in `ops.acceptance-reporting-guardrails`.",
  );
}

export async function lintRepository(repoRoot, options = {}) {
  const includeGeneralChecks = options.includeGeneralChecks ?? true;
  const includePolicyChecks = options.includePolicyChecks ?? true;
  const now = normalizeNowOption(options.now);
  const managedDocPaths = await discoverManagedDocs(repoRoot);
  const documents = [];
  const errors = [];
  const warnings = [];

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
      errors.push(...validatePlanRereviewCleanScope(document));
      errors.push(...validateActivePlanReferenceCloseout(document));
      errors.push(...validateModuleDataStructureStaleOwners(document));
      errors.push(...validateEntryPolicyBody(document));
      warnings.push(...validateStaleActiveDocumentWarnings(document, now));
      warnings.push(...validateDocumentSizeWarnings(document));
    }
    if (includePolicyChecks) {
      errors.push(...validatePlanModeRouting(document));
      errors.push(...validateRootCauseInvestigationContract(document));
      errors.push(...validateIssueInvestigationSkillRoute(document));
      errors.push(...validateFreshEvidenceDisciplineContract(document));
      errors.push(...validateFreshEvidenceSkillRoutes(document));
    }
  }

  if (includeGeneralChecks) {
    errors.push(...validateUniqueDocIds(documents));
    errors.push(...validateKnownDocIdReferences(documents));
    errors.push(...validateGlobalSkillRegistry(documents));
    errors.push(...validateSkillTriggerMatrix(documents));
    errors.push(...validateIndexCoverage(documents, repoRoot));
    errors.push(...validateIndexStatusLabels(documents, repoRoot));
    errors.push(...validateActivePlanBudget(documents));
    errors.push(...validateActivePlanMutualReferences(documents, repoRoot));
    errors.push(...validateActivePlanArtifactShapes(documents));
    errors.push(...validateActivePlanArtifactOwnerCollisions(documents));
    errors.push(...validateProgressEvidenceOverload(documents));
    errors.push(...(await validateRuleHarnessAdvisoryMemory(repoRoot, documents)));
    errors.push(...(await validateDocsMetaArtifacts(repoRoot)));
    errors.push(...(await validateGlobalRuleSkillMarkdownLinks(options)));
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
    warnings: sortDiagnostics(warnings),
  };
}

export function formatLintReport(result) {
  const warnings = result.warnings || [];

  if (result.errors.length === 0) {
    const lines = [
      warnings.length === 0
        ? `Docs lint passed. Checked ${result.managedDocPaths.length} managed document(s).`
        : `Docs lint passed with ${warnings.length} warning(s). Checked ${result.managedDocPaths.length} managed document(s).`,
    ];
    appendWarningReportLines(lines, warnings);
    return lines.join("\n");
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

  appendWarningReportLines(lines, warnings);
  return lines.join("\n");
}

export function buildDocsHealthReport(result) {
  const documents = Array.isArray(result.documents) ? result.documents : [];
  const activeNonProgressPlans = getActiveNonProgressPlans(documents)
    .map((document) => document.repoRelativePath)
    .sort((left, right) => left.localeCompare(right));
  const errors = Array.isArray(result.errors) ? sortDiagnostics(result.errors) : [];
  const warnings = Array.isArray(result.warnings) ? sortDiagnostics(result.warnings) : [];

  return {
    schemaVersion: 1,
    managedDocumentCount: Array.isArray(result.managedDocPaths)
      ? result.managedDocPaths.length
      : documents.length,
    blockingIssueCount: errors.length,
    warningCount: warnings.length,
    activeNonProgressPlanBudget: {
      current: activeNonProgressPlans.length,
      limit: ACTIVE_PLAN_BUDGET_EXCLUDING_PROGRESS,
      status: activeNonProgressPlans.length <= ACTIVE_PLAN_BUDGET_EXCLUDING_PROGRESS
        ? "ok"
        : "over-budget",
      paths: activeNonProgressPlans,
    },
    errorsByCode: countDiagnosticsByCode(errors),
    warningsByCode: countDiagnosticsByCode(warnings),
    errors,
    warnings,
  };
}

export function formatDocsHealthReport(report) {
  const budget = report.activeNonProgressPlanBudget || {
    current: 0,
    limit: ACTIVE_PLAN_BUDGET_EXCLUDING_PROGRESS,
    status: "ok",
    paths: [],
  };
  const lines = [
    "Docs health report",
    `Managed docs: ${report.managedDocumentCount}`,
    `Blocking lint issues: ${report.blockingIssueCount}`,
    `Warnings: ${report.warningCount}`,
    `Active non-progress plans: ${budget.current}/${budget.limit} (${budget.status})`,
  ];

  if (budget.paths.length > 0) {
    lines.push("Active plan owners:");
    for (const planPath of budget.paths) {
      lines.push(`- ${planPath}`);
    }
  }

  appendDiagnosticsSummary(lines, "Blocking issue codes", report.errorsByCode);
  appendDiagnosticsSummary(lines, "Warning codes", report.warningsByCode);
  appendDiagnosticsList(lines, "Blocking issues", report.errors);
  appendDiagnosticsList(lines, "Warnings", report.warnings, "warning:");

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

  const ruleHarnessPromptsDir = path.join(repoRoot, "tools", "rule-harness", "prompts");
  for (const markdownFile of await walkMarkdownFiles(ruleHarnessPromptsDir, repoRoot)) {
    discovered.add(markdownFile);
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

async function walkFiles(rootDir, repoRoot, predicate) {
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
      collected.push(...(await walkFiles(absolutePath, repoRoot, predicate)));
      continue;
    }

    if (entry.isFile() && predicate(entry, repoRelativePath)) {
      collected.push(absolutePath);
    }
  }

  return collected;
}

async function walkExternalMarkdownFiles(rootDir) {
  if (!(await pathExists(rootDir))) {
    return [];
  }

  const collected = [];
  const entries = await fs.readdir(rootDir, { withFileTypes: true });
  for (const entry of entries) {
    const absolutePath = path.join(rootDir, entry.name);

    if (entry.isDirectory()) {
      collected.push(...(await walkExternalMarkdownFiles(absolutePath)));
      continue;
    }

    if (entry.isFile() && entry.name.endsWith(".md")) {
      collected.push(absolutePath);
    }
  }

  return collected.sort((left, right) => left.localeCompare(right));
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

async function validateGlobalRuleSkillMarkdownLinks(options = {}) {
  const globalRuleSkillsRoot =
    options.globalRuleSkillsRoot ?? getDefaultGlobalRuleSkillsRoot();
  if (!(await pathExists(globalRuleSkillsRoot))) {
    return [];
  }

  const errors = [];
  const entries = await fs.readdir(globalRuleSkillsRoot, { withFileTypes: true });
  const ruleSkillDirs = entries
    .filter((entry) => entry.isDirectory() && entry.name.startsWith("rule-"))
    .map((entry) => path.join(globalRuleSkillsRoot, entry.name));

  for (const skillDir of ruleSkillDirs) {
    const markdownFiles = await walkExternalMarkdownFiles(skillDir);
    for (const markdownFile of markdownFiles) {
      const content = await fs.readFile(markdownFile, "utf8");
      const lines = stripFencedCodeBlocks(content).split(/\r?\n/);

      for (let index = 0; index < lines.length; index += 1) {
        const line = lines[index];
        for (const target of extractRelativeMarkdownTargets(line)) {
          const resolvedPath = path.resolve(path.dirname(markdownFile), target);
          if (await pathExists(resolvedPath)) {
            continue;
          }

          errors.push(
            createError(
              "global-rule-broken-relative-link",
              toGlobalRuleSkillReportPath(globalRuleSkillsRoot, markdownFile),
              `Global rule skill relative link target \`${target}\` does not exist. Repo lint cannot track external file diffs, but it can guard link integrity here.`,
              index + 1,
            ),
          );
        }
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

function validatePlanRereviewCleanScope(document) {
  const role = document.metadata.get("role");
  if (role !== "plan") {
    return [];
  }

  const content = stripFencedCodeBlocks(document.content);
  const lines = content.split(/\r?\n/);
  const errors = [];

  for (let index = 0; index < lines.length; index += 1) {
    const line = lines[index];
    if (!/^\s*-?\s*plan rereview:\s*clean\s*$/u.test(line)) {
      continue;
    }

    errors.push(
      createError(
        "bare-plan-rereview-clean",
        document.repoRelativePath,
        "`plan rereview: clean` must name the checked scope, for example `plan rereview: clean - owner/scope/residual checked`.",
        index + 1,
      ),
    );
  }

  return errors;
}

function validateActivePlanReferenceCloseout(document) {
  const status = document.metadata.get("상태");
  const role = document.metadata.get("role");
  if (status !== "active" || role !== "plan") {
    return [];
  }

  const content = stripFencedCodeBlocks(document.content);
  const lines = content.split(/\r?\n/);
  const closeoutPatterns = [
    /reference\s*전환\s*이유/u,
    /\bStatus:\s*reference\b/u,
    /active\s*실행\s*계획에서\s*reference/u,
  ];
  const errors = [];

  for (let index = 0; index < lines.length; index += 1) {
    const line = lines[index];
    if (!closeoutPatterns.some((pattern) => pattern.test(line))) {
      continue;
    }

    errors.push(
      createError(
        "active-plan-reference-closeout",
        document.repoRelativePath,
        "Active plan contains reference-closeout wording. Move it to `reference` or remove the completed lifecycle wording.",
        index + 1,
      ),
    );
  }

  return errors;
}

function validateModuleDataStructureStaleOwners(document) {
  if (document.repoRelativePath !== MODULE_DATA_STRUCTURE_PATH) {
    return [];
  }

  const lines = document.content.split(/\r?\n/);
  const startIndex = lines.findIndex((line) => line.trim() === MODULE_DATA_STRUCTURE_UNIT_SECTION_START);
  const endIndex = lines.findIndex((line) => line.trim() === MODULE_DATA_STRUCTURE_UNIT_SECTION_END);
  if (startIndex < 0 || endIndex < 0 || endIndex <= startIndex) {
    return [
      createError(
        "stale-module-data-structure",
        document.repoRelativePath,
        `\`module_data_structure.md\` stale owner guard could not find the expected Unit/Module section anchors (${MODULE_DATA_STRUCTURE_UNIT_SECTION_START} -> ${MODULE_DATA_STRUCTURE_UNIT_SECTION_END}). Keep these anchors stable or update the guard with matching fixtures.`,
      ),
    ];
  }

  const stalePatterns = [
    /namespace\s+Features\.Garage\.(Domain|Infrastructure)\b/u,
    /menuName\s*=\s*"Garage\//u,
  ];
  const errors = [];

  for (let index = startIndex; index < endIndex; index += 1) {
    const line = lines[index];
    if (!stalePatterns.some((pattern) => pattern.test(line))) {
      continue;
    }

    errors.push(
      createError(
        "stale-module-data-structure",
        document.repoRelativePath,
        "`module_data_structure.md` Unit/Module-owned sections must use the current `Features.Unit.*` owner, not stale Garage namespaces or Garage create menus.",
        index + 1,
      ),
    );
  }

  return errors;
}

function validateEntryPolicyBody(document) {
  if (!ENTRY_POLICY_BODY_PATHS.has(document.repoRelativePath)) {
    return [];
  }

  const lines = stripFencedCodeBlocks(document.content).split(/\r?\n/);
  const errors = [];

  for (let index = 0; index < lines.length; index += 1) {
    const match = lines[index].match(/^##\s+(.+?)\s*$/u);
    if (!match || !ENTRY_POLICY_BODY_HEADINGS.has(match[1].trim())) {
      continue;
    }

    errors.push(
      createError(
        "entry-policy-body",
        document.repoRelativePath,
        `Entry documents must route to owner docs instead of owning policy body heading \`${match[1].trim()}\`. Keep short route notes or sharp-edge reminders only.`,
        index + 1,
      ),
    );
  }

  return errors;
}

function validateStaleActiveDocumentWarnings(document, now) {
  if (document.metadata.get("상태") !== "active") {
    return [];
  }

  const lastUpdated = parseMetadataDate(document.metadata.get("마지막 업데이트"));
  if (!lastUpdated) {
    return [];
  }

  const ageDays = getWholeUtcAgeDays(lastUpdated, now);
  const role = document.metadata.get("role");

  if (
    role === "plan"
    && document.repoRelativePath.startsWith("docs/plans/")
    && document.repoRelativePath !== "docs/plans/progress.md"
    && ageDays > ACTIVE_PLAN_STALE_DAYS
  ) {
    return [
      createWarning(
        "stale-active-plan",
        document.repoRelativePath,
        `Active plan was last updated ${ageDays} day(s) ago. Reconfirm whether it should stay active, move to reference, or hand residuals to progress.md.`,
      ),
    ];
  }

  if (role !== "plan" && ageDays > ACTIVE_DOC_STALE_DAYS) {
    return [
      createWarning(
        "stale-active-doc",
        document.repoRelativePath,
        `Active non-plan document was last updated ${ageDays} day(s) ago. Reconfirm whether the owner route and current judgment are still fresh.`,
      ),
    ];
  }

  return [];
}

function validateDocumentSizeWarnings(document) {
  const nonblankLineCount = countNonblankLines(document.content);
  const role = document.metadata.get("role");
  const status = document.metadata.get("상태");

  if (
    document.repoRelativePath === "docs/plans/progress.md"
    && nonblankLineCount > PROGRESS_NONBLANK_LINE_WARNING_BUDGET
  ) {
    return [
      createWarning(
        "progress-size-advisory",
        document.repoRelativePath,
        `progress.md has ${nonblankLineCount} nonblank line(s). Keep current state and residuals here; move detailed evidence or history to owner plans/reference docs.`,
      ),
    ];
  }

  if (
    status === "active"
    && role === "plan"
    && document.repoRelativePath.startsWith("docs/plans/")
    && document.repoRelativePath !== "docs/plans/progress.md"
    && nonblankLineCount > ACTIVE_PLAN_NONBLANK_LINE_WARNING_BUDGET
  ) {
    return [
      createWarning(
        "active-plan-size-advisory",
        document.repoRelativePath,
        `Active plan has ${nonblankLineCount} nonblank line(s). Consider compressing logs, closed phases, and evidence detail into reference closeout links.`,
      ),
    ];
  }

  if (
    document.repoRelativePath === "AGENTS.md"
    && nonblankLineCount > AGENTS_NONBLANK_LINE_WARNING_BUDGET
  ) {
    return [
      createWarning(
        "entry-size-advisory",
        document.repoRelativePath,
        `AGENTS.md has ${nonblankLineCount} nonblank line(s). Keep entry content to routes and short sharp-edge reminders.`,
      ),
    ];
  }

  if (
    document.repoRelativePath === "docs/index.md"
    && nonblankLineCount > INDEX_NONBLANK_LINE_WARNING_BUDGET
  ) {
    return [
      createWarning(
        "entry-size-advisory",
        document.repoRelativePath,
        `docs/index.md has ${nonblankLineCount} nonblank line(s). Keep the index as a registry and move policy body to owner docs.`,
      ),
    ];
  }

  if (
    isRepoLocalSkillEntry(document.repoRelativePath)
    && nonblankLineCount > SKILL_ENTRY_NONBLANK_LINE_WARNING_BUDGET
  ) {
    return [
      createWarning(
        "skill-entry-size-advisory",
        document.repoRelativePath,
        `Skill entry has ${nonblankLineCount} nonblank line(s). Check whether policy body belongs in the owner docs instead of the skill route.`,
      ),
    ];
  }

  return [];
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
  const changedFiles = await getRecurrenceChangedFiles({
    repoRoot,
    changedFiles: options.changedFiles,
  });
  const relevantChangedFiles = changedFiles.filter(
    (filePath) => isRulesOnlyRecurrenceTarget(filePath) && !isRecurrenceCloseoutArtifactPath(filePath),
  );

  if (relevantChangedFiles.length === 0) {
    return [];
  }

  const errors = [];
  const changedCloseoutArtifacts = changedFiles
    .map((entry) => normalizeRepoRelativePath(entry))
    .filter((entry) => isRecurrenceCloseoutArtifactPath(entry));

  if (changedCloseoutArtifacts.length === 0) {
    const legacyArtifactExists = await pathExists(path.join(repoRoot, RECURRENCE_CLOSEOUT_PATH));
    const errorCode = legacyArtifactExists
      ? "missing-recurrence-closeout-update"
      : "missing-recurrence-closeout-artifact";
    const message = legacyArtifactExists
      ? `Rules-only changed files must update a closeout artifact in the same change: \`${RECURRENCE_CLOSEOUT_PATH}\` or \`${RECURRENCE_CLOSEOUT_DIR}/*.json\`.`
      : `Rules-only changed files require a tracked closeout artifact: \`${RECURRENCE_CLOSEOUT_PATH}\` or \`${RECURRENCE_CLOSEOUT_DIR}/*.json\`.`;
    return [
      createError(
        errorCode,
        RECURRENCE_CLOSEOUT_PATH,
        message,
      ),
    ];
  }

  const coveredChangedPaths = new Set();
  for (const artifactPath of changedCloseoutArtifacts) {
    const artifactAbsolutePath = path.join(repoRoot, artifactPath);
    if (!(await pathExists(artifactAbsolutePath))) {
      errors.push(
        createError(
          "missing-recurrence-closeout-artifact",
          artifactPath,
          `Rules-only closeout artifact \`${artifactPath}\` does not exist.`,
        ),
      );
      continue;
    }

    let payload;
    try {
      payload = JSON.parse(await fs.readFile(artifactAbsolutePath, "utf8"));
    } catch (error) {
      errors.push(
        createError(
          "invalid-recurrence-closeout-json",
          artifactPath,
          `Failed to parse recurrence closeout artifact JSON. ${error.message}`,
        ),
      );
      continue;
    }

    errors.push(...validateRecurrenceCloseoutPayload(payload, artifactPath));
    if (Array.isArray(payload.changedPaths)) {
      for (const changedPath of payload.changedPaths) {
        coveredChangedPaths.add(normalizeRepoRelativePath(changedPath));
      }
    }
  }

  for (const changedFile of relevantChangedFiles.map((entry) => normalizeRepoRelativePath(entry))) {
    if (!coveredChangedPaths.has(changedFile)) {
      errors.push(
        createError(
          "recurrence-closeout-missing-changed-path",
          changedCloseoutArtifacts[0] || RECURRENCE_CLOSEOUT_PATH,
          `Closeout artifact \`changedPaths\` must include rules-only changed file \`${changedFile}\`.`,
        ),
      );
    }
  }

  return errors;
}

function validateRecurrenceCloseoutPayload(payload, artifactPath = RECURRENCE_CLOSEOUT_PATH) {
  const errors = [];

  if (!payload || typeof payload !== "object" || Array.isArray(payload)) {
    return [
      createError(
        "invalid-recurrence-closeout-field",
        artifactPath,
        "Recurrence closeout artifact must be a JSON object.",
      ),
    ];
  }

  if (!Number.isInteger(payload.schemaVersion) || payload.schemaVersion < 1) {
    errors.push(
      createError(
        "invalid-recurrence-closeout-field",
        artifactPath,
        "`schemaVersion` must be an integer >= 1.",
      ),
    );
  }

  if (payload.scope !== "rules-only") {
    errors.push(
      createError(
        "invalid-recurrence-closeout-field",
        artifactPath,
        "`scope` must be `rules-only`.",
      ),
    );
  }

  if (typeof payload.issueDetected !== "boolean") {
    errors.push(
      createError(
        "invalid-recurrence-closeout-field",
        artifactPath,
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
          artifactPath,
          `\`${field}\` must be a string.`,
        ),
      );
    }
  }

  if (typeof payload.escalationRequired !== "boolean") {
    errors.push(
      createError(
        "invalid-recurrence-closeout-field",
        artifactPath,
        "`escalationRequired` must be a boolean.",
      ),
    );
  }

  if (!Array.isArray(payload.changedPaths) || payload.changedPaths.some((value) => typeof value !== "string")) {
    errors.push(
      createError(
        "invalid-recurrence-closeout-field",
        artifactPath,
        "`changedPaths` must be an array of strings.",
      ),
    );
  }

  if (typeof payload.verification === "string" && payload.verification.trim() === "") {
    errors.push(
      createError(
        "invalid-recurrence-closeout-field",
        artifactPath,
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
            artifactPath,
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
        artifactPath,
        "`blockedReason` must not be empty when `escalationRequired = true`.",
      ),
    );
  }

  if (
    typeof payload.rootCause === "string" &&
    UNCERTAIN_ROOT_CAUSE_PATTERN.test(payload.rootCause) &&
    typeof payload.blockedReason === "string" &&
    payload.blockedReason.trim() === ""
  ) {
    errors.push(
      createError(
        "uncertain-root-cause-without-blocked-reason",
        artifactPath,
        "`rootCause` must not contain uncertain hypothesis wording unless `blockedReason` explains the missing verification.",
      ),
    );
  }

  if (
    payload.issueDetected === true &&
    typeof payload.rootCause === "string" &&
    payload.rootCause.trim() !== "" &&
    typeof payload.verification === "string" &&
    payload.verification.trim() !== "" &&
    !RECURRENCE_VERIFICATION_EVIDENCE_PATTERN.test(payload.verification)
  ) {
    errors.push(
      createError(
        "missing-recurrence-closeout-verification-evidence",
        artifactPath,
        "`verification` must name a concrete command, artifact path, owner path, or evidence anchor when `rootCause` is populated.",
      ),
    );
  }

  if (payload.issueDetected === true) {
    const coverageText = [
      payload.observedMutationClass,
      payload.rootCause,
      payload.prevention,
    ]
      .filter((value) => typeof value === "string")
      .join("\n");

    for (const { pattern, label } of RECURRENCE_CLOSEOUT_COVERAGE_PATTERNS) {
      if (pattern.test(coverageText)) {
        continue;
      }

      errors.push(
        createError(
          "missing-recurrence-closeout-coverage",
          artifactPath,
          `Recurrence closeout artifact must mention hard-fail coverage ${label} in observedMutationClass, rootCause, or prevention.`,
        ),
      );
    }
  }

  const artifactChangedPaths = Array.isArray(payload.changedPaths)
    ? new Set(payload.changedPaths.map((entry) => normalizeRepoRelativePath(entry)))
    : new Set();

  if (!artifactChangedPaths.has(normalizeRepoRelativePath(artifactPath))) {
    errors.push(
      createError(
        "recurrence-closeout-missing-changed-path",
        artifactPath,
        `\`changedPaths\` must include closeout artifact \`${artifactPath}\`.`,
      ),
    );
  }

  return errors;
}

export async function getRecurrenceChangedFiles({
  repoRoot,
  changedFiles = null,
} = {}) {
  if (Array.isArray(changedFiles)) {
    return changedFiles
      .map((entry) => normalizeRepoRelativePath(entry))
      .filter(Boolean);
  }

  const raw = process.env[RECURRENCE_CHANGED_FILES_ENV];
  if (raw) {
    return raw
      .split(/\r?\n/u)
      .map((entry) => normalizeRepoRelativePath(entry))
      .filter(Boolean);
  }

  return readChangedFilesFromGit(repoRoot);
}

async function readChangedFilesFromGit(repoRoot) {
  if (!repoRoot || !(await pathExists(path.join(repoRoot, ".git")))) {
    return [];
  }

  const commands = [
    ["diff", "--name-only", "--diff-filter=ACMRD"],
    ["diff", "--cached", "--name-only", "--diff-filter=ACMRD"],
    ["ls-files", "--others", "--exclude-standard"],
  ];
  const changedFiles = [];

  for (const args of commands) {
    try {
      const { stdout } = await execFileAsync("git", args, { cwd: repoRoot });
      changedFiles.push(...stdout.split(/\r?\n/u));
    } catch {
      return [];
    }
  }

  return [...new Set(
    changedFiles
      .map((entry) => normalizeRepoRelativePath(entry))
      .filter(Boolean),
  )].sort((left, right) => left.localeCompare(right));
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
    for (const token of splitMetadataList(document.metadata.get("upstream") || "")) {
      if (token.toLowerCase() === "none") {
        continue;
      }

      if (!DOC_ID_REFERENCE_PATTERN.test(token)) {
        errors.push(
          createError(
            "missing-upstream-doc-id-reference",
            document.repoRelativePath,
            `Metadata upstream owner reference \`${token}\` must use a managed doc_id, not a path or free-form token.`,
          ),
        );
        continue;
      }

      if (!knownDocIds.has(token)) {
        errors.push(
          createError(
            "missing-upstream-doc-id-reference",
            document.repoRelativePath,
            `Metadata upstream owner doc_id reference \`${token}\` does not match any managed document.`,
          ),
        );
      }
    }

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

function validateGlobalSkillRegistry(documents) {
  const registryDocument = documents.find(
    (document) => document.repoRelativePath === SKILL_ROUTING_REGISTRY_PATH,
  );
  if (!registryDocument) {
    return [];
  }

  const registeredSkillNames = new Set(
    extractGlobalRuleSkillNames(registryDocument.content),
  );
  const errors = [];

  for (const document of documents) {
    if (document.repoRelativePath === SKILL_ROUTING_REGISTRY_PATH) {
      continue;
    }

    const lines = stripFencedCodeBlocks(document.content).split(/\r?\n/);
    for (let index = 0; index < lines.length; index += 1) {
      const line = lines[index];
      for (const token of extractInlineCodeTokens(line)) {
        if (!isGlobalRuleSkillToken(token) || registeredSkillNames.has(token)) {
          continue;
        }

        errors.push(
          createError(
            "missing-global-skill-registry-entry",
            document.repoRelativePath,
            `Global rule skill reference \`${token}\` must be registered in \`${SKILL_ROUTING_REGISTRY_PATH}\` before repo docs or skills route to it.`,
            index + 1,
          ),
        );
      }
    }
  }

  return errors;
}

function validateSkillTriggerMatrix(documents) {
  const matrixDocument = documents.find(
    (document) => document.repoRelativePath === SKILL_TRIGGER_MATRIX_PATH,
  );
  if (!matrixDocument) {
    return [];
  }

  const repoLocalSkillNames = new Set(getRepoLocalSkillNames(documents));
  const registryDocument = documents.find(
    (document) => document.repoRelativePath === SKILL_ROUTING_REGISTRY_PATH,
  );
  const registeredGlobalSkillNames = new Set(
    registryDocument ? extractGlobalRuleSkillNames(registryDocument.content) : [],
  );
  const matrixSkillNames = new Set(extractSkillRouteNames(matrixDocument.content));
  const errors = [];

  const lines = stripFencedCodeBlocks(matrixDocument.content).split(/\r?\n/);
  for (let index = 0; index < lines.length; index += 1) {
    const line = lines[index];
    for (const token of extractInlineCodeTokens(line)) {
      if (!isSkillRouteToken(token)) {
        continue;
      }

      const knownRepoLocalSkill = token.startsWith("jg-") && repoLocalSkillNames.has(token);
      const knownGlobalSkill = token.startsWith("rule-") && registeredGlobalSkillNames.has(token);
      if (knownRepoLocalSkill || knownGlobalSkill) {
        continue;
      }

      errors.push(
        createError(
          "unknown-skill-trigger-route",
          matrixDocument.repoRelativePath,
          `Skill trigger fixture references unknown or unregistered skill route \`${token}\`.`,
          index + 1,
        ),
      );
    }
  }

  for (const skillName of [...repoLocalSkillNames].sort((left, right) => left.localeCompare(right))) {
    if (matrixSkillNames.has(skillName)) {
      continue;
    }

    errors.push(
      createError(
        "missing-skill-trigger-fixture",
        matrixDocument.repoRelativePath,
        `Skill trigger matrix must include at least one fixture for repo-local skill \`${skillName}\`.`,
      ),
    );
  }

  for (const skillName of [...registeredGlobalSkillNames].sort((left, right) => left.localeCompare(right))) {
    if (matrixSkillNames.has(skillName)) {
      continue;
    }

    errors.push(
      createError(
        "missing-skill-trigger-fixture",
        matrixDocument.repoRelativePath,
        `Skill trigger matrix must include at least one fixture for registered global skill \`${skillName}\`.`,
      ),
    );
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

function validateActivePlanBudget(documents) {
  const activePlans = getActiveNonProgressPlans(documents)
    .map((document) => document.repoRelativePath);

  if (activePlans.length <= ACTIVE_PLAN_BUDGET_EXCLUDING_PROGRESS) {
    return [];
  }

  return [
    createError(
      "active-plan-budget",
      "docs/index.md",
      `Too many active non-progress plans (${activePlans.length}/${ACTIVE_PLAN_BUDGET_EXCLUDING_PROGRESS}). Move completed, blocked, or residual-only plans to \`reference\` and keep current execution owners focused. Active plans: ${activePlans.join(", ")}.`,
    ),
  ];
}

function validateActivePlanMutualReferences(documents, repoRoot) {
  const activePlans = getActiveNonProgressPlans(documents);
  const activePlanPaths = new Set(activePlans.map((document) => document.repoRelativePath));
  const activePlanDocIds = new Map(
    activePlans.map((document) => [document.metadata.get("doc_id"), document.repoRelativePath]),
  );
  const referencesByPath = new Map();

  for (const document of activePlans) {
    const references = new Set();

    for (const target of getDocumentMarkdownTargets(document, repoRoot)) {
      if (activePlanPaths.has(target) && target !== document.repoRelativePath) {
        references.add(target);
      }
    }

    for (const token of splitMetadataList(document.metadata.get("upstream") || "")) {
      const target = activePlanDocIds.get(token);
      if (target && target !== document.repoRelativePath) {
        references.add(target);
      }
    }

    referencesByPath.set(document.repoRelativePath, references);
  }

  const errors = [];
  for (const document of activePlans) {
    const references = referencesByPath.get(document.repoRelativePath) || new Set();
    for (const target of references) {
      if (!(referencesByPath.get(target) || new Set()).has(document.repoRelativePath)) {
        continue;
      }

      errors.push(
        createError(
          "active-plan-mutual-reference",
          document.repoRelativePath,
          `Active non-progress plans must not mutually depend on each other. Break the reciprocal reference between \`${document.repoRelativePath}\` and \`${target}\`; keep one owner active and move handoff detail to \`progress.md\`, a reference plan, or a one-way upstream.`,
        ),
      );
    }
  }

  return errors;
}

function validateActivePlanArtifactOwnerCollisions(documents) {
  const ownersByArtifact = new Map();

  for (const document of getActiveNonProgressPlans(documents)) {
    for (const artifactPath of extractConcreteArtifactMetadataPaths(document.metadata.get("artifacts") || "")) {
      const owners = ownersByArtifact.get(artifactPath) || [];
      owners.push(document.repoRelativePath);
      ownersByArtifact.set(artifactPath, owners);
    }
  }

  const errors = [];
  for (const [artifactPath, owners] of ownersByArtifact.entries()) {
    if (owners.length < 2) {
      continue;
    }

    for (const owner of owners) {
      errors.push(
        createError(
          "active-plan-artifact-owner-collision",
          owner,
          `Concrete artifact \`${artifactPath}\` is claimed by multiple active non-progress plans: ${owners.join(", ")}. Keep the concrete file owner in one active plan and route other plans through distinct evidence directories or reference links.`,
        ),
      );
    }
  }

  return errors;
}

function validateActivePlanArtifactShapes(documents) {
  const errors = [];

  for (const document of getActiveNonProgressPlans(documents)) {
    for (const artifactPath of extractArtifactMetadataTokens(document.metadata.get("artifacts") || "")) {
      if (isAllowedActivePlanArtifactShape(artifactPath)) {
        continue;
      }

      errors.push(
        createError(
          "active-plan-artifact-shape",
          document.repoRelativePath,
          `Active plan artifact owner \`${artifactPath}\` is too broad or not an evidence artifact. Use \`artifacts: none\`, a concrete artifact file, a concrete evidence directory, or a narrow glob inside an evidence directory.`,
        ),
      );
    }
  }

  return errors;
}

function validateProgressEvidenceOverload(documents) {
  const progressDocument = documents.find(
    (document) => document.repoRelativePath === "docs/plans/progress.md",
  );
  if (!progressDocument) {
    return [];
  }

  const evidenceArtifacts = extractConcreteProgressEvidenceArtifacts(progressDocument.content);
  if (evidenceArtifacts.length <= PROGRESS_EVIDENCE_ARTIFACT_BUDGET) {
    return [];
  }

  return [
    createError(
      "progress-evidence-overload",
      progressDocument.repoRelativePath,
      `\`progress.md\` should keep current state and residuals, not detailed evidence logs (${evidenceArtifacts.length}/${PROGRESS_EVIDENCE_ARTIFACT_BUDGET}). Move concrete evidence artifact paths to the active owner plan or reference closeout. Artifacts: ${evidenceArtifacts.join(", ")}.`,
    ),
  ];
}

async function validateDocsMetaArtifacts(repoRoot) {
  const docsRoot = path.join(repoRoot, "docs");
  const metaFiles = await walkFiles(docsRoot, repoRoot, (entry) => entry.name.endsWith(".meta"));
  return metaFiles.map((absolutePath) =>
    createError(
      "docs-meta-artifact",
      toRepoRelative(repoRoot, absolutePath),
      "Unity `.meta` artifacts must not live under `docs/`. Remove the artifact or move Unity-owned files under Unity asset roots.",
    ),
  );
}

async function validateRuleHarnessAdvisoryMemory(repoRoot, documents) {
  const memoryAbsolutePath = path.join(repoRoot, RULE_HARNESS_ADVISORY_MEMORY_PATH);
  if (!(await pathExists(memoryAbsolutePath))) {
    return [];
  }

  let payload;
  try {
    payload = JSON.parse(await fs.readFile(memoryAbsolutePath, "utf8"));
  } catch (error) {
    return [
      createError(
        "invalid-advisory-memory-json",
        RULE_HARNESS_ADVISORY_MEMORY_PATH,
        `Rule harness advisory memory must be valid JSON. ${error.message}`,
      ),
    ];
  }

  const entries = Array.isArray(payload?.entries) ? payload.entries : [];
  const knownDocIds = new Set(
    documents
      .map((document) => document.metadata.get("doc_id"))
      .filter(Boolean),
  );
  const errors = [];

  entries.forEach((entry, index) => {
    for (const field of ["scopePath", "promotionTarget"]) {
      const value = typeof entry?.[field] === "string" ? entry[field].trim() : "";
      if (!value) {
        continue;
      }

      const staleReason = getStaleAdvisoryMemoryReferenceReason(repoRoot, knownDocIds, value);
      if (!staleReason) {
        continue;
      }

      errors.push(
        createError(
          "stale-advisory-memory-reference",
          RULE_HARNESS_ADVISORY_MEMORY_PATH,
          `Advisory memory entry #${index + 1} field \`${field}\` references stale ${staleReason}: \`${value}\`. Prune or reroute the advisory entry before it feeds recurrence planning.`,
        ),
      );
    }

    const validationHints = Array.isArray(entry?.validationHints) ? entry.validationHints : [];
    validationHints.forEach((hint) => {
      if (typeof hint !== "string" || !isPathLikeAdvisoryMemoryReference(hint)) {
        return;
      }

      const normalizedHint = normalizeRepoRelativePath(hint);
      if (isUnsafeRepoRelativeReference(normalizedHint)) {
        errors.push(
          createError(
            "stale-advisory-memory-reference",
            RULE_HARNESS_ADVISORY_MEMORY_PATH,
            `Advisory memory entry #${index + 1} validation hint uses unsafe path \`${hint}\`.`,
          ),
        );
        return;
      }

      if (!fsSyncPathExists(path.join(repoRoot, normalizedHint))) {
        errors.push(
          createError(
            "stale-advisory-memory-reference",
            RULE_HARNESS_ADVISORY_MEMORY_PATH,
            `Advisory memory entry #${index + 1} validation hint references missing path \`${hint}\`.`,
          ),
        );
      }
    });
  });

  return errors;
}

function getStaleAdvisoryMemoryReferenceReason(repoRoot, knownDocIds, value) {
  if (DOC_ID_REFERENCE_PATTERN.test(value)) {
    return knownDocIds.has(value) ? null : "doc_id";
  }

  if (!isPathLikeAdvisoryMemoryReference(value)) {
    return null;
  }

  const normalized = normalizeRepoRelativePath(value);
  if (isUnsafeRepoRelativeReference(normalized)) {
    return "path";
  }

  return fsSyncPathExists(path.join(repoRoot, normalized)) ? null : "path";
}

function isPathLikeAdvisoryMemoryReference(value) {
  const normalized = normalizeRepoRelativePath(value);
  if (!normalized || normalized.includes("*")) {
    return false;
  }

  return REPO_PATH_PREFIXES.some(
    (prefix) => normalized === prefix || normalized.startsWith(prefix),
  );
}

function isUnsafeRepoRelativeReference(value) {
  return !value || path.isAbsolute(value) || value.split("/").includes("..");
}

function fsSyncPathExists(absolutePath) {
  try {
    accessSync(absolutePath);
    return true;
  } catch {
    return false;
  }
}

function getActiveNonProgressPlans(documents) {
  return documents
    .filter((document) => document.repoRelativePath.startsWith("docs/plans/"))
    .filter((document) => document.repoRelativePath !== "docs/plans/progress.md")
    .filter((document) => document.metadata.get("상태") === "active")
    .filter((document) => document.metadata.get("role") === "plan");
}

function getDocumentMarkdownTargets(document, repoRoot) {
  const targets = [];
  const content = stripFencedCodeBlocks(document.content);
  for (const line of content.split(/\r?\n/)) {
    for (const target of extractRelativeMarkdownTargets(line)) {
      targets.push(toRepoRelative(repoRoot, path.resolve(path.dirname(document.absolutePath), target)));
    }
  }

  return targets;
}

function splitMetadataList(value) {
  return value
    .split(",")
    .map((entry) => entry.trim().replace(/^`|`$/g, ""))
    .filter(Boolean);
}

function extractArtifactMetadataTokens(value) {
  if (!value || value.trim().toLowerCase() === "none") {
    return [];
  }

  return splitMetadataList(value)
    .map((candidate) => normalizeArtifactPath(candidate))
    .filter(Boolean);
}

function extractConcreteArtifactMetadataPaths(value) {
  return [...new Set(
    extractArtifactMetadataTokens(value)
      .filter((candidate) => isConcreteRepoFilePath(candidate)),
  )].sort();
}

function extractConcreteProgressEvidenceArtifacts(content) {
  const artifacts = new Set();
  const stripped = stripFencedCodeBlocks(content);
  const pattern = /\b(?:\.\/)?(artifacts\/[^\s`),]+?\.(?:json|png|jpe?g|md|txt|log))\b/giu;
  for (const match of stripped.matchAll(pattern)) {
    const normalized = normalizeArtifactPath(match[1]);
    if (normalized) {
      artifacts.add(normalized);
    }
  }

  return [...artifacts].sort();
}

function normalizeArtifactPath(value) {
  return normalizeRepoRelativePath(
    value
      .trim()
      .replace(/^['"`]+|['"`.,;:)]+$/gu, "")
      .replace(/^\.\//u, ""),
  );
}

function isConcreteRepoFilePath(value) {
  return Boolean(value)
    && value.includes("/")
    && !value.endsWith("/")
    && /\/[^/]+\.[A-Za-z0-9]+$/u.test(value);
}

function isAllowedActivePlanArtifactShape(value) {
  if (!value || value.toLowerCase() === "none") {
    return true;
  }

  if (value.includes("*")) {
    return isNarrowArtifactGlob(value);
  }

  if (isConcreteRepoFilePath(value)) {
    return value.startsWith("artifacts/");
  }

  if (value.endsWith("/")) {
    return isConcreteEvidenceDirectory(value);
  }

  return false;
}

function isNarrowArtifactGlob(value) {
  if (!value.startsWith("artifacts/") || value.includes("**")) {
    return false;
  }

  const wildcardIndex = value.indexOf("*");
  const slashBeforeWildcard = value.lastIndexOf("/", wildcardIndex);
  if (slashBeforeWildcard < 0) {
    return false;
  }

  return isConcreteEvidenceDirectory(value.slice(0, slashBeforeWildcard + 1));
}

function isConcreteEvidenceDirectory(value) {
  if (!value.startsWith("artifacts/") || value.includes("*")) {
    return false;
  }

  const normalized = value.replace(/\/+$/u, "");
  if (!normalized || normalized === "artifacts") {
    return false;
  }

  const broadRoots = new Set([
    "artifacts/rules",
    "artifacts/stitch",
    "artifacts/unity",
    "artifacts/webgl",
  ]);
  return !broadRoots.has(normalized);
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

function extractGlobalRuleSkillNames(content) {
  const names = new Set();
  const lines = stripFencedCodeBlocks(content).split(/\r?\n/);

  for (const line of lines) {
    for (const token of extractInlineCodeTokens(line)) {
      if (isGlobalRuleSkillToken(token)) {
        names.add(token);
      }
    }
  }

  return [...names].sort((left, right) => left.localeCompare(right));
}

function getRepoLocalSkillNames(documents) {
  return documents
    .map((document) => {
      const match = document.repoRelativePath.match(/^\.codex\/skills\/(jg-[^/]+)\/SKILL\.md$/u);
      return match ? match[1] : null;
    })
    .filter(Boolean)
    .sort((left, right) => left.localeCompare(right));
}

function extractSkillRouteNames(content) {
  const names = new Set();
  const lines = stripFencedCodeBlocks(content).split(/\r?\n/);

  for (const line of lines) {
    for (const token of extractInlineCodeTokens(line)) {
      if (isSkillRouteToken(token)) {
        names.add(token);
      }
    }
  }

  return [...names].sort((left, right) => left.localeCompare(right));
}

function isSkillRouteToken(token) {
  return /^jg-[a-z0-9-]+$/u.test(token) || isGlobalRuleSkillToken(token);
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

function isGlobalRuleSkillToken(token) {
  return GLOBAL_RULE_SKILL_NAMES.has(token);
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

function createWarning(code, repoRelativePath, message, line = null) {
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
  return sortDiagnostics(errors);
}

function sortDiagnostics(diagnostics) {
  return [...diagnostics].sort((left, right) => {
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

function appendWarningReportLines(lines, warnings) {
  if (warnings.length === 0) {
    return;
  }

  lines.push(`Docs lint warnings (${warnings.length}):`);
  for (const warning of warnings) {
    const location = warning.line
      ? `${warning.path}:${warning.line}`
      : warning.path;
    lines.push(`- [warning:${warning.code}] ${location} - ${warning.message}`);
  }
}

function appendDiagnosticsSummary(lines, label, countsByCode) {
  const entries = Object.entries(countsByCode || {});
  if (entries.length === 0) {
    return;
  }

  lines.push(`${label}:`);
  for (const [code, count] of entries) {
    lines.push(`- ${code}: ${count}`);
  }
}

function appendDiagnosticsList(lines, label, diagnostics, codePrefix = "") {
  if (!Array.isArray(diagnostics) || diagnostics.length === 0) {
    return;
  }

  lines.push(label);
  for (const diagnostic of diagnostics) {
    const location = diagnostic.line
      ? `${diagnostic.path}:${diagnostic.line}`
      : diagnostic.path;
    lines.push(`- [${codePrefix}${diagnostic.code}] ${location} - ${diagnostic.message}`);
  }
}

function countDiagnosticsByCode(diagnostics) {
  return [...diagnostics]
    .sort((left, right) => left.code.localeCompare(right.code))
    .reduce((counts, diagnostic) => {
      counts[diagnostic.code] = (counts[diagnostic.code] || 0) + 1;
      return counts;
    }, {});
}

function normalizeNowOption(now) {
  if (now instanceof Date && !Number.isNaN(now.getTime())) {
    return now;
  }

  if (typeof now === "string") {
    const parsed = new Date(now);
    if (!Number.isNaN(parsed.getTime())) {
      return parsed;
    }
  }

  return new Date();
}

function parseMetadataDate(value) {
  if (typeof value !== "string") {
    return null;
  }

  const match = value.trim().match(/^(\d{4})-(\d{2})-(\d{2})$/u);
  if (!match) {
    return null;
  }

  const year = Number.parseInt(match[1], 10);
  const month = Number.parseInt(match[2], 10);
  const day = Number.parseInt(match[3], 10);
  const parsed = new Date(Date.UTC(year, month - 1, day));
  if (
    parsed.getUTCFullYear() !== year
    || parsed.getUTCMonth() !== month - 1
    || parsed.getUTCDate() !== day
  ) {
    return null;
  }

  return parsed;
}

function getWholeUtcAgeDays(startDate, now) {
  const startDay = Date.UTC(
    startDate.getUTCFullYear(),
    startDate.getUTCMonth(),
    startDate.getUTCDate(),
  );
  const nowDay = Date.UTC(
    now.getUTCFullYear(),
    now.getUTCMonth(),
    now.getUTCDate(),
  );

  return Math.max(0, Math.floor((nowDay - startDay) / (24 * 60 * 60 * 1000)));
}

function countNonblankLines(content) {
  return content.split(/\r?\n/u).filter((line) => line.trim() !== "").length;
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

function getDefaultGlobalRuleSkillsRoot() {
  const codexHome = process.env.CODEX_HOME || path.join(os.homedir(), ".codex");
  return path.join(codexHome, "skills");
}

function toGlobalRuleSkillReportPath(globalRuleSkillsRoot, absolutePath) {
  const relativePath = toPosixPath(path.relative(globalRuleSkillsRoot, absolutePath));
  if (relativePath && !relativePath.startsWith("..")) {
    return `global-rule-skills/${relativePath}`;
  }

  return toPosixPath(absolutePath);
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
    normalized === "docs/index.md" ||
    normalized.startsWith("docs/ops/") ||
    /^\.codex\/skills\/jg-[^/]+\//u.test(normalized) ||
    normalized.startsWith(".githooks/") ||
    normalized.startsWith("tools/docs-lint/") ||
    normalized.startsWith("tools/rule-harness/") ||
    normalized === ".github/workflows/docs-lint.yml" ||
    isRecurrenceCloseoutArtifactPath(normalized)
  ) {
    return true;
  }

  return false;
}

export function isRecurrenceCloseoutArtifactPath(repoRelativePath) {
  const normalized = normalizeRepoRelativePath(repoRelativePath);
  return normalized === RECURRENCE_CLOSEOUT_PATH
    || (
      normalized.startsWith(`${RECURRENCE_CLOSEOUT_DIR}/`) &&
      /^[a-z0-9][a-z0-9._-]*\.json$/u.test(normalized.slice(RECURRENCE_CLOSEOUT_DIR.length + 1))
    );
}
