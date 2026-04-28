import fs from "node:fs/promises";
import path from "node:path";
import process from "node:process";
import { execFile } from "node:child_process";
import { promisify } from "node:util";
import { fileURLToPath } from "node:url";

import {
  RECURRENCE_CLOSEOUT_PATH,
  getRecurrenceChangedFiles,
  isRulesOnlyRecurrenceTarget,
  normalizeRepoRelativePath,
} from "./lib.mjs";

const execFileAsync = promisify(execFile);

export async function getRulesOnlyChangedFiles({
  repoRoot,
  changedFiles = null,
  includeArtifact = true,
} = {}) {
  const sourceFiles = Array.isArray(changedFiles)
    ? changedFiles
    : await getRecurrenceChangedFiles({ repoRoot });

  const filtered = sourceFiles
    .map((entry) => normalizeRepoRelativePath(entry))
    .filter(Boolean)
    .filter((entry) => isRulesOnlyRecurrenceTarget(entry))
    .filter((entry) => includeArtifact || entry !== RECURRENCE_CLOSEOUT_PATH);

  if (includeArtifact) {
    filtered.push(RECURRENCE_CLOSEOUT_PATH);
  }

  return [...new Set(filtered)].sort((left, right) => left.localeCompare(right));
}

export async function syncRecurrenceCloseoutArtifact({
  repoRoot,
  changedFiles = null,
} = {}) {
  const artifactAbsolutePath = path.join(repoRoot, RECURRENCE_CLOSEOUT_PATH);
  const payload = JSON.parse(await fs.readFile(artifactAbsolutePath, "utf8"));
  const changedPaths = await getRulesOnlyChangedFiles({
    repoRoot,
    changedFiles,
    includeArtifact: true,
  });

  const nextPayload = {
    ...payload,
    updatedAt: formatDateForArtifact(new Date()),
    changedPaths,
  };

  const previousJson = JSON.stringify(payload, null, 2);
  const nextJson = JSON.stringify(nextPayload, null, 2);
  const changed = previousJson !== nextJson;

  if (changed) {
    await fs.writeFile(artifactAbsolutePath, `${nextJson}\n`, "utf8");
  }

  return {
    changed,
    artifactPath: RECURRENCE_CLOSEOUT_PATH,
    changedPaths,
  };
}

function formatDateForArtifact(value) {
  const year = value.getFullYear();
  const month = `${value.getMonth() + 1}`.padStart(2, "0");
  const day = `${value.getDate()}`.padStart(2, "0");
  return `${year}-${month}-${day}`;
}

async function stageArtifact(repoRoot) {
  await execFileAsync("git", ["add", RECURRENCE_CLOSEOUT_PATH], { cwd: repoRoot });
}

async function main() {
  const repoRoot = path.resolve(fileURLToPath(new URL("../..", import.meta.url)));
  const args = new Set(process.argv.slice(2));
  const shouldStage = args.has("--stage");
  const result = await syncRecurrenceCloseoutArtifact({ repoRoot });

  if (result.changed && shouldStage) {
    await stageArtifact(repoRoot);
  }

  const summary = result.changed
    ? `Synced ${result.artifactPath} with ${result.changedPaths.length} rules-only changed path(s).`
    : `${result.artifactPath} was already in sync.`;
  console.log(summary);
}

const directRunPath = process.argv[1]
  ? path.resolve(process.argv[1])
  : "";
const modulePath = fileURLToPath(import.meta.url);

if (directRunPath && modulePath === directRunPath) {
  await main();
}
