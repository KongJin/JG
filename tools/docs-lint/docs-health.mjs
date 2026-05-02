import path from "node:path";
import process from "node:process";
import { fileURLToPath } from "node:url";

import {
  buildDocsHealthReport,
  formatDocsHealthReport,
  lintRepository,
} from "./lib.mjs";

const repoRoot = path.resolve(fileURLToPath(new URL("../..", import.meta.url)));
const shouldPrintJson = process.argv.slice(2).includes("--json");
const result = await lintRepository(repoRoot, {
  includeGeneralChecks: true,
  includePolicyChecks: true,
});
const report = buildDocsHealthReport(result);

console.log(
  shouldPrintJson
    ? JSON.stringify(report, null, 2)
    : formatDocsHealthReport(report),
);

process.exitCode = 0;
