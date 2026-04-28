import test from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs/promises";
import os from "node:os";
import path from "node:path";
import { execFile } from "node:child_process";
import { fileURLToPath } from "node:url";
import { promisify } from "node:util";

import { lintRepository } from "../lib.mjs";

const fixturesRoot = fileURLToPath(new URL("./fixtures", import.meta.url));
const execFileAsync = promisify(execFile);

function getFixturePath(name) {
  return path.join(fixturesRoot, name);
}

async function writeFile(root, repoRelativePath, content) {
  const absolutePath = path.join(root, repoRelativePath);
  await fs.mkdir(path.dirname(absolutePath), { recursive: true });
  await fs.writeFile(absolutePath, content, "utf8");
}

test("reports missing metadata", async () => {
  const result = await lintRepository(getFixturePath("missing-meta"), {
    includeGeneralChecks: true,
    includePolicyChecks: false,
  });
  assert.ok(result.errors.some((error) => error.code === "missing-meta"));
});

test("reports missing metadata in managed rule-harness prompts", async () => {
  const result = await lintRepository(getFixturePath("prompt-managed-missing-meta"), {
    includeGeneralChecks: true,
    includePolicyChecks: false,
  });
  assert.ok(
    result.errors.some(
      (error) =>
        error.code === "missing-meta" &&
        error.path === "tools/rule-harness/prompts/review.md",
    ),
  );
});

test("reports duplicate doc_id values", async () => {
  const result = await lintRepository(getFixturePath("duplicate-doc-id"), {
    includeGeneralChecks: true,
    includePolicyChecks: false,
  });
  assert.ok(result.errors.some((error) => error.code === "duplicate-doc-id"));
});

test("reports broken relative markdown links", async () => {
  const result = await lintRepository(getFixturePath("broken-relative-link"), {
    includeGeneralChecks: true,
    includePolicyChecks: false,
  });
  assert.ok(
    result.errors.some((error) => error.code === "broken-relative-link"),
  );
});

test("reports docs/index status label mismatches", async () => {
  const result = await lintRepository(getFixturePath("index-status-mismatch"), {
    includeGeneralChecks: true,
    includePolicyChecks: false,
  });
  assert.ok(
    result.errors.some((error) => error.code === "index-status-mismatch"),
  );
});

test("reports docs/index entries that are missing from the registry", async () => {
  const result = await lintRepository(getFixturePath("index-missing-entry"), {
    includeGeneralChecks: true,
    includePolicyChecks: false,
  });
  assert.ok(result.errors.some((error) => error.code === "index-missing-entry"));
});

test("reports doc_id prefixes that do not match the document path owner", async () => {
  const result = await lintRepository(getFixturePath("doc-id-path-prefix-mismatch"), {
    includeGeneralChecks: true,
    includePolicyChecks: false,
  });
  assert.ok(
    result.errors.some((error) => error.code === "doc-id-path-prefix-mismatch"),
  );
});

test("reports completed plan wording left in draft plans", async () => {
  const result = await lintRepository(getFixturePath("completed-draft-plan"), {
    includeGeneralChecks: true,
    includePolicyChecks: false,
  });
  assert.ok(result.errors.some((error) => error.code === "completed-draft-plan"));
});

test("reports stale Garage owners in active module data structure SSOT", async () => {
  const result = await lintRepository(getFixturePath("stale-module-data-structure"), {
    includeGeneralChecks: true,
    includePolicyChecks: false,
  });
  assert.ok(
    result.errors.some((error) => error.code === "stale-module-data-structure"),
  );
});

test("reports missing module data structure guard anchors", async () => {
  const result = await lintRepository(getFixturePath("stale-module-data-structure-missing-anchors"), {
    includeGeneralChecks: true,
    includePolicyChecks: false,
  });
  assert.ok(
    result.errors.some((error) => error.code === "stale-module-data-structure"),
  );
});

test("reports reference closeout wording left in active plans", async () => {
  const result = await lintRepository(getFixturePath("active-plan-reference-closeout"), {
    includeGeneralChecks: true,
    includePolicyChecks: false,
  });
  assert.ok(
    result.errors.some((error) => error.code === "active-plan-reference-closeout"),
  );
});

test("reports Unity meta artifacts under docs", async () => {
  const result = await lintRepository(getFixturePath("docs-meta-artifact"), {
    includeGeneralChecks: true,
    includePolicyChecks: false,
  });
  assert.ok(result.errors.some((error) => error.code === "docs-meta-artifact"));
});

test("reports too many active non-progress plans", async () => {
  const result = await lintRepository(getFixturePath("active-plan-budget"), {
    includeGeneralChecks: true,
    includePolicyChecks: false,
  });
  assert.ok(result.errors.some((error) => error.code === "active-plan-budget"));
});

test("accepts active non-progress plan count at budget", async () => {
  const result = await lintRepository(getFixturePath("active-plan-budget-valid"), {
    includeGeneralChecks: true,
    includePolicyChecks: false,
  });
  assert.equal(result.errors.length, 0);
});

