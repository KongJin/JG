import fs from "node:fs";
import path from "node:path";
import os from "node:os";
import assert from "node:assert/strict";

const repoRoot = path.resolve(process.argv[2] && !process.argv[2].startsWith("--") ? process.argv[2] : process.cwd());
const args = new Set(process.argv.slice(process.argv[2] && !process.argv[2].startsWith("--") ? 3 : 2));
const shouldWriteBaseline = args.has("--write-baseline");
const noBaseline = args.has("--no-baseline");
const selfTest = args.has("--self-test");

const baselinePath = path.join(repoRoot, "tools", "workflow", "csharp-source-guardrails-baseline.json");

const nullDefenseRegexes = [
  /\b\w[\w.]*\s*(?:==|!=)\s*null\b/u,
  /\bnull\s*(?:==|!=)\s*\w[\w.]*\b/u,
  /\bis\s+not\s+null\b/u,
  /\bis\s+null\b/u,
  /\?\?/u,
  /\?\./u,
  /\?\[/u,
];

const callableBlockers = new Set([
  "if",
  "for",
  "foreach",
  "while",
  "switch",
  "catch",
  "using",
  "lock",
  "return",
  "new",
  "sizeof",
  "typeof",
  "nameof",
]);

function repoRelative(filePath, root = repoRoot) {
  return path.relative(root, filePath).replaceAll(path.sep, "/");
}

function listCsFilesRecursive(directory) {
  const results = [];
  if (!fs.existsSync(directory)) return results;

  for (const entry of fs.readdirSync(directory, { withFileTypes: true })) {
    const entryPath = path.join(directory, entry.name);
    if (entry.isDirectory()) {
      results.push(...listCsFilesRecursive(entryPath));
      continue;
    }

    if (entry.isFile() && entry.name.endsWith(".cs")) {
      results.push(entryPath);
    }
  }

  return results;
}

function normalizeNewlines(text) {
  return text.replace(/\r\n/g, "\n").replace(/\r/g, "\n");
}

function stripCommentsAndStrings(text) {
  const source = normalizeNewlines(text);
  let output = "";
  let state = "code";

  for (let index = 0; index < source.length; index += 1) {
    const current = source[index];
    const next = source[index + 1] ?? "";

    if (state === "code") {
      if (current === "/" && next === "/") {
        output += "  ";
        state = "line-comment";
        index += 1;
        continue;
      }

      if (current === "/" && next === "*") {
        output += "  ";
        state = "block-comment";
        index += 1;
        continue;
      }

      if (current === "@" && next === "\"") {
        output += "  ";
        state = "verbatim-string";
        index += 1;
        continue;
      }

      if (current === "\"") {
        output += " ";
        state = "string";
        continue;
      }

      if (current === "'") {
        output += " ";
        state = "char";
        continue;
      }

      output += current;
      continue;
    }

    if (state === "line-comment") {
      if (current === "\n") {
        output += current;
        state = "code";
      } else {
        output += " ";
      }
      continue;
    }

    if (state === "block-comment") {
      if (current === "*" && next === "/") {
        output += "  ";
        state = "code";
        index += 1;
      } else {
        output += current === "\n" ? current : " ";
      }
      continue;
    }

    if (state === "string") {
      if (current === "\\" && next !== "") {
        output += "  ";
        index += 1;
      } else if (current === "\"") {
        output += " ";
        state = "code";
      } else {
        output += current === "\n" ? current : " ";
      }
      continue;
    }

    if (state === "verbatim-string") {
      if (current === "\"" && next === "\"") {
        output += "  ";
        index += 1;
      } else if (current === "\"") {
        output += " ";
        state = "code";
      } else {
        output += current === "\n" ? current : " ";
      }
      continue;
    }

    if (state === "char") {
      if (current === "\\" && next !== "") {
        output += "  ";
        index += 1;
      } else if (current === "'") {
        output += " ";
        state = "code";
      } else {
        output += current === "\n" ? current : " ";
      }
    }
  }

  return output;
}

function countChar(text, character) {
  let count = 0;
  for (const value of text) {
    if (value === character) count += 1;
  }
  return count;
}

function extractAttributeNames(line) {
  const names = [];
  const attributeRegex = /\[([^\]]+)\]/gu;
  let match;
  while ((match = attributeRegex.exec(line)) !== null) {
    const parts = match[1].split(",");
    for (const part of parts) {
      const nameMatch = part.trim().match(/^([A-Za-z_]\w*)(?:Attribute)?(?:\s*\(|$)/u);
      if (nameMatch) names.push(nameMatch[1]);
    }
  }
  return names;
}

function removeAttributeBlocks(line) {
  return line.replace(/\[[^\]]+\]/gu, " ").trim();
}

