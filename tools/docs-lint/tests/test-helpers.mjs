import fs from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";

export const fixturesRoot = fileURLToPath(new URL("./fixtures", import.meta.url));

export function getFixturePath(name) {
  return path.join(fixturesRoot, name);
}

export async function writeFile(root, repoRelativePath, content) {
  const absolutePath = path.join(root, repoRelativePath);
  await fs.mkdir(path.dirname(absolutePath), { recursive: true });
  await fs.writeFile(absolutePath, content, "utf8");
}