test("reports mutual references between active non-progress plans", async () => {
  const result = await lintRepository(getFixturePath("active-plan-mutual-reference"), {
    includeGeneralChecks: true,
    includePolicyChecks: false,
  });
  assert.ok(
    result.errors.some((error) => error.code === "active-plan-mutual-reference"),
  );
});

test("accepts one-way references between active non-progress plans", async () => {
  const result = await lintRepository(getFixturePath("active-plan-one-way-reference-valid"), {
    includeGeneralChecks: true,
    includePolicyChecks: false,
  });
  assert.equal(result.errors.length, 0);
});

test("reports active plan concrete artifact owner collisions", async () => {
  const result = await lintRepository(getFixturePath("active-plan-artifact-owner-collision"), {
    includeGeneralChecks: true,
    includePolicyChecks: false,
  });
  assert.ok(
    result.errors.some((error) => error.code === "active-plan-artifact-owner-collision"),
  );
});

test("accepts shared broad artifact directories across active plans", async () => {
  const result = await lintRepository(getFixturePath("active-plan-artifact-directory-valid"), {
    includeGeneralChecks: true,
    includePolicyChecks: false,
  });
  assert.equal(result.errors.length, 0);
});

test("reports progress evidence artifact overload", async () => {
  const result = await lintRepository(getFixturePath("progress-evidence-overload"), {
    includeGeneralChecks: true,
    includePolicyChecks: false,
  });
  assert.ok(
    result.errors.some((error) => error.code === "progress-evidence-overload"),
  );
});

test("accepts progress evidence artifact count at budget", async () => {
  const result = await lintRepository(getFixturePath("progress-evidence-overload-valid"), {
    includeGeneralChecks: true,
    includePolicyChecks: false,
  });
  assert.equal(result.errors.length, 0);
});

test("ignores excluded .system skill files", async () => {
  const result = await lintRepository(getFixturePath("excluded-system-skill-ignored"), {
    includeGeneralChecks: true,
    includePolicyChecks: false,
  });
  assert.equal(result.errors.length, 0);
});

test("reports stale inline repo paths in skill markdown", async () => {
  const result = await lintRepository(getFixturePath("stale-inline-path-in-skill"), {
    includeGeneralChecks: true,
    includePolicyChecks: false,
  });
  assert.ok(result.errors.some((error) => error.code === "missing-inline-path"));
});

test("reports legacy agent paths in repo-local skills", async () => {
  const result = await lintRepository(getFixturePath("legacy-agent-token-in-skill"), {
    includeGeneralChecks: true,
    includePolicyChecks: false,
  });
  assert.ok(result.errors.some((error) => error.code === "legacy-owner-path"));
});

test("reports deprecated historical stitch paths in repo-local skills", async () => {
  const result = await lintRepository(getFixturePath("deprecated-stitch-path-in-skill"), {
    includeGeneralChecks: true,
    includePolicyChecks: false,
  });
  assert.ok(
    result.errors.some((error) => error.code === "deprecated-skill-inline-path"),
  );
});

test("reports deprecated historical stitch path mentions in repo-local skills", async () => {
  const result = await lintRepository(getFixturePath("deprecated-stitch-mention-in-skill"), {
    includeGeneralChecks: true,
    includePolicyChecks: false,
  });
  assert.ok(
    result.errors.some((error) => error.code === "deprecated-skill-path-mention"),
  );
});

test("reports historical stitch links inside active documents", async () => {
  const result = await lintRepository(getFixturePath("historical-link-in-active-doc"), {
    includeGeneralChecks: true,
    includePolicyChecks: false,
  });
  assert.ok(
    result.errors.some((error) => error.code === "historical-link-in-active-doc"),
  );
});

test("reports missing concrete contract artifacts", async () => {
  const result = await lintRepository(getFixturePath("missing-contract-artifact"), {
    includeGeneralChecks: true,
    includePolicyChecks: false,
  });
  assert.ok(
    result.errors.some((error) => error.code === "missing-contract-artifact"),
  );
});

test("reports deleted skill references still mentioned from SKILL.md", async () => {
  const result = await lintRepository(getFixturePath("deleted-reference-mentioned"), {
    includeGeneralChecks: true,
    includePolicyChecks: false,
  });
  assert.ok(result.errors.some((error) => error.code === "missing-inline-path"));
});

test("reports inline owner doc_id references that do not resolve", async () => {
  const result = await lintRepository(getFixturePath("missing-doc-id-reference"), {
    includeGeneralChecks: true,
    includePolicyChecks: false,
  });
  assert.ok(
    result.errors.some((error) => error.code === "missing-doc-id-reference"),
  );
});

test("ignores valid search-pattern inline code", async () => {
  const result = await lintRepository(getFixturePath("valid-search-pattern-ignored"), {
    includeGeneralChecks: true,
    includePolicyChecks: false,
  });
  assert.equal(result.errors.length, 0);
});

test("reports missing Plan Mode routing in AGENTS.md", async () => {
  const result = await lintRepository(getFixturePath("missing-plan-routing-agents"), {
    includeGeneralChecks: false,
    includePolicyChecks: true,
  });
  assert.ok(
    result.errors.some((error) => error.code === "missing-plan-mode-routing"),
  );
});

