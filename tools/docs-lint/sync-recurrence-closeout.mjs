import fs from "node:fs/promises";
import path from "node:path";
import process from "node:process";
import crypto from "node:crypto";
import { execFile } from "node:child_process";
import { promisify } from "node:util";
import { fileURLToPath } from "node:url";

import {
  RECURRENCE_CLOSEOUT_DIR,
  RECURRENCE_CLOSEOUT_PATH,
  getRecurrenceChangedFiles,
  isRecurrenceCloseoutArtifactPath,
  isRulesOnlyRecurrenceTarget,
  normalizeRepoRelativePath,
} from "./lib.mjs";

const execFileAsync = promisify(execFile);

export async function getRulesOnlyChangedFiles({
  repoRoot,
  changedFiles = null,
  includeArtifact = true,
  artifactPath = RECURRENCE_CLOSEOUT_PATH,
} = {}) {
  const sourceFiles = Array.isArray(changedFiles)
    ? changedFiles
    : await getRecurrenceChangedFiles({ repoRoot });

  const filtered = sourceFiles
    .map((entry) => normalizeRepoRelativePath(entry))
    .filter(Boolean)
    .filter((entry) => isRulesOnlyRecurrenceTarget(entry))
    .filter((entry) => includeArtifact || !isRecurrenceCloseoutArtifactPath(entry));

  if (includeArtifact) {
    filtered.push(artifactPath);
  }

  return [...new Set(filtered)].sort((left, right) => left.localeCompare(right));
}

export async function syncRecurrenceCloseoutArtifact({
  repoRoot,
  changedFiles = null,
  artifactPath = RECURRENCE_CLOSEOUT_PATH,
} = {}) {
  const normalizedArtifactPath = normalizeRepoRelativePath(artifactPath) || RECURRENCE_CLOSEOUT_PATH;
  const artifactAbsolutePath = path.join(repoRoot, normalizedArtifactPath);
  const templateAbsolutePath = path.join(repoRoot, RECURRENCE_CLOSEOUT_PATH);
  const payloadPath = await pathExists(artifactAbsolutePath)
    ? artifactAbsolutePath
    : templateAbsolutePath;
  const payload = JSON.parse(await fs.readFile(payloadPath, "utf8"));
  const changedPaths = await getRulesOnlyChangedFiles({
    repoRoot,
    changedFiles,
    includeArtifact: true,
    artifactPath: normalizedArtifactPath,
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
    await fs.mkdir(path.dirname(artifactAbsolutePath), { recursive: true });
    await fs.writeFile(artifactAbsolutePath, `${nextJson}\n`, "utf8");
  }

  return {
    changed,
    artifactPath: normalizedArtifactPath,
    changedPaths,
  };
}

export async function getDefaultRecurrenceCloseoutShardPath({
  repoRoot,
  changedFiles = null,
} = {}) {
  const changedPaths = await getRulesOnlyChangedFiles({
    repoRoot,
    changedFiles,
    includeArtifact: false,
  });
  if (changedPaths.length === 0) {
    return RECURRENCE_CLOSEOUT_PATH;
  }

  const hash = crypto
    .createHash("sha1")
    .update(changedPaths.join("\n"))
    .digest("hex")
    .slice(0, 10);

  return `${RECURRENCE_CLOSEOUT_DIR}/local-${hash}.json`;
}

function formatDateForArtifact(value) {
  const year = value.getFullYear();
  const month = `${value.getMonth() + 1}`.padStart(2, "0");
  const day = `${value.getDate()}`.padStart(2, "0");
  return `${year}-${month}-${day}`;
}

async function pathExists(absolutePath) {
  try {
    await fs.access(absolutePath);
    return true;
  } catch {
    return false;
  }
}

async function stageArtifact(repoRoot, artifactPath) {
  await execFileAsync("git", ["add", artifactPath], { cwd: repoRoot });
}

async function main() {
  const repoRoot = path.resolve(fileURLToPath(new URL("../..", import.meta.url)));
  const args = process.argv.slice(2);
  const argSet = new Set(args);
  const shouldStage = argSet.has("--stage");
  const artifactArgIndex = args.findIndex((arg) => arg === "--artifact");
  const artifactPath = argSet.has("--primary")
    ? RECURRENCE_CLOSEOUT_PATH
    : artifactArgIndex >= 0 && args[artifactArgIndex + 1]
      ? normalizeRepoRelativePath(args[artifactArgIndex + 1])
      : await getDefaultRecurrenceCloseoutShardPath({ repoRoot });
  const result = await syncRecurrenceCloseoutArtifact({ repoRoot, artifactPath });

  if (result.changed && shouldStage) {
    await stageArtifact(repoRoot, result.artifactPath);
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
