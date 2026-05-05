import path from "node:path";

const INDEX_STATUS_LINE_PATTERN =
  /^-\s+`(active|draft|reference|historical|paused)`:\s+\[[^\]]+\]\(([^)]+)\)/;

export function validateIndexCoverage(documents, repoRoot) {
  const indexDocument = documents.find(
    (document) => document.repoRelativePath === "docs/index.md",
  );
  if (!indexDocument) {
    return [];
  }

  const indexedDocPaths = new Set();
  const lines = indexDocument.content.split(/\r?\n/);
  for (const line of lines) {
    const match = line.match(INDEX_STATUS_LINE_PATTERN);
    if (!match) {
      continue;
    }

    const target = normalizeMarkdownTarget(match[2]);
    if (!target || !isRelativeTarget(target)) {
      continue;
    }

    const resolvedPath = path.resolve(path.dirname(indexDocument.absolutePath), target);
    indexedDocPaths.add(toRepoRelative(repoRoot, resolvedPath));
  }

  const errors = [];
  for (const document of documents) {
    if (
      !document.repoRelativePath.startsWith("docs/") ||
      document.repoRelativePath === "docs/index.md"
    ) {
      continue;
    }

    if (indexedDocPaths.has(document.repoRelativePath)) {
      continue;
    }

    errors.push(
      createError(
        "index-missing-entry",
        "docs/index.md",
        `docs/index.md must register \`${document.repoRelativePath}\` with a status label entry.`,
      ),
    );
  }

  return errors;
}

export function validateIndexStatusLabels(documents, repoRoot) {
  const indexDocument = documents.find(
    (document) => document.repoRelativePath === "docs/index.md",
  );
  if (!indexDocument) {
    return [];
  }

  const metadataByAbsolutePath = new Map(
    documents.map((document) => [document.absolutePath, document.metadata]),
  );
  const errors = [];
  const lines = indexDocument.content.split(/\r?\n/);

  for (let index = 0; index < lines.length; index += 1) {
    const line = lines[index];
    const match = line.match(INDEX_STATUS_LINE_PATTERN);
    if (!match) {
      continue;
    }

    const expectedStatus = match[1];
    const target = normalizeMarkdownTarget(match[2]);
    if (!target || !isRelativeTarget(target)) {
      continue;
    }

    const resolvedPath = path.resolve(path.dirname(indexDocument.absolutePath), target);
    const targetMetadata = metadataByAbsolutePath.get(resolvedPath);
    if (!targetMetadata) {
      continue;
    }

    const actualStatus = targetMetadata.get("상태");
    if (actualStatus !== expectedStatus) {
      errors.push(
        createError(
          "index-status-mismatch",
          indexDocument.repoRelativePath,
          `Index status label \`${expectedStatus}\` for \`${toRepoRelative(repoRoot, resolvedPath)}\` does not match document 상태 \`${actualStatus || "missing"}\`.`,
          index + 1,
        ),
      );
    }
  }

  return errors;
}

