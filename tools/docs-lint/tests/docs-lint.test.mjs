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
  const result = await lintRepository(getFixturePath("missing-meta"));
  assert.ok(result.errors.some((error) => error.code === "missing-meta"));
});

test("reports duplicate doc_id values", async () => {
  const result = await lintRepository(getFixturePath("duplicate-doc-id"));
  assert.ok(result.errors.some((error) => error.code === "duplicate-doc-id"));
});

test("reports broken relative markdown links", async () => {
  const result = await lintRepository(getFixturePath("broken-relative-link"));
  assert.ok(
    result.errors.some((error) => error.code === "broken-relative-link"),
  );
});

test("reports docs/index status label mismatches", async () => {
  const result = await lintRepository(getFixturePath("index-status-mismatch"));
  assert.ok(
    result.errors.some((error) => error.code === "index-status-mismatch"),
  );
});

test("ignores excluded .system skill files", async () => {
  const result = await lintRepository(getFixturePath("excluded-system-skill-ignored"));
  assert.equal(result.errors.length, 0);
});

test("reports stale inline repo paths in skill markdown", async () => {
  const result = await lintRepository(getFixturePath("stale-inline-path-in-skill"));
  assert.ok(result.errors.some((error) => error.code === "missing-inline-path"));
});

test("reports legacy agent paths in repo-local skills", async () => {
  const result = await lintRepository(getFixturePath("legacy-agent-token-in-skill"));
  assert.ok(result.errors.some((error) => error.code === "legacy-owner-path"));
});

test("reports deleted skill references still mentioned from SKILL.md", async () => {
  const result = await lintRepository(getFixturePath("deleted-reference-mentioned"));
  assert.ok(result.errors.some((error) => error.code === "missing-inline-path"));
});

test("ignores valid search-pattern inline code", async () => {
  const result = await lintRepository(getFixturePath("valid-search-pattern-ignored"));
  assert.equal(result.errors.length, 0);
});
