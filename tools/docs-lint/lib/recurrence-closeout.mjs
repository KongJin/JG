import fs from "node:fs/promises";
import path from "node:path";
import process from "node:process";
import { execFile } from "node:child_process";
import { promisify } from "node:util";

const execFileAsync = promisify(execFile);

export const RECURRENCE_CLOSEOUT_PATH = "artifacts/rules/issue-recurrence-closeout.json";
export const RECURRENCE_CLOSEOUT_DIR = "artifacts/rules/issue-recurrence-closeout.d";
export const RECURRENCE_CHANGED_FILES_ENV = "RULES_LINT_CHANGED_FILES";

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

export async function validateRulesOnlyRecurrenceCloseout(repoRoot, options) {
  const changedFiles = await getRecurrenceChangedFiles({
    repoRoot,
    changedFiles: options.changedFiles,
  });
  const relevantChangedFiles = [];
  for (const filePath of changedFiles) {
    const normalizedPath = normalizeRepoRelativePath(filePath);
    if (
      !isRulesOnlyRecurrenceTarget(normalizedPath) ||
      isRecurrenceCloseoutArtifactPath(normalizedPath)
    ) {
      continue;
    }

    if (!(await pathExists(path.join(repoRoot, normalizedPath)))) {
      continue;
    }

    relevantChangedFiles.push(normalizedPath);
  }

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

  for (const changedFile of relevantChangedFiles) {
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
    normalized.startsWith("docs/owners/operations/") ||
    normalized.startsWith("docs/owners/ui-workflow/") ||
    normalized === ".codex/skills/IMPORT_MANIFEST.md" ||
    normalized === ".codex/skills/README.md" ||
    /^\.codex\/skills\/(?!\.system\/)[^/]+\//u.test(normalized) ||
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

function createError(code, repoRelativePath, message, line = null) {
  return {
    code,
    path: repoRelativePath,
    message,
    line,
  };
}

async function pathExists(absolutePath) {
  try {
    await fs.access(absolutePath);
    return true;
  } catch {
    return false;
  }
}