export function validateIndexRegistryConsistency(documents, repoRoot) {
  const indexDocument = documents.find(
    (document) => document.repoRelativePath === "docs/index.md",
  );
  if (!indexDocument) {
    return [];
  }

  const metadataByRepoPath = new Map(
    documents.map((document) => [document.repoRelativePath, document.metadata]),
  );
  const registryEntries = parseIndexDocIdRegistry(indexDocument, repoRoot);
  const statusEntries = parseIndexStatusEntries(indexDocument, repoRoot);
  const registryByPath = new Map(registryEntries.map((entry) => [entry.repoRelativePath, entry]));
  const statusByPath = new Map(statusEntries.map((entry) => [entry.repoRelativePath, entry]));
  const errors = [];

  for (const entry of registryEntries) {
    const targetMetadata = metadataByRepoPath.get(entry.repoRelativePath);
    if (targetMetadata) {
      const actualDocId = targetMetadata.get("doc_id");
      if (actualDocId && actualDocId !== entry.docId) {
        errors.push(
          createError(
            "index-doc-id-registry-mismatch",
            indexDocument.repoRelativePath,
            `Doc ID Registry entry for \`${entry.repoRelativePath}\` uses \`${entry.docId}\`, but document metadata has \`${actualDocId}\`.`,
            entry.line,
          ),
        );
      }

      const actualStatus = targetMetadata.get("상태");
      if (actualStatus && actualStatus !== entry.status) {
        errors.push(
          createError(
            "index-doc-id-registry-status-mismatch",
            indexDocument.repoRelativePath,
            `Doc ID Registry entry for \`${entry.repoRelativePath}\` uses status \`${entry.status}\`, but document metadata has \`${actualStatus}\`.`,
            entry.line,
          ),
        );
      }
    }

    if (entry.repoRelativePath === "docs/index.md") {
      continue;
    }

    const statusEntry = statusByPath.get(entry.repoRelativePath);
    if (!statusEntry) {
      errors.push(
        createError(
          "index-status-entry-missing-for-doc-id",
          indexDocument.repoRelativePath,
          `Status Label Entries must include Doc ID Registry path \`${entry.repoRelativePath}\`.`,
          entry.line,
        ),
      );
      continue;
    }

    if (statusEntry.status !== entry.status) {
      errors.push(
        createError(
          "index-registry-status-entry-mismatch",
          indexDocument.repoRelativePath,
          `Doc ID Registry status \`${entry.status}\` for \`${entry.repoRelativePath}\` does not match Status Label Entries status \`${statusEntry.status}\`.`,
          statusEntry.line,
        ),
      );
    }
  }

  for (const entry of statusEntries) {
    if (registryByPath.has(entry.repoRelativePath)) {
      continue;
    }

    errors.push(
      createError(
        "index-status-entry-missing-doc-id",
        indexDocument.repoRelativePath,
        `Status Label Entries path \`${entry.repoRelativePath}\` must have a matching Doc ID Registry row.`,
        entry.line,
      ),
    );
  }

  return errors;
}

function parseIndexDocIdRegistry(indexDocument, repoRoot) {
  const entries = [];
  const lines = indexDocument.content.split(/\r?\n/);
  let inSection = false;

  for (let index = 0; index < lines.length; index += 1) {
    const line = lines[index];
    if (line.startsWith("## ")) {
      inSection = line.trim() === "## Doc ID Registry";
      continue;
    }

    if (!inSection) {
      continue;
    }

    const match = line.match(/^\|\s*([^|]+?)\s*\|\s*([^|]+?)\s*\|\s*([^|]+?)\s*\|$/u);
    if (!match) {
      continue;
    }

    const docId = match[1].trim();
    const fileName = match[2].trim();
    const status = match[3].trim();
    if (docId === "doc_id" || docId === "---") {
      continue;
    }

    const resolvedPath = path.resolve(path.dirname(indexDocument.absolutePath), fileName);
    entries.push({
      docId,
      repoRelativePath: toRepoRelative(repoRoot, resolvedPath),
      status,
      line: index + 1,
    });
  }

  return entries;
}

function parseIndexStatusEntries(indexDocument, repoRoot) {
  const entries = [];
  const lines = indexDocument.content.split(/\r?\n/);
  let inSection = false;

  for (let index = 0; index < lines.length; index += 1) {
    const line = lines[index];
    if (line.startsWith("## ")) {
      inSection = line.trim() === "## Status Label Entries";
      continue;
    }

    if (!inSection) {
      continue;
    }

    const match = line.match(INDEX_STATUS_LINE_PATTERN);
    if (!match) {
      continue;
    }

    const target = normalizeMarkdownTarget(match[2]);
    if (!target || !isRelativeTarget(target)) {
      continue;
    }

    const resolvedPath = path.resolve(path.dirname(indexDocument.absolutePath), target);
    entries.push({
      status: match[1],
      repoRelativePath: toRepoRelative(repoRoot, resolvedPath),
      line: index + 1,
    });
  }

  return entries;
}

function normalizeMarkdownTarget(rawTarget) {
  if (!rawTarget) {
    return "";
  }

  return rawTarget.split("#")[0].trim();
}

function isRelativeTarget(target) {
  if (!target || target.startsWith("#") || target.startsWith("/")) {
    return false;
  }

  return !/^[a-zA-Z][a-zA-Z0-9+.-]*:/.test(target);
}

function createError(code, repoRelativePath, message, line = null) {
  return {
    code,
    path: repoRelativePath,
    message,
    line,
  };
}

function toRepoRelative(repoRoot, absolutePath) {
  return toPosixPath(path.relative(repoRoot, absolutePath));
}

function toPosixPath(targetPath) {
  return targetPath.split(path.sep).join("/");
}

