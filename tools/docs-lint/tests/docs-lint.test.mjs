import test from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs/promises";
import os from "node:os";
import path from "node:path";
import { execFile } from "node:child_process";
import { fileURLToPath } from "node:url";
import { promisify } from "node:util";

import {
  buildDocsHealthReport,
  formatDocsHealthReport,
  formatLintReport,
  lintRepository,
} from "../lib.mjs";

const fixturesRoot = fileURLToPath(new URL("./fixtures", import.meta.url));
const docsLintToolRoot = fileURLToPath(new URL("..", import.meta.url));
const execFileAsync = promisify(execFile);

function getFixturePath(name) {
  return path.join(fixturesRoot, name);
}

async function writeFile(root, repoRelativePath, content) {
  const absolutePath = path.join(root, repoRelativePath);
  await fs.mkdir(path.dirname(absolutePath), { recursive: true });
  await fs.writeFile(absolutePath, content, "utf8");
}

const recurrenceCoverageText = [
  "active plan artifact owner shape drift",
  "active plan concrete artifact owner collision",
  "progress evidence overload",
].join("; ");

async function writeMinimalRecurrenceRepo(root, {
  rootCause,
  blockedReason = "",
  prevention = `Added owner-doc and lint coverage for ${recurrenceCoverageText}.`,
  verification = "Ran rules:lint in the same change.",
}) {
  await writeFile(
    root,
    "AGENTS.md",
    `# AGENTS

Plan Mode 또는 Codex 운영 작업은 \`docs/index.md\`를 통해 \`rule-operations\` owner 문서로 라우팅한다.
해당 lane에서는 mutation 금지.
`,
  );
  await writeFile(
    root,
    "docs/index.md",
    `# Docs Index

Plan Mode / Codex 운영 규칙 확인: \`rule-operations\`
`,
  );
  await writeFile(
    root,
    "artifacts/rules/issue-recurrence-closeout.json",
    `${JSON.stringify({
      schemaVersion: 1,
      updatedAt: "2026-04-30",
      scope: "rules-only",
      issueDetected: true,
      declaredLane: "rules-only recurrence prevention",
      observedMutationClass: "docs and lint policy",
      acceptanceEvidenceClass: "rules-only closeout",
      escalationRequired: false,
      rootCause,
      prevention,
      verification,
      blockedReason,
      changedPaths: [
        "docs/index.md",
        "artifacts/rules/issue-recurrence-closeout.json",
      ],
    }, null, 2)}\n`,
  );
}

