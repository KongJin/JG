using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Features.Account.Infrastructure;
using Features.Garage.Application;
using Features.Garage.Domain;
using Features.Garage.Infrastructure;
using Features.Unit.Domain;
using Features.Unit.Infrastructure;
using UnityEditor;
using UnityEngine;

namespace ProjectSD.EditorTools
{
    public static class Nova1492PartValidationTool
    {
        private const string ModuleCatalogPath = "Assets/Data/Garage/ModuleCatalog.asset";
        private const string ReportPath = "artifacts/nova1492/nova_part_validation_closeout_report.md";

        [MenuItem("Tools/Nova1492/Validate Playable Part Closeout")]
        public static void ValidatePlayablePartCloseout()
        {
            AssetDatabase.Refresh();

            var report = new ValidationReport();
            var catalog = AssetDatabase.LoadAssetAtPath<ModuleCatalog>(ModuleCatalogPath);
            if (catalog == null)
            {
                report.Failures.Add($"ModuleCatalog not found: {ModuleCatalogPath}");
                WriteReport(report);
                throw new FileNotFoundException("ModuleCatalog asset was not found.", ModuleCatalogPath);
            }

            ValidateCatalog(catalog, report);
            ValidateGeneratedCompositions(catalog, report);
            ValidateSaveLoadRoundTrips(catalog, report);

            WriteReport(report);

            if (!report.Success)
            {
                throw new InvalidOperationException($"Nova1492 playable closeout validation failed. See {ReportPath}");
            }

            Debug.Log($"[Nova1492] Playable closeout validation passed. report={ReportPath}");
        }

        private static void ValidateCatalog(ModuleCatalog catalog, ValidationReport report)
        {
            var allIds = new HashSet<string>(StringComparer.Ordinal);
            var duplicateIds = new List<string>();

            foreach (var frame in catalog.UnitFrames)
            {
                if (frame == null)
                {
                    report.Failures.Add("ModuleCatalog contains a null frame entry.");
                    continue;
                }

                AddId(frame.FrameId, allIds, duplicateIds);
                if (IsGeneratedFrame(frame) && frame.PreviewPrefab == null)
                {
                    report.Failures.Add($"Generated frame has no preview prefab: {frame.FrameId}");
                }

                if (IsGeneratedFrame(frame) && (frame.BaseHp <= 0f || frame.Defense < 0f))
                {
                    report.Failures.Add($"Generated frame has non-positive stats: {frame.FrameId}");
                }
            }

            foreach (var firepower in catalog.FirepowerModules)
            {
                if (firepower == null)
                {
                    report.Failures.Add("ModuleCatalog contains a null firepower entry.");
                    continue;
                }

                AddId(firepower.ModuleId, allIds, duplicateIds);
                if (IsGeneratedFirepower(firepower) && firepower.PreviewPrefab == null)
                {
                    report.Failures.Add($"Generated firepower has no preview prefab: {firepower.ModuleId}");
                }

                if (IsGeneratedFirepower(firepower) && (firepower.AttackDamage <= 0f || firepower.AttackSpeed <= 0f || firepower.Range <= 0f))
                {
                    report.Failures.Add($"Generated firepower has non-positive stats: {firepower.ModuleId}");
                }
            }

            foreach (var mobility in catalog.MobilityModules)
            {
                if (mobility == null)
                {
                    report.Failures.Add("ModuleCatalog contains a null mobility entry.");
                    continue;
                }

                AddId(mobility.ModuleId, allIds, duplicateIds);
                if (IsGeneratedMobility(mobility) && mobility.PreviewPrefab == null)
                {
                    report.Failures.Add($"Generated mobility has no preview prefab: {mobility.ModuleId}");
                }

                if (IsGeneratedMobility(mobility) && (mobility.MoveSpeed <= 0f || mobility.MoveRange <= 0f))
                {
                    report.Failures.Add($"Generated mobility has non-positive stats: {mobility.ModuleId}");
                }
            }

            foreach (var duplicateId in duplicateIds)
            {
                report.Failures.Add($"Duplicate catalog id: {duplicateId}");
            }

            report.TotalFrames = catalog.UnitFrames.Count;
            report.TotalFirepower = catalog.FirepowerModules.Count;
            report.TotalMobility = catalog.MobilityModules.Count;
            report.GeneratedFrames = Count(catalog.UnitFrames, IsGeneratedFrame);
            report.GeneratedFirepower = Count(catalog.FirepowerModules, IsGeneratedFirepower);
            report.GeneratedMobility = Count(catalog.MobilityModules, IsGeneratedMobility);
        }

