using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Tests.Editor
{
    public sealed class ArchitectureGuardrailReflectionTests
    {
        private static readonly HashSet<string> ResourcesLoadAllowlist = new(StringComparer.OrdinalIgnoreCase)
        {
            NormalizeRelativePath("Assets/Scripts/Features/Enemy/EnemySetup.cs"),
            NormalizeRelativePath("Assets/Scripts/Features/Player/Infrastructure/DefaultPlayerSpecProvider.cs"),
            NormalizeRelativePath("Assets/Scripts/Shared/Ui/RoundedRectGraphic.cs"),
        };

        private static readonly HashSet<string> SingletonAllowlist = new(StringComparer.OrdinalIgnoreCase)
        {
            NormalizeRelativePath("Assets/Scripts/Features/Combat/CombatSetup.cs"),
            NormalizeRelativePath("Assets/Scripts/Shared/Runtime/Sound/SoundPlayer.cs"),
        };

        [Test]
        public void SetupAndRootFiles_DoNotDeclareUpdateMethods()
        {
            var offenders = EnumerateScriptFiles()
                .Where(path => path.EndsWith("Setup.cs", StringComparison.OrdinalIgnoreCase) ||
                               path.EndsWith("Root.cs", StringComparison.OrdinalIgnoreCase))
                .Where(path => File.ReadAllText(path).Contains("void Update(", StringComparison.Ordinal))
                .Select(ToRepoRelativePath)
                .OrderBy(path => path)
                .ToArray();

            Assert.That(
                offenders,
                Is.Empty,
                BuildFailureMessage("Setup/Root classes must stay wiring-only. See .codex/skills/jg-unity-workflow/SKILL.md.", offenders));
        }

        [Test]
        public void Codebase_DoesNotUseAsyncVoidHandlers()
        {
            var offenders = EnumerateScriptFiles()
                .Where(path => File.ReadAllText(path).Contains("async void", StringComparison.Ordinal))
                .Select(ToRepoRelativePath)
                .OrderBy(path => path)
                .ToArray();

            Assert.That(
                offenders,
                Is.Empty,
                BuildFailureMessage("Use thin wrapper handlers that delegate to Task methods. See .codex/skills/jg-unity-workflow/SKILL.md.", offenders));
        }

        [Test]
        public void ResourcesLoad_IsRestrictedToAllowlist()
        {
            var offenders = EnumerateScriptFiles()
                .Select(path => new
                {
                    RelativePath = ToRepoRelativePath(path),
                    Text = File.ReadAllText(path),
                })
                .Where(entry =>
                    (entry.Text.Contains("Resources.Load<", StringComparison.Ordinal) ||
                     entry.Text.Contains("Resources.Load(", StringComparison.Ordinal)) &&
                    !ResourcesLoadAllowlist.Contains(NormalizeRelativePath(entry.RelativePath)))
                .Select(entry => entry.RelativePath)
                .OrderBy(path => path)
                .ToArray();

            Assert.That(
                offenders,
                Is.Empty,
                BuildFailureMessage("Resources.Load usage must stay isolated behind a small allowlist. See .codex/skills/jg-unity-workflow/SKILL.md.", offenders));
        }

        [Test]
        public void SingletonPatterns_AreRestrictedToAllowlist()
        {
            var offenders = EnumerateScriptFiles()
                .Select(path => new
                {
                    RelativePath = ToRepoRelativePath(path),
                    Text = File.ReadAllText(path),
                })
                .Where(entry =>
                    (entry.Text.Contains("public static", StringComparison.Ordinal) && entry.Text.Contains(" Instance", StringComparison.Ordinal) ||
                     entry.Text.Contains("DontDestroyOnLoad(", StringComparison.Ordinal)) &&
                    !SingletonAllowlist.Contains(NormalizeRelativePath(entry.RelativePath)))
                .Select(entry => entry.RelativePath)
                .OrderBy(path => path)
                .ToArray();

            Assert.That(
                offenders,
                Is.Empty,
                BuildFailureMessage("New singleton/static scene dependencies are blocked by guardrail. See .codex/skills/jg-unity-workflow/SKILL.md.", offenders));
        }

        [Test]
        public void UnityWorkflowSkill_ContainsArchitectureGuardrails()
        {
            string skillPath = Path.Combine(GetRepoRoot(), ".codex", "skills", "jg-unity-workflow", "SKILL.md");
            Assert.That(File.Exists(skillPath), Is.True, "Expected the canonical Unity workflow skill file to exist.");

            string text = File.ReadAllText(skillPath);

            StringAssert.Contains("Architecture Guardrails", text);
            StringAssert.Contains("`*Setup` and `*Root` classes are wiring-only entry points.", text);
            StringAssert.Contains("`async void` is forbidden", text);
            StringAssert.Contains("`Resources.Load`, `transform.Find`, and runtime child traversal", text);
            StringAssert.Contains("Refactor Checklist", text);
        }

        private static IEnumerable<string> EnumerateScriptFiles()
        {
            string scriptsRoot = Path.Combine(GetRepoRoot(), "Assets", "Scripts");
            return Directory.EnumerateFiles(scriptsRoot, "*.cs", SearchOption.AllDirectories);
        }

        private static string BuildFailureMessage(string headline, IReadOnlyCollection<string> offenders)
        {
            return offenders.Count == 0
                ? headline
                : headline + Environment.NewLine + string.Join(Environment.NewLine, offenders);
        }

        private static string GetRepoRoot()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        }

        private static string ToRepoRelativePath(string absolutePath)
        {
            string repoRoot = GetRepoRoot().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string trimmed = absolutePath.Substring(repoRoot.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return trimmed.Replace('\\', '/');
        }

        private static string NormalizeRelativePath(string path)
        {
            return path.Replace('\\', '/');
        }
    }
}