const serializedFieldTypesWithoutRequired = new Set([
  "bool",
  "byte",
  "sbyte",
  "short",
  "ushort",
  "int",
  "uint",
  "long",
  "ulong",
  "float",
  "double",
  "decimal",
  "char",
  "string",
  "Color",
  "Color32",
  "Vector2",
  "Vector2Int",
  "Vector3",
  "Vector3Int",
  "Vector4",
  "Quaternion",
  "Rect",
  "RectInt",
  "Bounds",
  "BoundsInt",
  "LayerMask",
]);

const fieldModifiers = new Set([
  "public",
  "private",
  "protected",
  "internal",
  "static",
  "readonly",
  "volatile",
  "new",
  "unsafe",
]);

function collectEnumNames(files) {
  const enumNames = new Set();
  for (const filePath of files) {
    const sanitizedLines = stripCommentsAndStrings(fs.readFileSync(filePath, "utf8")).split("\n");
    for (const line of sanitizedLines) {
      const match = line.match(/\benum\s+([A-Za-z_]\w*)\b/u);
      if (match) enumNames.add(match[1]);
    }
  }

  return enumNames;
}

function parseFieldDeclaration(fieldCandidate) {
  const declaration = fieldCandidate.slice(0, fieldCandidate.indexOf(";")).split("=")[0].trim();
  const tokens = declaration.split(/\s+/u).filter(Boolean);
  const meaningfulTokens = tokens.filter((token) => !fieldModifiers.has(token));
  if (meaningfulTokens.length < 2) return null;

  const name = meaningfulTokens[meaningfulTokens.length - 1];
  const type = meaningfulTokens.slice(0, -1).join(" ");
  return { type, name };
}

function normalizeFieldType(typeName) {
  return typeName
    .replace(/\?$/u, "")
    .replace(/\[\]$/u, "")
    .split(".")
    .pop()
    .trim();
}

function canSerializeWithoutRequired(fieldCandidate, enumNames) {
  const declaration = parseFieldDeclaration(fieldCandidate);
  if (!declaration) return false;

  const normalizedType = normalizeFieldType(declaration.type);
  return serializedFieldTypesWithoutRequired.has(normalizedType) || enumNames.has(normalizedType);
}