        private static void ValidateGeneratedCompositions(ModuleCatalog catalog, ValidationReport report)
        {
            var generatedFirepower = Filter(catalog.FirepowerModules, IsGeneratedFirepower);
            var generatedMobility = Filter(catalog.MobilityModules, IsGeneratedMobility);

            foreach (var firepower in generatedFirepower)
            {
                foreach (var mobility in generatedMobility)
                {
                    report.GeneratedCompositionChecks++;
                    if (!UnitComposition.Validate(mobility.MoveRange, firepower.Range, out var errorMessage))
                    {
                        report.Failures.Add(
                            $"Invalid generated composition: {firepower.ModuleId} + {mobility.ModuleId}: {errorMessage}");
                    }
                }
            }
        }

        private static void ValidateSaveLoadRoundTrips(ModuleCatalog catalog, ValidationReport report)
        {
            var frames = Filter(catalog.UnitFrames, IsGeneratedFrame);
            var firepower = Filter(catalog.FirepowerModules, IsGeneratedFirepower);
            var mobility = Filter(catalog.MobilityModules, IsGeneratedMobility);
            if (frames.Count < 3 || firepower.Count < 3 || mobility.Count < 3)
            {
                report.Failures.Add("Not enough generated parts to build a 3-slot save/load smoke roster.");
                return;
            }

            var roster = new GarageRoster();
            for (var i = 0; i < 3; i++)
            {
                roster.SetSlot(i, new GarageRoster.UnitLoadout(
                    frames[i].FrameId,
                    firepower[i].ModuleId,
                    mobility[i].ModuleId));
            }

            var validation = new ValidateRosterUseCase(new RosterValidationProvider(catalog));
            var validationResult = validation.Execute(roster, out var rosterError);
            report.RosterUseCaseValidationPassed = validationResult.IsSuccess;
            if (validationResult.IsFailure)
            {
                report.Failures.Add($"Generated smoke roster failed ValidateRosterUseCase: {rosterError}");
                return;
            }

            ValidateFirestoreRoundTrip(roster, report);
            ValidateLocalSaveLoadRoundTrip(roster, report);
        }

        private static void ValidateFirestoreRoundTrip(GarageRoster roster, ValidationReport report)
        {
            var documentJson = FirestoreFieldSerializer.BuildRawJsonDocument(FirestoreGarageMapper.ToJson(roster));
            var restored = FirestoreGarageMapper.FromDocument(documentJson);
            if (!SameRosterIds(roster, restored, 3))
            {
                report.Failures.Add("Firestore garage mapper did not preserve generated part ids.");
                return;
            }

            report.FirestoreRoundTripPassed = true;
        }

        private static void ValidateLocalSaveLoadRoundTrip(GarageRoster roster, ValidationReport report)
        {
            var persistence = new GarageJsonPersistence();
            var path = Path.Combine(Application.persistentDataPath, "garage_roster.json");
            var backupPath = path + ".nova1492-closeout-backup";
            var hadOriginal = File.Exists(path);

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                }

                if (hadOriginal)
                {
                    File.Copy(path, backupPath);
                }

                persistence.Save(roster);
                var restored = persistence.Load();
                if (!SameRosterIds(roster, restored, 3))
                {
                    report.Failures.Add("Local GarageJsonPersistence save/load did not preserve generated part ids.");
                    return;
                }