async function writeMinimalActivePlanArtifactRepo(root, artifactMetadata) {
  await writeFile(
    root,
    "AGENTS.md",
    `# AGENTS

> 마지막 업데이트: 2026-04-30
> 상태: active
> doc_id: repo.agents
> role: entry
> owner_scope: fixture entry
> upstream: none
> artifacts: none
`,
  );
  await writeFile(
    root,
    "docs/index.md",
    `# Docs Index

> 마지막 업데이트: 2026-04-30
> 상태: active
> doc_id: docs.index
> role: entry
> owner_scope: fixture index
> upstream: repo.agents
> artifacts: none

- \`active\`: [demo.md](./plans/active/demo.md) - active plan
`,
  );
  await writeFile(
    root,
    "docs/plans/active/demo.md",
    `# Demo Plan

> 마지막 업데이트: 2026-04-30
> 상태: active
> doc_id: plans.demo
> role: plan
> owner_scope: fixture active plan
> upstream: docs.index
> artifacts: ${artifactMetadata}

Fixture active plan.
`,
  );
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

test("reports broken relative markdown links in optional external rule skills", async () => {
  const repoRoot = await fs.mkdtemp(path.join(os.tmpdir(), "docs-lint-global-rule-links-"));
  const globalRuleSkillsRoot = await fs.mkdtemp(path.join(os.tmpdir(), "global-rule-skills-"));
  try {
    await writeFile(
      repoRoot,
      "AGENTS.md",
      `# AGENTS

> 마지막 업데이트: 2026-05-01
> 상태: active
> doc_id: repo.agents
> role: entry
> owner_scope: fixture entry
> upstream: none
> artifacts: none
`,
    );
    await writeFile(
      repoRoot,
      "docs/index.md",
      `# Docs Index

> 마지막 업데이트: 2026-05-01
> 상태: active
> doc_id: docs.index
> role: entry
> owner_scope: fixture index
> upstream: repo.agents
> artifacts: none
`,
    );
    await writeFile(
      globalRuleSkillsRoot,
      "rule-demo/SKILL.md",
      `# Demo

[Broken](missing.md)
`,
    );

    const result = await lintRepository(repoRoot, {
      includeGeneralChecks: true,
      includePolicyChecks: false,
      globalRuleSkillsRoot,
    });
    assert.ok(
      result.errors.some((error) => error.code === "global-rule-broken-relative-link"),
    );
  } finally {
    await fs.rm(repoRoot, { recursive: true, force: true });
    await fs.rm(globalRuleSkillsRoot, { recursive: true, force: true });
  }
});

test("accepts valid relative markdown links in optional external rule skills", async () => {
  const repoRoot = await fs.mkdtemp(path.join(os.tmpdir(), "docs-lint-global-rule-links-valid-"));
  const globalRuleSkillsRoot = await fs.mkdtemp(path.join(os.tmpdir(), "global-rule-skills-valid-"));
  try {
    await writeFile(
      repoRoot,
      "AGENTS.md",
      `# AGENTS

> 마지막 업데이트: 2026-05-01
> 상태: active
> doc_id: repo.agents
> role: entry
> owner_scope: fixture entry
> upstream: none
> artifacts: none
`,
    );
    await writeFile(
      repoRoot,
      "docs/index.md",
      `# Docs Index

> 마지막 업데이트: 2026-05-01
> 상태: active
> doc_id: docs.index
> role: entry
> owner_scope: fixture index
> upstream: repo.agents
> artifacts: none
`,
    );
    await writeFile(globalRuleSkillsRoot, "rule-demo/SKILL.md", "[Valid](reference.md)\n");
    await writeFile(globalRuleSkillsRoot, "rule-demo/reference.md", "# Reference\n");

    const result = await lintRepository(repoRoot, {
      includeGeneralChecks: true,
      includePolicyChecks: false,
      globalRuleSkillsRoot,
    });
    assert.equal(result.errors.length, 0);
  } finally {
    await fs.rm(repoRoot, { recursive: true, force: true });
    await fs.rm(globalRuleSkillsRoot, { recursive: true, force: true });
  }
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

test("reports bare plan rereview clean without checked scope", async () => {
  const result = await lintRepository(getFixturePath("bare-plan-rereview-clean"), {
    includeGeneralChecks: true,
    includePolicyChecks: false,
  });
  assert.ok(
    result.errors.some((error) => error.code === "bare-plan-rereview-clean"),
  );
});

test("reports Unity meta artifacts under docs", async () => {
  const result = await lintRepository(getFixturePath("docs-meta-artifact"), {
    includeGeneralChecks: true,
    includePolicyChecks: false,
  });
  assert.ok(result.errors.some((error) => error.code === "docs-meta-artifact"));
});

test("reports stale repo paths in rules artifact markdown inventories", async () => {
  const repoRoot = await fs.mkdtemp(path.join(os.tmpdir(), "docs-lint-rules-artifact-stale-"));
  try {
    await writeFile(
      repoRoot,
      "AGENTS.md",
      `# AGENTS

> 마지막 업데이트: 2026-05-01
> 상태: active
> doc_id: repo.agents
> role: entry
> owner_scope: fixture entry
> upstream: none
> artifacts: none
`,
    );
    await writeFile(
      repoRoot,
      "docs/index.md",
      `# Docs Index

> 마지막 업데이트: 2026-05-01
> 상태: active
> doc_id: docs.index
> role: entry
> owner_scope: fixture index
> upstream: repo.agents
> artifacts: none
`,
    );
    await writeFile(
      repoRoot,
      "artifacts/rules/inventory.md",
      "# Inventory\n\nStale owner: `docs/owners/operations/missing_owner.md`\n",
    );

    const result = await lintRepository(repoRoot, {
      includeGeneralChecks: true,
      includePolicyChecks: false,
    });
    assert.ok(
      result.errors.some((error) => error.code === "stale-rules-artifact-reference"),
    );
  } finally {
    await fs.rm(repoRoot, { recursive: true, force: true });
  }
});

test("accepts existing repo paths and glob examples in rules artifact markdown inventories", async () => {
  const repoRoot = await fs.mkdtemp(path.join(os.tmpdir(), "docs-lint-rules-artifact-valid-"));
  try {
    await writeFile(
      repoRoot,
      "AGENTS.md",
      `# AGENTS

> 마지막 업데이트: 2026-05-01
> 상태: active
> doc_id: repo.agents
> role: entry
> owner_scope: fixture entry
> upstream: none
> artifacts: none
`,
    );
    await writeFile(
      repoRoot,
      "docs/index.md",
      `# Docs Index

> 마지막 업데이트: 2026-05-01
> 상태: active
> doc_id: docs.index
> role: entry
> owner_scope: fixture index
> upstream: repo.agents
> artifacts: none
`,
    );
    await writeFile(
      repoRoot,
      "docs/owners/operations/current_owner.md",
      `# Current Owner

> 마지막 업데이트: 2026-05-01
> 상태: active
> doc_id: ops.current-owner
> role: ssot
> owner_scope: fixture owner
> upstream: docs.index
> artifacts: none
`,
    );
    await writeFile(
      repoRoot,
      "artifacts/rules/inventory.md",
      "# Inventory\n\nCurrent owner: `docs/owners/operations/current_owner.md`; example glob: `docs/plans/{current,active,reference,historical}/*.md`\n",
    );

    const result = await lintRepository(repoRoot, {
      includeGeneralChecks: true,
      includePolicyChecks: false,
    });
    assert.ok(
      !result.errors.some((error) => error.code === "stale-rules-artifact-reference"),
    );
  } finally {
    await fs.rm(repoRoot, { recursive: true, force: true });
  }
});

test("reports owner-only policy body headings in entry documents", async () => {
  const repoRoot = await fs.mkdtemp(path.join(os.tmpdir(), "docs-lint-entry-policy-"));
  try {
    await writeFile(
      repoRoot,
      "AGENTS.md",
      `# AGENTS

> 마지막 업데이트: 2026-05-01
> 상태: active
> doc_id: repo.agents
> role: entry
> owner_scope: fixture entry
> upstream: none
> artifacts: none

## 상위 원칙

Fixture policy body that belongs in an owner document.
`,
    );
    await writeFile(
      repoRoot,
      "docs/index.md",
      `# Docs Index

> 마지막 업데이트: 2026-05-01
> 상태: active
> doc_id: docs.index
> role: entry
> owner_scope: fixture index
> upstream: repo.agents
> artifacts: none
`,
    );

    const result = await lintRepository(repoRoot, {
      includeGeneralChecks: true,
      includePolicyChecks: false,
    });
    assert.ok(result.errors.some((error) => error.code === "entry-policy-body"));
  } finally {
    await fs.rm(repoRoot, { recursive: true, force: true });
  }
});

test("accepts sharp-edge reminders in entry documents", async () => {
  const repoRoot = await fs.mkdtemp(path.join(os.tmpdir(), "docs-lint-entry-sharp-"));
  try {
    await writeFile(
      repoRoot,
      "AGENTS.md",
      `# AGENTS

> 마지막 업데이트: 2026-05-01
> 상태: active
> doc_id: repo.agents
> role: entry
> owner_scope: fixture entry
> upstream: none
> artifacts: none

## Sharp edges

- Entry docs route only.
- Mechanical pass and actual acceptance stay separate.
`,
    );
    await writeFile(
      repoRoot,
      "docs/index.md",
      `# Docs Index

> 마지막 업데이트: 2026-05-01
> 상태: active
> doc_id: docs.index
> role: entry
> owner_scope: fixture index
> upstream: repo.agents
> artifacts: none
`,
    );

    const result = await lintRepository(repoRoot, {
      includeGeneralChecks: true,
      includePolicyChecks: false,
    });
    assert.equal(result.errors.length, 0);
    assert.ok(!result.warnings.some((warning) => warning.code === "entry-policy-body"));
  } finally {
    await fs.rm(repoRoot, { recursive: true, force: true });
  }
});

test("reports stale active documents as warnings only", async () => {
  const repoRoot = await fs.mkdtemp(path.join(os.tmpdir(), "docs-lint-stale-active-"));
  try {
    await writeFile(
      repoRoot,
      "AGENTS.md",
      `# AGENTS

> 마지막 업데이트: 2026-05-20
> 상태: active
> doc_id: repo.agents
> role: entry
> owner_scope: fixture entry
> upstream: none
> artifacts: none
`,
    );
    await writeFile(
      repoRoot,
      "docs/index.md",
      `# Docs Index

> 마지막 업데이트: 2026-05-20
> 상태: active
> doc_id: docs.index
> role: entry
> owner_scope: fixture index
> upstream: repo.agents
> artifacts: none

- \`active\`: [demo.md](./plans/active/demo.md) - demo plan
- \`active\`: [demo_ops.md](./owners/operations/demo_ops.md) - demo ops
`,
    );
    await writeFile(
      repoRoot,
      "docs/plans/active/demo.md",
      `# Demo Plan

> 마지막 업데이트: 2026-05-01
> 상태: active
> doc_id: plans.demo
> role: plan
> owner_scope: fixture plan
> upstream: docs.index
> artifacts: none
`,
    );
    await writeFile(
      repoRoot,
      "docs/owners/operations/demo_ops.md",
      `# Demo Ops

> 마지막 업데이트: 2026-05-01
> 상태: active
> doc_id: ops.demo-ops
> role: ssot
> owner_scope: fixture ops
> upstream: docs.index
> artifacts: none
`,
    );

    const result = await lintRepository(repoRoot, {
      includeGeneralChecks: true,
      includePolicyChecks: false,
      now: "2026-06-20T00:00:00Z",
    });
    assert.equal(result.errors.length, 0);
    assert.ok(result.warnings.some((warning) => warning.code === "stale-active-plan"));
    assert.ok(result.warnings.some((warning) => warning.code === "stale-active-doc"));
  } finally {
    await fs.rm(repoRoot, { recursive: true, force: true });
  }
});

test("reports oversized managed documents as warnings only", async () => {
  const repoRoot = await fs.mkdtemp(path.join(os.tmpdir(), "docs-lint-size-warning-"));
  const agentsPadding = Array.from({ length: 82 }, (_, index) => `- route reminder ${index + 1}`).join("\n");
  const progressPadding = Array.from({ length: 82 }, (_, index) => `- current state ${index + 1}`).join("\n");
  const planPadding = Array.from({ length: 122 }, (_, index) => `- execution note ${index + 1}`).join("\n");

  try {
    await writeFile(
      repoRoot,
      "AGENTS.md",
      `# AGENTS

> 마지막 업데이트: 2026-05-20
> 상태: active
> doc_id: repo.agents
> role: entry
> owner_scope: fixture entry
> upstream: none
> artifacts: none

${agentsPadding}
`,
    );
    await writeFile(
      repoRoot,
      "docs/index.md",
      `# Docs Index

> 마지막 업데이트: 2026-05-20
> 상태: active
> doc_id: docs.index
> role: entry
> owner_scope: fixture index
> upstream: repo.agents
> artifacts: none

- \`active\`: [progress.md](./plans/current/progress.md) - progress
- \`active\`: [demo.md](./plans/active/demo.md) - demo plan
`,
    );
    await writeFile(
      repoRoot,
      "docs/plans/current/progress.md",
      `# Progress

> 마지막 업데이트: 2026-05-20
> 상태: active
> doc_id: plans.progress
> role: plan
> owner_scope: fixture progress
> upstream: docs.index
> artifacts: none

${progressPadding}
`,
    );
    await writeFile(
      repoRoot,
      "docs/plans/active/demo.md",
      `# Demo Plan

> 마지막 업데이트: 2026-05-20
> 상태: active
> doc_id: plans.demo
> role: plan
> owner_scope: fixture plan
> upstream: docs.index
> artifacts: none

${planPadding}
`,
    );

    const result = await lintRepository(repoRoot, {
      includeGeneralChecks: true,
      includePolicyChecks: false,
      now: "2026-05-20T00:00:00Z",
    });
    assert.equal(result.errors.length, 0);
    assert.ok(result.warnings.some((warning) => warning.code === "entry-size-advisory"));
    assert.ok(result.warnings.some((warning) => warning.code === "progress-size-advisory"));
    assert.ok(result.warnings.some((warning) => warning.code === "active-plan-size-advisory"));
  } finally {
    await fs.rm(repoRoot, { recursive: true, force: true });
  }
});

test("formats warning-only results as passed", () => {
  const report = formatLintReport({
    managedDocPaths: ["AGENTS.md"],
    errors: [],
    warnings: [
      {
        code: "entry-size-advisory",
        line: null,
        message: "Fixture warning.",
        path: "AGENTS.md",
      },
    ],
  });

  assert.match(report, /Docs lint passed with 1 warning/);
  assert.match(report, /\[warning:entry-size-advisory\]/);
});

test("CLI wrappers exit zero for warning-only reports", async () => {
  const repoRoot = await fs.mkdtemp(path.join(os.tmpdir(), "docs-lint-cli-warning-"));
  const tempToolRoot = path.join(repoRoot, "tools/docs-lint");
  const stubLib = `export async function lintRepository() {
  return {
    managedDocPaths: ["AGENTS.md"],
    documents: [],
    errors: [],
    warnings: [
      {
        code: "fixture-warning",
        line: null,
        message: "Fixture warning only.",
        path: "AGENTS.md",
      },
    ],
  };
}

export function formatLintReport(result) {
  return \`stub pass with \${result.warnings.length} warning(s)\`;
}
`;

  try {
    await fs.mkdir(tempToolRoot, { recursive: true });
    await fs.copyFile(
      path.join(docsLintToolRoot, "docs-lint.mjs"),
      path.join(tempToolRoot, "docs-lint.mjs"),
    );
    await fs.copyFile(
      path.join(docsLintToolRoot, "policy-lint.mjs"),
      path.join(tempToolRoot, "policy-lint.mjs"),
    );
    await writeFile(repoRoot, "tools/docs-lint/lib.mjs", stubLib);

    for (const scriptName of ["docs-lint.mjs", "policy-lint.mjs"]) {
      const { stdout } = await execFileAsync(
        process.execPath,
        [path.join(tempToolRoot, scriptName)],
        { cwd: repoRoot },
      );
      assert.match(stdout, /stub pass with 1 warning/);
    }
  } finally {
    await fs.rm(repoRoot, { recursive: true, force: true });
  }
});

test("reports oversized skill entries as warnings only", async () => {
  const repoRoot = await fs.mkdtemp(path.join(os.tmpdir(), "docs-lint-skill-entry-size-"));
  const skillPadding = Array.from({ length: 162 }, (_, index) => `- route note ${index + 1}`).join("\n");

  try {
    await writeFile(
      repoRoot,
      "AGENTS.md",
      `# AGENTS

> 마지막 업데이트: 2026-05-20
> 상태: active
> doc_id: repo.agents
> role: entry
> owner_scope: fixture entry
> upstream: none
> artifacts: none
`,
    );
    await writeFile(
      repoRoot,
      "docs/index.md",
      `# Docs Index

> 마지막 업데이트: 2026-05-20
> 상태: active
> doc_id: docs.index
> role: entry
> owner_scope: fixture index
> upstream: repo.agents
> artifacts: none
`,
    );
    await writeFile(
      repoRoot,
      ".codex/skills/jg-demo/SKILL.md",
      `# Demo Skill

> 마지막 업데이트: 2026-05-20
> 상태: active
> doc_id: skill.demo
> role: skill-entry
> owner_scope: fixture skill
> upstream: repo.agents
> artifacts: none

${skillPadding}
`,
    );

    const result = await lintRepository(repoRoot, {
      includeGeneralChecks: true,
      includePolicyChecks: false,
      now: "2026-05-20T00:00:00Z",
    });
    assert.equal(result.errors.length, 0);
    assert.ok(result.warnings.some((warning) => warning.code === "skill-entry-size-advisory"));
  } finally {
    await fs.rm(repoRoot, { recursive: true, force: true });
  }
});

test("builds docs health summaries from lint results", () => {
  const result = {
    managedDocPaths: [
      "AGENTS.md",
      "docs/index.md",
      "docs/plans/current/progress.md",
      "docs/plans/active/demo.md",
    ],
    documents: [
      {
        repoRelativePath: "docs/plans/current/progress.md",
        metadata: new Map([
          ["상태", "active"],
          ["role", "plan"],
        ]),
      },
      {
        repoRelativePath: "docs/plans/active/demo.md",
        metadata: new Map([
          ["상태", "active"],
          ["role", "plan"],
        ]),
      },
    ],
    errors: [
      {
        code: "fixture-error",
        line: null,
        message: "Fixture error.",
        path: "AGENTS.md",
      },
    ],
    warnings: [
      {
        code: "fixture-warning",
        line: null,
        message: "Fixture warning.",
        path: "docs/plans/active/demo.md",
      },
    ],
  };

  const report = buildDocsHealthReport(result);
  assert.equal(report.managedDocumentCount, 4);
  assert.equal(report.blockingIssueCount, 1);
  assert.equal(report.warningCount, 1);
  assert.equal(report.activeNonProgressPlanBudget.current, 1);
  assert.deepEqual(report.activeNonProgressPlanBudget.paths, ["docs/plans/active/demo.md"]);
  assert.deepEqual(report.errorsByCode, { "fixture-error": 1 });
  assert.deepEqual(report.warningsByCode, { "fixture-warning": 1 });
  assert.match(formatDocsHealthReport(report), /Docs health report/);
});

test("docs health CLI prints text and JSON without failing on diagnostics", async () => {
  const repoRoot = await fs.mkdtemp(path.join(os.tmpdir(), "docs-health-cli-"));
  const tempToolRoot = path.join(repoRoot, "tools/docs-lint");
  const stubLib = `export async function lintRepository() {
  return {
    managedDocPaths: ["AGENTS.md"],
    documents: [],
    errors: [
      {
        code: "fixture-error",
        line: null,
        message: "Fixture error.",
        path: "AGENTS.md",
      },
    ],
    warnings: [
      {
        code: "fixture-warning",
        line: null,
        message: "Fixture warning.",
        path: "AGENTS.md",
      },
    ],
  };
}

export function buildDocsHealthReport(result) {
  return {
    schemaVersion: 1,
    managedDocumentCount: result.managedDocPaths.length,
    blockingIssueCount: result.errors.length,
    warningCount: result.warnings.length,
    activeNonProgressPlanBudget: { current: 0, limit: 5, status: "ok", paths: [] },
    errorsByCode: { "fixture-error": 1 },
    warningsByCode: { "fixture-warning": 1 },
    errors: result.errors,
    warnings: result.warnings,
  };
}

export function formatDocsHealthReport(report) {
  return \`stub health errors=\${report.blockingIssueCount} warnings=\${report.warningCount}\`;
}
`;

  try {
    await fs.mkdir(tempToolRoot, { recursive: true });
    await fs.copyFile(
      path.join(docsLintToolRoot, "docs-health.mjs"),
      path.join(tempToolRoot, "docs-health.mjs"),
    );
    await writeFile(repoRoot, "tools/docs-lint/lib.mjs", stubLib);

    const textResult = await execFileAsync(
      process.execPath,
      [path.join(tempToolRoot, "docs-health.mjs")],
      { cwd: repoRoot },
    );
    assert.match(textResult.stdout, /stub health errors=1 warnings=1/);

    const jsonResult = await execFileAsync(
      process.execPath,
      [path.join(tempToolRoot, "docs-health.mjs"), "--json"],
      { cwd: repoRoot },
    );
    const payload = JSON.parse(jsonResult.stdout);
    assert.equal(payload.blockingIssueCount, 1);
    assert.equal(payload.warningCount, 1);
  } finally {
    await fs.rm(repoRoot, { recursive: true, force: true });
  }
});

test("reports stale rule harness advisory memory references", async () => {
  const repoRoot = await fs.mkdtemp(path.join(os.tmpdir(), "docs-lint-advisory-memory-"));
  try {
    await writeFile(
      repoRoot,
      "AGENTS.md",
      `# AGENTS

> 마지막 업데이트: 2026-05-01
> 상태: active
> doc_id: repo.agents
> role: entry
> owner_scope: fixture entry
> upstream: none
> artifacts: none
`,
    );
    await writeFile(
      repoRoot,
      "docs/index.md",
      `# Docs Index

> 마지막 업데이트: 2026-05-01
> 상태: active
> doc_id: docs.index
> role: entry
> owner_scope: fixture index
> upstream: repo.agents
> artifacts: none
`,
    );
    await writeFile(
      repoRoot,
      "tools/rule-harness/memory/advisory-memory.json",
      `${JSON.stringify({
        schemaVersion: 1,
        entries: [
          {
            scopePath: "docs/missing.md",
            promotionTarget: "ops.missing",
            validationHints: ["Tests/Missing/Run.ps1"],
          },
        ],
      }, null, 2)}\n`,
    );

    const result = await lintRepository(repoRoot, {
      includeGeneralChecks: true,
      includePolicyChecks: false,
    });
    assert.ok(
      result.errors.some((error) => error.code === "stale-advisory-memory-reference"),
    );
  } finally {
    await fs.rm(repoRoot, { recursive: true, force: true });
  }
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

test("accepts distinct evidence directories across active plans", async () => {
  const result = await lintRepository(getFixturePath("active-plan-artifact-directory-valid"), {
    includeGeneralChecks: true,
    includePolicyChecks: false,
  });
  assert.equal(result.errors.length, 0);
});

test("reports broad active plan artifact owner shapes", async () => {
  const rejectedArtifactShapes = [
    "`docs/plans/{current,active,reference,historical}/*.md`",
    "`Assets/**`",
    "`Build/**`",
    "`Assets/UI/UIToolkit/AccountSync/`",
    "`artifacts/unity/`",
    "`artifacts/unity/*uitk*`",
  ];

  for (const artifactMetadata of rejectedArtifactShapes) {
    const repoRoot = await fs.mkdtemp(path.join(os.tmpdir(), "docs-lint-artifact-shape-"));
    try {
      await writeMinimalActivePlanArtifactRepo(repoRoot, artifactMetadata);
      const result = await lintRepository(repoRoot, {
        includeGeneralChecks: true,
        includePolicyChecks: false,
      });
      assert.ok(
        result.errors.some((error) => error.code === "active-plan-artifact-shape"),
        `Expected active-plan-artifact-shape for: ${artifactMetadata}`,
      );
    } finally {
      await fs.rm(repoRoot, { recursive: true, force: true });
    }
  }
});

test("accepts active plan evidence artifact shapes", async () => {
  const acceptedArtifactShapes = [
    "`artifacts/unity/game-flow/actual-flow/`",
    "`artifacts/nova1492/`",
    "`artifacts/unity/game-flow/*.png`",
    "`artifacts/unity/account-sync-uitk-preview-gameview.png`",
    "none",
  ];

  for (const artifactMetadata of acceptedArtifactShapes) {
    const repoRoot = await fs.mkdtemp(path.join(os.tmpdir(), "docs-lint-artifact-shape-valid-"));
    try {
      await writeMinimalActivePlanArtifactRepo(repoRoot, artifactMetadata);
      const result = await lintRepository(repoRoot, {
        includeGeneralChecks: true,
        includePolicyChecks: false,
      });
      assert.equal(
        result.errors.length,
        0,
        `Expected no lint errors for: ${artifactMetadata}; got ${result.errors.map((error) => error.code).join(", ")}`,
      );
    } finally {
      await fs.rm(repoRoot, { recursive: true, force: true });
    }
  }
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

test("checks imported non-jg skill links without requiring owner metadata", async () => {
  const repoRoot = await fs.mkdtemp(path.join(os.tmpdir(), "docs-lint-imported-skill-"));
  try {
    await writeFile(
      repoRoot,
      "AGENTS.md",
      `# AGENTS

> 마지막 업데이트: 2026-05-05
> 상태: active
> doc_id: repo.agents
> role: entry
> owner_scope: fixture entry
> upstream: none
> artifacts: none
`,
    );
    await writeFile(
      repoRoot,
      "docs/index.md",
      `# Docs Index

> 마지막 업데이트: 2026-05-05
> 상태: active
> doc_id: docs.index
> role: entry
> owner_scope: fixture index
> upstream: repo.agents
> artifacts: none
`,
    );
    await writeFile(
      repoRoot,
      ".codex/skills/docx/SKILL.md",
      `---
name: docx
description: Test imported skill.
---

# Imported Skill

[Reference](reference.md)
`,
    );
    await writeFile(repoRoot, ".codex/skills/docx/reference.md", "# Reference\n");

    const result = await lintRepository(repoRoot, {
      includeGeneralChecks: true,
      includePolicyChecks: false,
    });
    assert.equal(result.errors.length, 0);
    assert.ok(result.managedDocPaths.includes(".codex/skills/docx/SKILL.md"));
  } finally {
    await fs.rm(repoRoot, { recursive: true, force: true });
  }
});

test("reports broken relative links in imported non-jg skills", async () => {
  const repoRoot = await fs.mkdtemp(path.join(os.tmpdir(), "docs-lint-imported-skill-broken-"));
  try {
    await writeFile(
      repoRoot,
      "AGENTS.md",
      `# AGENTS

> 마지막 업데이트: 2026-05-05
> 상태: active
> doc_id: repo.agents
> role: entry
> owner_scope: fixture entry
> upstream: none
> artifacts: none
`,
    );
    await writeFile(
      repoRoot,
      "docs/index.md",
      `# Docs Index

> 마지막 업데이트: 2026-05-05
> 상태: active
> doc_id: docs.index
> role: entry
> owner_scope: fixture index
> upstream: repo.agents
> artifacts: none
`,
    );
    await writeFile(
      repoRoot,
      ".codex/skills/pdf/SKILL.md",
      `---
name: pdf
description: Test imported skill.
---

# Imported Skill

[Missing](missing.md)
`,
    );

    const result = await lintRepository(repoRoot, {
      includeGeneralChecks: true,
      includePolicyChecks: false,
    });
    assert.ok(
      result.errors.some((error) =>
        error.code === "broken-relative-link" &&
        error.path === ".codex/skills/pdf/SKILL.md"
      ),
    );
  } finally {
    await fs.rm(repoRoot, { recursive: true, force: true });
  }
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

test("reports metadata upstream owner doc_id references that do not resolve", async () => {
  const repoRoot = await fs.mkdtemp(path.join(os.tmpdir(), "docs-lint-upstream-doc-id-"));
  try {
    await writeFile(
      repoRoot,
      "AGENTS.md",
      `# AGENTS

> 마지막 업데이트: 2026-04-30
> 상태: active
> doc_id: repo.agents
> role: entry
> owner_scope: fixture entry
> upstream: none
> artifacts: none
`,
    );
    await writeFile(
      repoRoot,
      "docs/index.md",
      `# Docs Index

> 마지막 업데이트: 2026-04-30
> 상태: active
> doc_id: docs.index
> role: entry
> owner_scope: fixture index
> upstream: repo.agents
> artifacts: none

- \`active\`: [Demo](./owners/operations/demo.md) - demo owner
`,
    );
    await writeFile(
      repoRoot,
      "docs/owners/operations/demo.md",
      `# Demo

> 마지막 업데이트: 2026-04-30
> 상태: active
> doc_id: ops.demo
> role: ssot
> owner_scope: fixture owner
> upstream: docs.index, ops.missing-owner
> artifacts: none
`,
    );

    const result = await lintRepository(repoRoot, {
      includeGeneralChecks: true,
      includePolicyChecks: false,
    });
    assert.ok(
      result.errors.some((error) => error.code === "missing-upstream-doc-id-reference"),
    );
  } finally {
    await fs.rm(repoRoot, { recursive: true, force: true });
  }
});

test("reports path-style metadata upstream owner references", async () => {
  const repoRoot = await fs.mkdtemp(path.join(os.tmpdir(), "docs-lint-upstream-path-"));
  try {
    await writeFile(
      repoRoot,
      "AGENTS.md",
      `# AGENTS

> 마지막 업데이트: 2026-04-30
> 상태: active
> doc_id: repo.agents
> role: entry
> owner_scope: fixture entry
> upstream: none
> artifacts: none
`,
    );
    await writeFile(
      repoRoot,
      "docs/index.md",
      `# Docs Index

> 마지막 업데이트: 2026-04-30
> 상태: active
> doc_id: docs.index
> role: entry
> owner_scope: fixture index
> upstream: \`AGENTS.md\`
> artifacts: none
`,
    );

    const result = await lintRepository(repoRoot, {
      includeGeneralChecks: true,
      includePolicyChecks: false,
    });
    assert.ok(
      result.errors.some((error) => error.code === "missing-upstream-doc-id-reference"),
    );
  } finally {
    await fs.rm(repoRoot, { recursive: true, force: true });
  }
});

test("reports repo-imported rule skill references missing from the skill routing registry", async () => {
  const repoRoot = await fs.mkdtemp(path.join(os.tmpdir(), "docs-lint-global-skill-registry-"));
  try {
    await writeFile(
      repoRoot,
      "AGENTS.md",
      `# AGENTS

> 마지막 업데이트: 2026-05-01
> 상태: active
> doc_id: repo.agents
> role: entry
> owner_scope: fixture entry
> upstream: none
> artifacts: none
`,
    );
    await writeFile(
      repoRoot,
      "docs/index.md",
      `# Docs Index

> 마지막 업데이트: 2026-05-01
> 상태: active
> doc_id: docs.index
> role: entry
> owner_scope: fixture index
> upstream: repo.agents
> artifacts: none

### \`ops/\`

- \`active\`: [skill_routing_registry.md](./owners/operations/skill_routing_registry.md) - skill registry
- \`active\`: [demo.md](./owners/operations/demo.md) - demo owner
`,
    );
    await writeFile(
      repoRoot,
      "docs/owners/operations/skill_routing_registry.md",
      `# Skill Routing Registry

> 마지막 업데이트: 2026-05-01
> 상태: active
> doc_id: ops.skill-routing-registry
> role: reference
> owner_scope: fixture skill route registry
> upstream: docs.index
> artifacts: none

| Skill | Scope |
|---|---|
| \`rule-operations\` | operations |
`,
    );
    await writeFile(
      repoRoot,
      "docs/owners/operations/demo.md",
      `# Demo

> 마지막 업데이트: 2026-05-01
> 상태: active
> doc_id: ops.demo
> role: ssot
> owner_scope: fixture owner
> upstream: docs.index
> artifacts: none

Use \`rule-validation\` for a fixture route.
`,
    );

    const result = await lintRepository(repoRoot, {
      includeGeneralChecks: true,
      includePolicyChecks: false,
    });
    assert.ok(
      result.errors.some((error) => error.code === "missing-global-skill-registry-entry"),
    );
  } finally {
    await fs.rm(repoRoot, { recursive: true, force: true });
  }
});

test("accepts repo-imported rule skill references listed in the skill routing registry", async () => {
  const repoRoot = await fs.mkdtemp(path.join(os.tmpdir(), "docs-lint-global-skill-registry-valid-"));
  try {
    await writeFile(
      repoRoot,
      "AGENTS.md",
      `# AGENTS

> 마지막 업데이트: 2026-05-01
> 상태: active
> doc_id: repo.agents
> role: entry
> owner_scope: fixture entry
> upstream: none
> artifacts: none
`,
    );
    await writeFile(
      repoRoot,
      "docs/index.md",
      `# Docs Index

> 마지막 업데이트: 2026-05-01
> 상태: active
> doc_id: docs.index
> role: entry
> owner_scope: fixture index
> upstream: repo.agents
> artifacts: none

### \`ops/\`

- \`active\`: [skill_routing_registry.md](./owners/operations/skill_routing_registry.md) - skill registry
- \`active\`: [demo.md](./owners/operations/demo.md) - demo owner
`,
    );
    await writeFile(
      repoRoot,
      "docs/owners/operations/skill_routing_registry.md",
      `# Skill Routing Registry

> 마지막 업데이트: 2026-05-01
> 상태: active
> doc_id: ops.skill-routing-registry
> role: reference
> owner_scope: fixture skill route registry
> upstream: docs.index
> artifacts: none

| Skill | Scope |
|---|---|
| \`rule-operations\` | operations |
| \`rule-unity\` | unity |
`,
    );
    await writeFile(
      repoRoot,
      "docs/owners/operations/demo.md",
      `# Demo

> 마지막 업데이트: 2026-05-01
> 상태: active
> doc_id: ops.demo
> role: ssot
> owner_scope: fixture owner
> upstream: docs.index
> artifacts: none

Use \`rule-unity\` for a fixture route.
`,
    );

    const result = await lintRepository(repoRoot, {
      includeGeneralChecks: true,
      includePolicyChecks: false,
    });
    assert.equal(result.errors.length, 0);
  } finally {
    await fs.rm(repoRoot, { recursive: true, force: true });
  }
});

test("reports skill trigger matrix routes that do not match managed skills", async () => {
  const repoRoot = await fs.mkdtemp(path.join(os.tmpdir(), "docs-lint-skill-trigger-unknown-"));
  try {
    await writeFile(
      repoRoot,
      "AGENTS.md",
      `# AGENTS

> 마지막 업데이트: 2026-05-01
> 상태: active
> doc_id: repo.agents
> role: entry
> owner_scope: fixture entry
> upstream: none
> artifacts: none
`,
    );
    await writeFile(
      repoRoot,
      "docs/index.md",
      `# Docs Index

> 마지막 업데이트: 2026-05-01
> 상태: active
> doc_id: docs.index
> role: entry
> owner_scope: fixture index
> upstream: repo.agents
> artifacts: none

### \`ops/\`

- \`active\`: [skill_routing_registry.md](./owners/operations/skill_routing_registry.md) - skill registry
- \`active\`: [skill_trigger_matrix.md](./owners/operations/skill_trigger_matrix.md) - skill trigger matrix
`,
    );
    await writeFile(
      repoRoot,
      "docs/owners/operations/skill_routing_registry.md",
      `# Skill Routing Registry

> 마지막 업데이트: 2026-05-01
> 상태: active
> doc_id: ops.skill-routing-registry
> role: reference
> owner_scope: fixture skill route registry
> upstream: docs.index
> artifacts: none

| Skill | Scope |
|---|---|
| \`rule-operations\` | operations |
`,
    );
    await writeFile(
      repoRoot,
      "docs/owners/operations/skill_trigger_matrix.md",
      `# Skill Trigger Matrix

> 마지막 업데이트: 2026-05-01
> 상태: active
> doc_id: ops.skill-trigger-matrix
> role: reference
> owner_scope: fixture skill trigger matrix
> upstream: docs.index, ops.skill-routing-registry
> artifacts: none

| ID | Prompt signal | Expected skills |
|---|---|---|
| T01 | fixture | \`jg-missing\`, \`rule-operations\` |
`,
    );

    const result = await lintRepository(repoRoot, {
      includeGeneralChecks: true,
      includePolicyChecks: false,
    });
    assert.ok(
      result.errors.some((error) => error.code === "unknown-skill-trigger-route"),
    );
  } finally {
    await fs.rm(repoRoot, { recursive: true, force: true });
  }
});

test("reports repo-local skills missing from the skill trigger matrix", async () => {
  const repoRoot = await fs.mkdtemp(path.join(os.tmpdir(), "docs-lint-skill-trigger-missing-"));
  try {
    await writeFile(
      repoRoot,
      "AGENTS.md",
      `# AGENTS

> 마지막 업데이트: 2026-05-01
> 상태: active
> doc_id: repo.agents
> role: entry
> owner_scope: fixture entry
> upstream: none
> artifacts: none
`,
    );
    await writeFile(
      repoRoot,
      "docs/index.md",
      `# Docs Index

> 마지막 업데이트: 2026-05-01
> 상태: active
> doc_id: docs.index
> role: entry
> owner_scope: fixture index
> upstream: repo.agents
> artifacts: none

### \`ops/\`

- \`active\`: [skill_routing_registry.md](./owners/operations/skill_routing_registry.md) - skill registry
- \`active\`: [skill_trigger_matrix.md](./owners/operations/skill_trigger_matrix.md) - skill trigger matrix
`,
    );
    await writeFile(
      repoRoot,
      ".codex/skills/jg-demo/SKILL.md",
      `---
name: jg-demo
description: Fixture demo skill.
---

# Demo Skill

> 마지막 업데이트: 2026-05-01
> 상태: active
> doc_id: skill.jg-demo
> role: skill-entry
> owner_scope: fixture skill
> upstream: docs.index
> artifacts: none
`,
    );
    await writeFile(
      repoRoot,
      "docs/owners/operations/skill_routing_registry.md",
      `# Skill Routing Registry

> 마지막 업데이트: 2026-05-01
> 상태: active
> doc_id: ops.skill-routing-registry
> role: reference
> owner_scope: fixture skill route registry
> upstream: docs.index
> artifacts: none

| Skill | Scope |
|---|---|
| \`rule-operations\` | operations |
`,
    );
    await writeFile(
      repoRoot,
      "docs/owners/operations/skill_trigger_matrix.md",
      `# Skill Trigger Matrix

> 마지막 업데이트: 2026-05-01
> 상태: active
> doc_id: ops.skill-trigger-matrix
> role: reference
> owner_scope: fixture skill trigger matrix
> upstream: docs.index, ops.skill-routing-registry
> artifacts: none

| ID | Prompt signal | Expected skills |
|---|---|---|
| T01 | fixture | \`rule-operations\` |
`,
    );

    const result = await lintRepository(repoRoot, {
      includeGeneralChecks: true,
      includePolicyChecks: false,
    });
    assert.ok(
      result.errors.some((error) => error.code === "missing-skill-trigger-fixture"),
    );
  } finally {
    await fs.rm(repoRoot, { recursive: true, force: true });
  }
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

test("reports missing root cause investigation contract in acceptance guardrails", async () => {
  const result = await lintRepository(getFixturePath("missing-root-cause-investigation-contract"), {
    includeGeneralChecks: false,
    includePolicyChecks: true,
  });
  assert.ok(
    result.errors.some((error) => error.code === "missing-root-cause-investigation-contract"),
  );
});

test("reports missing owner route in issue investigation skill", async () => {
  const result = await lintRepository(getFixturePath("missing-issue-investigation-owner-route"), {
    includeGeneralChecks: false,
    includePolicyChecks: true,
  });
  assert.ok(
    result.errors.some((error) => error.code === "missing-issue-investigation-owner-route"),
  );
});

test("reports missing Fresh Evidence Discipline contract in acceptance guardrails", async () => {
  const result = await lintRepository(getFixturePath("missing-fresh-evidence-discipline-contract"), {
    includeGeneralChecks: false,
    includePolicyChecks: true,
  });
  assert.ok(
    result.errors.some((error) => error.code === "missing-fresh-evidence-discipline-contract"),
  );
});

test("reports missing Fresh Evidence route in repo-local evidence skills", async () => {
  const result = await lintRepository(getFixturePath("missing-fresh-evidence-skill-route"), {
    includeGeneralChecks: false,
    includePolicyChecks: true,
  });
  assert.ok(
    result.errors.some((error) => error.code === "missing-fresh-evidence-skill-route"),
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

test("does not require recurrence closeout for ordinary plan document changes", async () => {
  const result = await lintRepository(getFixturePath("valid-recurrence-closeout"), {
    includeGeneralChecks: false,
    includePolicyChecks: true,
    changedFiles: ["docs/plans/current/progress.md"],
  });
  assert.equal(result.errors.length, 0);
});

test("does not require recurrence closeout for feature tool implementation changes", async () => {
  const result = await lintRepository(getFixturePath("valid-recurrence-closeout"), {
    includeGeneralChecks: false,
    includePolicyChecks: true,
    changedFiles: [
      "tools/audio-mcp/server.mjs",
      "tools/audio-mcp/README.md",
      "package-lock.json",
    ],
  });
  assert.equal(result.errors.length, 0);
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

test("does not require deleted rules-only paths in recurrence closeout changedPaths", async () => {
  const previousChangedFilesEnv = process.env.RULES_LINT_CHANGED_FILES;
  delete process.env.RULES_LINT_CHANGED_FILES;

  const repoRoot = await fs.mkdtemp(path.join(os.tmpdir(), "docs-lint-deleted-closeout-path-"));
  try {
    await execFileAsync("git", ["init"], { cwd: repoRoot });
    await execFileAsync("git", ["config", "user.email", "fixture@example.test"], { cwd: repoRoot });
    await execFileAsync("git", ["config", "user.name", "Fixture"], { cwd: repoRoot });
    await writeFile(
      repoRoot,
      "AGENTS.md",
      `# AGENTS.md

> 마지막 업데이트: 2026-05-05
> 상태: active
> doc_id: repo.agents
> role: entry
> owner_scope: fixture entry
> upstream: none
> artifacts: none

Plan Mode / Codex 운영 규칙은 docs/index.md에서 current path 확인 후 rule-operations owner 문서로 라우팅하고, 그 lane에서는 mutation 금지.
`,
    );
    await writeFile(
      repoRoot,
      "tools/docs-lint/deleted-fixture.md",
      "# Deleted fixture\n",
    );
    await execFileAsync("git", ["add", "."], { cwd: repoRoot });
    await execFileAsync("git", ["commit", "-m", "baseline"], { cwd: repoRoot });
    await fs.rm(path.join(repoRoot, "tools/docs-lint/deleted-fixture.md"));
    const shardPath = "artifacts/rules/issue-recurrence-closeout.d/deleted-path.json";
    await writeFile(
      repoRoot,
      shardPath,
      `${JSON.stringify({
        schemaVersion: 1,
        updatedAt: "2026-05-05",
        scope: "rules-only",
        issueDetected: true,
        declaredLane: "rules-only deleted path fixture",
        observedMutationClass: `docs-lint fixture coverage for ${recurrenceCoverageText}`,
        acceptanceEvidenceClass: "docs-lint unit test",
        escalationRequired: false,
        rootCause: `Deleted rules-only paths are git evidence, not active repo paths for ${recurrenceCoverageText}.`,
        prevention: `Changed closeout coverage so ${recurrenceCoverageText} shards can omit deleted paths.`,
        verification: "node --test tools/docs-lint/tests/docs-lint.test.mjs",
        blockedReason: "",
        changedPaths: [shardPath],
      }, null, 2)}\n`,
    );

    const result = await lintRepository(repoRoot, {
      includeGeneralChecks: false,
      includePolicyChecks: true,
    });
    assert.equal(result.errors.length, 0, JSON.stringify(result.errors, null, 2));
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

test("reports uncertain rootCause without blockedReason", async () => {
  const result = await lintRepository(getFixturePath("uncertain-root-cause-closeout"), {
    includeGeneralChecks: false,
    includePolicyChecks: true,
    changedFiles: [
      "docs/index.md",
      "artifacts/rules/issue-recurrence-closeout.json",
    ],
  });
  assert.ok(
    result.errors.some((error) => error.code === "uncertain-root-cause-without-blocked-reason"),
  );
});

test("reports recurrence closeout missing hard-fail coverage wording", async () => {
  const repoRoot = await fs.mkdtemp(path.join(os.tmpdir(), "docs-lint-closeout-coverage-"));
  try {
    await writeMinimalRecurrenceRepo(repoRoot, {
      rootCause: "A rules-only change introduced a documentation issue.",
      prevention: "Added owner-doc and lint coverage.",
    });
    const result = await lintRepository(repoRoot, {
      includeGeneralChecks: false,
      includePolicyChecks: true,
      changedFiles: [
        "docs/index.md",
        "artifacts/rules/issue-recurrence-closeout.json",
      ],
    });
    assert.ok(
      result.errors.some((error) => error.code === "missing-recurrence-closeout-coverage"),
    );
  } finally {
    await fs.rm(repoRoot, { recursive: true, force: true });
  }
});

test("reports recurrence closeout verification without concrete evidence anchor", async () => {
  const repoRoot = await fs.mkdtemp(path.join(os.tmpdir(), "docs-lint-closeout-verification-"));
  try {
    await writeMinimalRecurrenceRepo(repoRoot, {
      rootCause: "A rules-only change introduced a documentation issue.",
      verification: "Checked the change.",
    });
    const result = await lintRepository(repoRoot, {
      includeGeneralChecks: false,
      includePolicyChecks: true,
      changedFiles: [
        "docs/index.md",
        "artifacts/rules/issue-recurrence-closeout.json",
      ],
    });
    assert.ok(
      result.errors.some((error) => error.code === "missing-recurrence-closeout-verification-evidence"),
    );
  } finally {
    await fs.rm(repoRoot, { recursive: true, force: true });
  }
});

test("reports expanded uncertain rootCause expressions without blockedReason", async () => {
  const expressions = [
    "maybe the changed-path sync missed a case",
    "the route appears to skip the owner doc",
    "원인이 아직 가능성 수준이다",
    "로그상 누락으로 보임",
    "로그상 누락으로 보인다",
    "로그상 누락으로 보여",
    "아직 검증 전인 듯하다",
    "검증이 덜 된 것 같다",
  ];

  for (const rootCause of expressions) {
    const repoRoot = await fs.mkdtemp(path.join(os.tmpdir(), "docs-lint-uncertain-"));
    try {
      await writeMinimalRecurrenceRepo(repoRoot, { rootCause });
      const result = await lintRepository(repoRoot, {
        includeGeneralChecks: false,
        includePolicyChecks: true,
        changedFiles: [
          "docs/index.md",
          "artifacts/rules/issue-recurrence-closeout.json",
        ],
      });
      assert.ok(
        result.errors.some((error) => error.code === "uncertain-root-cause-without-blocked-reason"),
        `Expected uncertain rootCause error for: ${rootCause}`,
      );
    } finally {
      await fs.rm(repoRoot, { recursive: true, force: true });
    }
  }
});

test("accepts uncertain rootCause when blockedReason explains missing verification", async () => {
  const result = await lintRepository(getFixturePath("uncertain-root-cause-blocked-valid"), {
    includeGeneralChecks: false,
    includePolicyChecks: true,
    changedFiles: [
      "docs/index.md",
      "artifacts/rules/issue-recurrence-closeout.json",
    ],
  });
  assert.equal(result.errors.length, 0);
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

test("accepts sharded recurrence closeout artifact for rules-only changes", async () => {
  const repoRoot = await fs.mkdtemp(path.join(os.tmpdir(), "docs-lint-sharded-closeout-"));
  try {
    await writeMinimalRecurrenceRepo(repoRoot, {
      rootCause: `A rules-only change introduced coverage for ${recurrenceCoverageText}.`,
      prevention: `Added owner-doc and lint coverage for ${recurrenceCoverageText}.`,
      verification: "node --test tools/docs-lint/tests/docs-lint.test.mjs",
    });
    const shardPath = "artifacts/rules/issue-recurrence-closeout.d/local-fixture.json";
    await writeFile(
      repoRoot,
      shardPath,
      `${JSON.stringify({
        schemaVersion: 1,
        updatedAt: "2026-05-01",
        scope: "rules-only",
        issueDetected: true,
        declaredLane: "rules-only recurrence prevention",
        observedMutationClass: `sharded closeout coverage for ${recurrenceCoverageText}`,
        acceptanceEvidenceClass: "docs-lint fixture tests",
        escalationRequired: false,
        rootCause: `A rules-only change introduced coverage for ${recurrenceCoverageText}.`,
        prevention: `Added sharded closeout coverage for ${recurrenceCoverageText}.`,
        verification: "node --test tools/docs-lint/tests/docs-lint.test.mjs",
        blockedReason: "",
        changedPaths: [
          shardPath,
          "docs/index.md",
        ],
      }, null, 2)}\n`,
    );

    const result = await lintRepository(repoRoot, {
      includeGeneralChecks: false,
      includePolicyChecks: true,
      changedFiles: [
        "docs/index.md",
        shardPath,
      ],
    });
    assert.equal(result.errors.length, 0);
  } finally {
    await fs.rm(repoRoot, { recursive: true, force: true });
  }
});
