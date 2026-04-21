import path from "node:path";
import process from "node:process";
import { fileURLToPath } from "node:url";

import { formatLintReport, lintRepository } from "./lib.mjs";

const repoRoot = path.resolve(fileURLToPath(new URL("../..", import.meta.url)));
const result = await lintRepository(repoRoot, {
  includeGeneralChecks: false,
  includePolicyChecks: true,
});

console.log(formatLintReport(result));
process.exitCode = result.errors.length > 0 ? 1 : 0;
