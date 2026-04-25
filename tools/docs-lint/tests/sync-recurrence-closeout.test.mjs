import test from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs/promises";
import os from "node:os";
import path from "node:path";
import { fileURLToPath } from "node:url";

import { RECURRENCE_CLOSEOUT_PATH } from "../lib.mjs";
import { syncRecurrenceCloseoutArtifact } from "../sync-recurrence-closeout.mjs";

const fixturesRoot = fileURLToPath(new URL("./fixtures", import.meta.url));

async function createFixtureWorkspace(name) {
  const tempRoot = await fs.mkdtemp(path.join(os.tmpdir(), "jg-docs-lint-"));
  const sourceRoot = path.join(fixturesRoot, name);
  await fs.cp(sourceRoot, tempRoot, { recursive: true });
  return tempRoot;
}

test("syncRecurrenceCloseoutArtifact refreshes changedPaths for rules-only changes", async () => {
  const repoRoot = await createFixtureWorkspace("valid-recurrence-closeout");

  const result = await syncRecurrenceCloseoutArtifact({
    repoRoot,
    changedFiles: [
      "docs/index.md",
      "tools/docs-lint/lib.mjs",
      RECURRENCE_CLOSEOUT_PATH,
    ],
  });

  assert.equal(result.changed, true);
  assert.deepEqual(result.changedPaths, [
    RECURRENCE_CLOSEOUT_PATH,
    "docs/index.md",
    "tools/docs-lint/lib.mjs",
  ]);

  const payload = JSON.parse(
    await fs.readFile(path.join(repoRoot, RECURRENCE_CLOSEOUT_PATH), "utf8"),
  );
  assert.deepEqual(payload.changedPaths, result.changedPaths);
});

test("syncRecurrenceCloseoutArtifact is a no-op when changedPaths already match", async () => {
  const repoRoot = await createFixtureWorkspace("valid-recurrence-closeout");
  const changedFiles = [
    "docs/index.md",
    RECURRENCE_CLOSEOUT_PATH,
  ];

  await syncRecurrenceCloseoutArtifact({
    repoRoot,
    changedFiles,
  });

  const result = await syncRecurrenceCloseoutArtifact({
    repoRoot,
    changedFiles,
  });

  assert.equal(result.changed, false);
  assert.deepEqual(result.changedPaths, [
    RECURRENCE_CLOSEOUT_PATH,
    "docs/index.md",
  ]);
});
