import test from "node:test";
import assert from "node:assert/strict";

import { isRulesOnlyRecurrenceTarget } from "../lib.mjs";

test("treats repo skill control docs as rules-only recurrence targets", () => {
  assert.equal(isRulesOnlyRecurrenceTarget(".codex/skills/IMPORT_MANIFEST.md"), true);
  assert.equal(isRulesOnlyRecurrenceTarget(".codex/skills/README.md"), true);
  assert.equal(isRulesOnlyRecurrenceTarget(".codex/skills/.system/imagegen/SKILL.md"), false);
});