test("reports missing Plan Mode owner route in docs/index.md", async () => {
  const result = await lintRepository(getFixturePath("missing-plan-routing-index"), {
    includeGeneralChecks: false,
    includePolicyChecks: true,
  });
  assert.ok(
    result.errors.some((error) => error.code === "missing-plan-mode-owner-route"),
  );
});

test("reports missing inspection-only clause in mutating repo-local skill", async () => {
  const result = await lintRepository(getFixturePath("missing-skill-inspection-clause"), {
    includeGeneralChecks: false,
    includePolicyChecks: true,
  });
  assert.ok(
    result.errors.some((error) => error.code === "missing-skill-inspection-clause"),
  );
});

test("reports missing cohesion/coupling owner route in repo-local skill", async () => {
  const result = await lintRepository(getFixturePath("missing-skill-owner-route"), {
    includeGeneralChecks: false,
    includePolicyChecks: true,
  });
  assert.ok(
    result.errors.some((error) => error.code === "missing-skill-owner-route"),
  );
});

test("reports missing recurrence closeout artifact for rules-only changes", async () => {
  const result = await lintRepository(getFixturePath("missing-recurrence-closeout-artifact"), {
    includeGeneralChecks: false,
    includePolicyChecks: true,
    changedFiles: ["docs/index.md"],
  });
  assert.ok(
    result.errors.some((error) => error.code === "missing-recurrence-closeout-artifact"),
  );
});

test("reports recurrence closeout artifact that was not updated in the same change", async () => {
  const result = await lintRepository(getFixturePath("valid-recurrence-closeout"), {
    includeGeneralChecks: false,
    includePolicyChecks: true,
    changedFiles: ["docs/index.md"],
  });
  assert.ok(
    result.errors.some((error) => error.code === "missing-recurrence-closeout-update"),
  );
});

test("auto-detects dirty recurrence closeout changes without changedFiles or env", async () => {
  const previousChangedFilesEnv = process.env.RULES_LINT_CHANGED_FILES;
  delete process.env.RULES_LINT_CHANGED_FILES;

  const repoRoot = await fs.mkdtemp(path.join(os.tmpdir(), "docs-lint-dirty-"));
  try {
    await execFileAsync("git", ["init"], { cwd: repoRoot });
    await writeFile(
      repoRoot,
      "AGENTS.md",
      `# AGENTS.md

> 마지막 업데이트: 2026-04-28
> 상태: active
> doc_id: repo.agents
> role: entry
> owner_scope: test entry
> upstream: none
> artifacts: none

Plan Mode / Codex 운영 규칙은 docs/index.md에서 current path 확인 후 rule-operations owner 문서로 라우팅하고, 그 lane에서는 mutation 금지.
`,
    );
    await writeFile(
      repoRoot,
      "docs/index.md",
      `# Docs Index

> 마지막 업데이트: 2026-04-28
> 상태: active
> doc_id: docs.index
> role: entry
> owner_scope: test index
> upstream: repo.agents
> artifacts: none

Plan Mode / Codex 운영 규칙 확인: rule-operations owner 문서.
`,
    );
    await writeFile(
      repoRoot,
      "artifacts/rules/issue-recurrence-closeout.json",
      `${JSON.stringify({
        schemaVersion: 1,
        updatedAt: "2026-04-28",
        scope: "rules-only",
        issueDetected: false,
        declaredLane: "test",
        observedMutationClass: "docs",
        acceptanceEvidenceClass: "rules lint",
        escalationRequired: false,
        rootCause: "",
        prevention: "",
        verification: "rules lint",
        blockedReason: "",
        changedPaths: ["artifacts/rules/issue-recurrence-closeout.json"],
      }, null, 2)}\n`,
    );

    const result = await lintRepository(repoRoot, {
      includeGeneralChecks: false,
      includePolicyChecks: true,
    });
    assert.ok(
      result.errors.some((error) => error.code === "recurrence-closeout-missing-changed-path"),
    );
  } finally {
    if (previousChangedFilesEnv === undefined) {
      delete process.env.RULES_LINT_CHANGED_FILES;
    } else {
      process.env.RULES_LINT_CHANGED_FILES = previousChangedFilesEnv;
    }
    await fs.rm(repoRoot, { recursive: true, force: true });
  }
});

test("reports missing required recurrence closeout fields when issueDetected is true", async () => {
  const result = await lintRepository(getFixturePath("invalid-recurrence-closeout"), {
    includeGeneralChecks: false,
    includePolicyChecks: true,
    changedFiles: [
      "docs/index.md",
      "artifacts/rules/issue-recurrence-closeout.json",
    ],
  });
  assert.ok(
    result.errors.some((error) => error.code === "missing-recurrence-closeout-field"),
  );
});

test("accepts valid recurrence closeout artifact for rules-only changes", async () => {
  const result = await lintRepository(getFixturePath("valid-recurrence-closeout"), {
    includeGeneralChecks: false,
    includePolicyChecks: true,
    changedFiles: [
      "docs/index.md",
      "artifacts/rules/issue-recurrence-closeout.json",
    ],
  });
  assert.equal(result.errors.length, 0);
});