function parseClassDeclaration(line) {
  const match = line.match(/\b(?:class|struct)\s+([A-Za-z_]\w*)(?:\s*:\s*([^{]+))?/u);
  if (!match) return null;

  return {
    name: match[1],
    bases: (match[2] ?? "")
      .split(",")
      .map((value) => value.trim().split(/\s+/u).pop() ?? "")
      .map((value) => value.replace(/[<{].*$/u, ""))
      .filter(Boolean),
  };
}

function collectClassBases(files, root) {
  const classBases = new Map();
  for (const filePath of files) {
    const sanitizedLines = stripCommentsAndStrings(fs.readFileSync(filePath, "utf8")).split("\n");
    for (const line of sanitizedLines) {
      const declaration = parseClassDeclaration(line);
      if (!declaration) continue;
      classBases.set(declaration.name, declaration.bases);
    }
  }

  return classBases;
}

function isMonoBehaviourType(className, classBases, seen = new Set()) {
  if (!className || seen.has(className)) return false;
  seen.add(className);

  const bases = classBases.get(className) ?? [];
  for (const baseName of bases) {
    if (baseName.includes("MonoBehaviour")) return true;
    if (isMonoBehaviourType(baseName, classBases, seen)) return true;
  }

  return false;
}

function extractCallableHeader(signature) {
  const collapsed = signature.replace(/\s+/gu, " ").trim();
  if (!collapsed.includes("(") || collapsed.includes("=>")) return null;

  const prefix = collapsed.slice(0, collapsed.indexOf("(")).trim();
  const name = prefix.split(/\s+/u).pop();
  if (!name || callableBlockers.has(name)) return null;

  const match = collapsed.match(/([A-Za-z_]\w*)\s*\(([^)]*)\)\s*(?:where\b.*)?$/u);
  if (!match) return null;

  if (callableBlockers.has(match[1])) return null;

  return {
    name: match[1],
    parameters: match[2].trim(),
    parameterNames: extractParameterNames(match[2].trim()),
  };
}

function isNullDefenseLine(sanitizedLine) {
  return nullDefenseRegexes.some((regex) => regex.test(sanitizedLine));
}

function isAcceptedNullDefenseLine(sanitizedLine) {
  return isDelegateInvocationLine(sanitizedLine);
}

function isDelegateInvocationLine(sanitizedLine) {
  return /\?\.\s*Invoke\s*\(/u.test(sanitizedLine);
}

function extractParameterNames(parametersText) {
  if (parametersText.length === 0) return [];

  return splitParameters(parametersText)
    .map((parameter) => parameter.replace(/=.*/u, "").trim())
    .map((parameter) => parameter.replace(/\[[^\]]+\]/gu, "").trim())
    .map((parameter) => parameter.replace(/\b(?:this|ref|out|in|params)\b/gu, "").trim())
    .map((parameter) => {
      const match = parameter.match(/([A-Za-z_]\w*)\s*$/u);
      return match?.[1] ?? "";
    })
    .filter(Boolean);
}

function splitParameters(parametersText) {
  const parameters = [];
  let current = "";
  let angleDepth = 0;
  let parenDepth = 0;
  let bracketDepth = 0;

  for (const char of parametersText) {
    if (char === "<") angleDepth += 1;
    if (char === ">") angleDepth = Math.max(0, angleDepth - 1);
    if (char === "(") parenDepth += 1;
    if (char === ")") parenDepth = Math.max(0, parenDepth - 1);
    if (char === "[") bracketDepth += 1;
    if (char === "]") bracketDepth = Math.max(0, bracketDepth - 1);

    if (char === "," && angleDepth === 0 && parenDepth === 0 && bracketDepth === 0) {
      parameters.push(current.trim());
      current = "";
      continue;
    }

    current += char;
  }

  if (current.trim().length > 0) {
    parameters.push(current.trim());
  }

  return parameters;
}

function nullDefenseTargetsOnlyParameters(sanitizedLine, parameterNames) {
  const parameterSet = new Set(parameterNames);
  const targets = extractNullDefenseTargets(sanitizedLine);
  return targets.length > 0 && targets.every((target) => parameterSet.has(target));
}

function extractNullDefenseTargets(sanitizedLine) {
  const targets = [];
  const patterns = [
    /\b([A-Za-z_]\w*(?:\.[A-Za-z_]\w*)*)\s*(?:==|!=)\s*null\b/gu,
    /\bnull\s*(?:==|!=)\s*([A-Za-z_]\w*(?:\.[A-Za-z_]\w*)*)\b/gu,
    /\b([A-Za-z_]\w*(?:\.[A-Za-z_]\w*)*)\s+is\s+(?:not\s+)?null\b/gu,
    /\b([A-Za-z_]\w*(?:\.[A-Za-z_]\w*)*)\s*\?\?/gu,
    /\b([A-Za-z_]\w*)\s*\?\./gu,
    /\b([A-Za-z_]\w*)\s*\?\[/gu,
  ];

  for (const pattern of patterns) {
    let match;
    while ((match = pattern.exec(sanitizedLine)) !== null) {
      targets.push(match[1]);
    }
  }

  return targets;
}

function hasDirective(rawLines, index, directive) {
  for (const offset of [0, -1]) {
    const line = rawLines[index + offset];
    if (line && line.includes(`csharp-guardrails: ${directive}`)) return true;
  }

  return false;
}

function createIssue(code, relativePath, line, key, message) {
  return {
    code,
    path: relativePath,
    line,
    key,
    message,
  };
}

function analyzeFile(filePath, root, classBases, enumNames) {
  const relativePath = repoRelative(filePath, root);
  const rawText = normalizeNewlines(fs.readFileSync(filePath, "utf8"));
  const rawLines = rawText.split("\n");
  const sanitizedLines = stripCommentsAndStrings(rawText).split("\n");
  const issues = [];

  const classStack = [];
  const callableStack = [];
  let pendingClass = null;
  let pendingCallable = null;
  let pendingAttributes = [];
  let braceDepth = 0;

  for (let index = 0; index < sanitizedLines.length; index += 1) {
    const sanitizedLine = sanitizedLines[index];
    const rawLine = rawLines[index] ?? "";
    const trimmed = sanitizedLine.trim();

    if (pendingClass && trimmed.includes("{")) {
      classStack.push({
        ...pendingClass,
        depth: braceDepth + 1,
        isMonoBehaviour: isMonoBehaviourType(pendingClass.name, classBases),
      });
      pendingClass = null;
    }

    if (pendingCallable && trimmed.includes("{")) {
      const currentClass = classStack[classStack.length - 1];
      callableStack.push({
        ...pendingCallable,
        depth: braceDepth + 1,
        allowsNullDefense: pendingCallable.name === currentClass?.name || (pendingCallable.parameterNames?.length ?? 0) > 0,
      });
      pendingCallable = null;
    }

    const classDeclaration = parseClassDeclaration(trimmed);
    if (classDeclaration) {
      pendingClass = classDeclaration;
      if (trimmed.includes("{")) {
        classStack.push({
          ...pendingClass,
          depth: braceDepth + countChar(trimmed.slice(0, trimmed.indexOf("{") + 1), "{"),
          isMonoBehaviour: isMonoBehaviourType(pendingClass.name, classBases),
        });
        pendingClass = null;
      }
    }

    const currentClass = classStack[classStack.length - 1];
    const currentCallable = callableStack[callableStack.length - 1];

    if (currentClass?.isMonoBehaviour && callableStack.length === 0) {
      const names = extractAttributeNames(rawLine);
      const hasAttributeOnlyLine = names.length > 0 && removeAttributeBlocks(sanitizedLine).length === 0;
      if (names.length > 0) {
        for (const name of names) {
          pendingAttributes.push({ name, line: index + 1 });
        }
      }

      const fieldCandidate = removeAttributeBlocks(sanitizedLine);
      const fieldDeclarationCandidate = fieldCandidate.includes(";")
        ? fieldCandidate.slice(0, fieldCandidate.indexOf(";")).split("=")[0]
        : "";
      if (fieldCandidate.length > 0 && fieldCandidate.includes(";") && !fieldDeclarationCandidate.includes("(")) {
        const serializeIndex = pendingAttributes.findIndex((attribute) => attribute.name === "SerializeField");
        if (serializeIndex >= 0) {
          const requiredIndex = pendingAttributes.findIndex((attribute) => attribute.name === "Required");
          const fieldDeclaration = parseFieldDeclaration(fieldCandidate);
          const fieldName = fieldDeclaration?.name ?? fieldCandidate.slice(0, fieldCandidate.indexOf(";")).trim();
          if (
            (requiredIndex < 0 || requiredIndex > serializeIndex) &&
            !canSerializeWithoutRequired(fieldCandidate, enumNames) &&
            !hasDirective(rawLines, index, "allow-serialized-field-without-required")
          ) {
            issues.push(
              createIssue(
                "serialized-field-requires-required",
                relativePath,
                index + 1,
                `serialized-field-requires-required:${relativePath}:${currentClass.name}.${fieldName}`,
                `${currentClass.name}.${fieldName} uses [SerializeField] without Required before SerializeField.`,
              ),
            );
          }
        }

        pendingAttributes = [];
      } else if (!hasAttributeOnlyLine && trimmed.length > 0 && !trimmed.startsWith("[") && names.length === 0) {
        pendingAttributes = [];
      }
    }

    if (classStack.length > 0 && callableStack.length === 0 && !pendingCallable) {
      if (trimmed.includes("(") && !trimmed.endsWith(";")) {
        const headerSource = trimmed.includes("{") ? trimmed.slice(0, trimmed.indexOf("{")) : trimmed;
        const header = extractCallableHeader(headerSource.trim());
        if (header) {
          pendingCallable = header;
          if (trimmed.includes("{")) {
            callableStack.push({
              ...pendingCallable,
              depth: braceDepth + countChar(trimmed.slice(0, trimmed.indexOf("{") + 1), "{"),
              allowsNullDefense: pendingCallable.name === currentClass?.name || (pendingCallable.parameterNames?.length ?? 0) > 0,
            });
            pendingCallable = null;
          }
        } else if (!trimmed.includes(";")) {
          pendingCallable = { name: "", parameters: "", partial: trimmed };
        }
      }
    } else if (pendingCallable?.partial) {
      pendingCallable.partial = `${pendingCallable.partial} ${trimmed}`;
      const header = extractCallableHeader(pendingCallable.partial.replace(/\{\s*$/u, "").trim());
      if (header) {
        pendingCallable = header;
      } else if (trimmed.includes(";")) {
        pendingCallable = null;
      }
    }

    const nullDefenseCallable = callableStack[callableStack.length - 1];
    if (
      isNullDefenseLine(trimmed) &&
      !hasDirective(rawLines, index, "allow-null-defense") &&
      !isAcceptedNullDefenseLine(trimmed)
    ) {
      if (
        !nullDefenseCallable?.allowsNullDefense ||
        !nullDefenseTargetsOnlyParameters(trimmed, nullDefenseCallable.parameterNames ?? [])
      ) {
        const snippet = trimmed.replace(/\s+/gu, " ").trim();
        issues.push(
          createIssue(
            "null-defense-only-in-constructors-or-parameterized-functions",
            relativePath,
            index + 1,
            `null-defense-only-in-constructors-or-parameterized-functions:${relativePath}:${snippet}`,
            `Null defense is allowed only for parameters inside constructors or functions with parameters: ${snippet}`,
          ),
        );
      }
    }

    braceDepth += countChar(sanitizedLine, "{") - countChar(sanitizedLine, "}");

    while (callableStack.length > 0 && braceDepth < callableStack[callableStack.length - 1].depth) {
      callableStack.pop();
    }

    while (classStack.length > 0 && braceDepth < classStack[classStack.length - 1].depth) {
      classStack.pop();
    }
  }

  return issues;
}

function lint(root = repoRoot) {
  const rootScripts = path.join(root, "Assets", "Scripts");
  const files = listCsFilesRecursive(rootScripts)
    .filter((filePath) => !repoRelative(filePath, root).includes("/Generated/"))
    .sort((left, right) => left.localeCompare(right));
  const classBases = collectClassBases(files, root);
  const enumNames = collectEnumNames(files);
  return files.flatMap((filePath) => analyzeFile(filePath, root, classBases, enumNames));
}

function readBaseline() {
  if (noBaseline || !fs.existsSync(baselinePath)) {
    return new Set();
  }

  const parsed = JSON.parse(fs.readFileSync(baselinePath, "utf8"));
  return new Set(parsed.issues ?? []);
}

function writeBaseline(issues) {
  const keys = [...new Set(issues.map((issue) => issue.key))].sort();
  const payload = {
    schema: 1,
    description: "Existing C# source guardrail violations. New violations are not allowed.",
    issues: keys,
  };
  fs.writeFileSync(`${baselinePath}.tmp`, `${JSON.stringify(payload, null, 2)}\n`, "utf8");
  fs.renameSync(`${baselinePath}.tmp`, baselinePath);
}

function printIssues(title, issues) {
  if (issues.length === 0) return;
  console.error(title);
  for (const issue of issues.slice(0, 80)) {
    console.error(`  - ${issue.path}:${issue.line} ${issue.message}`);
  }
  if (issues.length > 80) {
    console.error(`  ... ${issues.length - 80} more issue(s) omitted`);
  }
}

function runSelfTest() {
  const tempRoot = fs.mkdtempSync(path.join(os.tmpdir(), "jg-csharp-guardrails-"));
  try {
    const featureRoot = path.join(tempRoot, "Assets", "Scripts", "Features", "Foo");
    fs.mkdirSync(featureRoot, { recursive: true });
    fs.writeFileSync(
      path.join(featureRoot, "FooController.cs"),
      `using UnityEngine;
using Shared.Attributes;

public sealed class FooController : MonoBehaviour
{
    [SerializeField] private GameObject missingRequired;
    [SerializeField] private float serializedFloatOk;
    [SerializeField] private string serializedStringOk;
    [SerializeField] private FooMode serializedEnumOk;
    [Required, SerializeField] private GameObject requiredOk;
    private object _cached;
    private event System.Action Ready;
    private GameObject _previewPrefab;

    public FooController(object ctorValue)
    {
        if (ctorValue == null) return;
        if (_cached == null) return;
    }

    private void Awake()
    {
        if (_cached == null) return;
        if (_previewPrefab == null) return;
        Ready?.Invoke();
    }

    public void Dispose()
    {
        if (_cached == null) return;
    }

    public void Initialize(object value)
    {
        if (value == null) return;
        if (_cached == null) return;
        if (value == null || _cached == null) return;
    }

    public void OneLineAllowed(object value) { if (value == null) return; }
    public void OneLineBlocked(object value) { if (_cached == null) return; }
}

public enum FooMode
{
    One,
}
`,
      "utf8",
    );

    const issues = lint(tempRoot);
    assert.equal(issues.filter((issue) => issue.code === "serialized-field-requires-required").length, 1);
    assert.equal(
      issues.filter((issue) => issue.code === "null-defense-only-in-constructors-or-parameterized-functions").length,
      7,
    );
  } finally {
    fs.rmSync(tempRoot, { recursive: true, force: true });
  }

  console.log("C# source guardrails self-test passed.");
}

if (selfTest) {
  runSelfTest();
  process.exit(0);
}

const issues = lint(repoRoot);

if (shouldWriteBaseline) {
  writeBaseline(issues);
  console.log(`C# source guardrails baseline written. Baseline issue count: ${new Set(issues.map((issue) => issue.key)).size}.`);
  process.exit(0);
}

const baseline = readBaseline();
const newIssues = issues.filter((issue) => !baseline.has(issue.key));
const currentKeys = new Set(issues.map((issue) => issue.key));
const staleBaselineKeys = [...baseline].filter((key) => !currentKeys.has(key));

if (newIssues.length > 0 || staleBaselineKeys.length > 0) {
  printIssues("C# source guardrails failed with new issue(s):", newIssues);
  if (staleBaselineKeys.length > 0) {
    console.error("C# source guardrails baseline has stale issue key(s):");
    for (const key of staleBaselineKeys.slice(0, 80)) {
      console.error(`  - ${key}`);
    }
    if (staleBaselineKeys.length > 80) {
      console.error(`  ... ${staleBaselineKeys.length - 80} more stale key(s) omitted`);
    }
  }
  console.error("");
  console.error("Use [Required, SerializeField] for MonoBehaviour serialized fields.");
  console.error("Keep null defense only for parameters inside constructors or functions with parameters.");
  console.error("For deliberate exceptions, add a narrow directive on the line above:");
  console.error("  // csharp-guardrails: allow-serialized-field-without-required");
  console.error("  // csharp-guardrails: allow-null-defense");
  process.exit(1);
}

console.log("C# source guardrails passed. No violations found.");
