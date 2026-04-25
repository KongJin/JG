import test from "node:test";
import assert from "node:assert/strict";
import path from "node:path";
import { fileURLToPath } from "node:url";

import { lintRepository } from "../lib.mjs";

const fixturesRoot = fileURLToPath(new URL("./fixtures", import.meta.url));

function getFixturePath(name) {
  return path.join(fixturesRoot, name);
}

test("reports missing metadata", async () => {
  const result = await lintRepository(getFixturePath("missing-meta"), {
    includeGeneralChecks: true,
    includePolicyChecks: false,
  });
  assert.ok(result.errors.some((error) => error.code === "missing-meta"));
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