                report.LocalSaveLoadPassed = true;
                report.LocalSaveLoadPath = path;
            }
            finally
            {
                if (hadOriginal)
                {
                    File.Copy(backupPath, path, true);
                }
                else if (File.Exists(path))
                {
                    File.Delete(path);
                }

                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                }
            }
        }

        private static bool SameRosterIds(GarageRoster expected, GarageRoster actual, int slotCount)
        {
            if (expected == null || actual == null)
            {
                return false;
            }

            expected.Normalize();
            actual.Normalize();
            for (var i = 0; i < slotCount; i++)
            {
                var left = expected.GetSlot(i);
                var right = actual.GetSlot(i);
                if (!string.Equals(left.frameId, right.frameId, StringComparison.Ordinal) ||
                    !string.Equals(left.firepowerModuleId, right.firepowerModuleId, StringComparison.Ordinal) ||
                    !string.Equals(left.mobilityModuleId, right.mobilityModuleId, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        private static void AddId(string id, HashSet<string> ids, List<string> duplicates)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                duplicates.Add("<empty>");
                return;
            }

            if (!ids.Add(id))
            {
                duplicates.Add(id);
            }
        }

        private static bool IsGeneratedFrame(UnitFrameData frame) =>
            frame != null && frame.FrameId != null && frame.FrameId.StartsWith("nova_frame_", StringComparison.Ordinal);

        private static bool IsGeneratedFirepower(FirepowerModuleData firepower) =>
            firepower != null && firepower.ModuleId != null && firepower.ModuleId.StartsWith("nova_fire_", StringComparison.Ordinal);

        private static bool IsGeneratedMobility(MobilityModuleData mobility) =>
            mobility != null && mobility.ModuleId != null && mobility.ModuleId.StartsWith("nova_mob_", StringComparison.Ordinal);

        private static int Count<T>(IReadOnlyList<T> values, Func<T, bool> predicate)
        {
            var count = 0;
            for (var i = 0; i < values.Count; i++)
            {
                if (predicate(values[i]))
                {
                    count++;
                }
            }

            return count;
        }

        private static List<T> Filter<T>(IReadOnlyList<T> values, Func<T, bool> predicate)
        {
            var results = new List<T>();
            for (var i = 0; i < values.Count; i++)
            {
                if (predicate(values[i]))
                {
                    results.Add(values[i]);
                }
            }

            return results;
        }

        private static void WriteReport(ValidationReport report)
        {
            var builder = new StringBuilder();
            builder.AppendLine("# Nova1492 Playable Closeout Validation Report");
            builder.AppendLine();
            builder.AppendLine($"> generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            builder.AppendLine();
            builder.AppendLine("## Summary");
            builder.AppendLine();
            builder.AppendLine($"- success: {report.Success.ToString(CultureInfo.InvariantCulture).ToLowerInvariant()}");
            builder.AppendLine($"- module catalog: `{ModuleCatalogPath}`");
            builder.AppendLine($"- total catalog counts: Frame {report.TotalFrames}, Firepower {report.TotalFirepower}, Mobility {report.TotalMobility}");
            builder.AppendLine($"- generated counts: Frame {report.GeneratedFrames}, Firepower {report.GeneratedFirepower}, Mobility {report.GeneratedMobility}");
            builder.AppendLine($"- generated Firepower x Mobility checks: {report.GeneratedCompositionChecks}");
            builder.AppendLine($"- roster use case validation: {Status(report.RosterUseCaseValidationPassed)}");
            builder.AppendLine($"- Firestore mapper roundtrip: {Status(report.FirestoreRoundTripPassed)}");
            builder.AppendLine($"- local save/load roundtrip: {Status(report.LocalSaveLoadPassed)}");
            if (!string.IsNullOrWhiteSpace(report.LocalSaveLoadPath))
            {
                builder.AppendLine($"- local save path restored after smoke: `{report.LocalSaveLoadPath}`");
            }

            builder.AppendLine();
            builder.AppendLine("## Failures");
            builder.AppendLine();
            if (report.Failures.Count == 0)
            {
                builder.AppendLine("- none");
            }
            else
            {
                foreach (var failure in report.Failures)
                {
                    builder.AppendLine($"- {failure}");
                }
            }

            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
            File.WriteAllText(ReportPath, builder.ToString(), Encoding.UTF8);
        }

        private static string Status(bool value) => value ? "pass" : "fail";

        private sealed class ValidationReport
        {
            public int TotalFrames;
            public int TotalFirepower;
            public int TotalMobility;
            public int GeneratedFrames;
            public int GeneratedFirepower;
            public int GeneratedMobility;
            public int GeneratedCompositionChecks;
            public bool RosterUseCaseValidationPassed;
            public bool FirestoreRoundTripPassed;
            public bool LocalSaveLoadPassed;
            public string LocalSaveLoadPath;
            public readonly List<string> Failures = new List<string>();

            public bool Success => Failures.Count == 0;
        }

    }
}
