using System;
using System.Collections.Generic;
using Features.Unit.Domain;
using UnityEngine;

namespace Features.Garage.Presentation
{
    internal static class GarageNovaPartsPanelOptionBuilder
    {
        public static List<GarageNovaPartOptionViewModel> BuildOptions(
            GaragePanelCatalog catalog,
            GarageNovaPartPanelSlot slot,
            GarageNovaPartsDraftSelection draftSelection)
        {
            var candidates = BuildCandidates(catalog, slot, draftSelection);
            var options = new List<GarageNovaPartOptionViewModel>(candidates.Count);
            string selectedId = GetSelectedId(draftSelection, slot);

            for (int i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[i];
                options.Add(candidate.ToViewModel(candidate.Id == selectedId));
            }

            return options;
        }

        public static List<GarageNovaPartOptionViewModel> FilterOptions(
            List<GarageNovaPartOptionViewModel> options,
            string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return options;

            var filtered = new List<GarageNovaPartOptionViewModel>();
            for (int i = 0; i < options.Count; i++)
            {
                var option = options[i];
                if (Contains(option.Id, searchText) ||
                    Contains(option.DisplayName, searchText) ||
                    Contains(option.SourcePath, searchText))
                {
                    filtered.Add(option);
                }
            }

            return filtered;
        }

        private static List<Candidate> BuildCandidates(
            GaragePanelCatalog catalog,
            GarageNovaPartPanelSlot slot,
            GarageNovaPartsDraftSelection draftSelection)
        {
            var options = new List<Candidate>();
            if (catalog == null)
                return options;

            var selectedFrame = catalog.FindFrame(draftSelection.FrameId);
            switch (slot)
            {
                case GarageNovaPartPanelSlot.Frame:
                    for (int i = 0; i < catalog.Frames.Count; i++)
                    {
                        var part = catalog.Frames[i];
                        options.Add(new Candidate(
                            slot,
                            part.Id,
                            part.DisplayName,
                            BuildFrameMetaText(part),
                            part.SourcePath,
                            part.NeedsNameReview,
                            part.PreviewPrefab,
                            part.Alignment,
                            BuildEnergyText(part.EnergyCost),
                            BuildFrameStats(catalog, part)));
                    }
                    break;
                case GarageNovaPartPanelSlot.Firepower:
                    for (int i = 0; i < catalog.Firepower.Count; i++)
                    {
                        var part = catalog.Firepower[i];
                        if (selectedFrame != null && !UnitPartCompatibility.AreAssemblyFormsCompatible(selectedFrame.AssemblyForm, part.AssemblyForm))
                            continue;

                        options.Add(new Candidate(
                            slot,
                            part.Id,
                            part.DisplayName,
                            BuildFirepowerMetaText(part),
                            part.SourcePath,
                            part.NeedsNameReview,
                            part.PreviewPrefab,
                            part.Alignment,
                            BuildEnergyText(part.EnergyCost),
                            BuildFirepowerStats(catalog, part)));
                    }
                    break;
                case GarageNovaPartPanelSlot.Mobility:
                    for (int i = 0; i < catalog.Mobility.Count; i++)
                    {
                        var part = catalog.Mobility[i];
                        options.Add(new Candidate(
                            slot,
                            part.Id,
                            part.DisplayName,
                            BuildMobilityMetaText(part),
                            part.SourcePath,
                            part.NeedsNameReview,
                            part.PreviewPrefab,
                            part.Alignment,
                            BuildEnergyText(part.EnergyCost),
                            BuildMobilityStats(catalog, part)));
                    }
                    break;
            }

            return options;
        }

        private static bool Contains(string text, string searchText)
        {
            return !string.IsNullOrWhiteSpace(text) &&
                   text.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string GetSelectedId(GarageNovaPartsDraftSelection draftSelection, GarageNovaPartPanelSlot slot)
        {
            return slot switch
            {
                GarageNovaPartPanelSlot.Frame => draftSelection.FrameId,
                GarageNovaPartPanelSlot.Firepower => draftSelection.FirepowerId,
                _ => draftSelection.MobilityId,
            };
        }

        private static string BuildFrameMetaText(GaragePanelCatalog.FrameOption part)
        {
            return $"HP {part.BaseHp:0} | DEF {part.Defense:0} | ASPD {part.BaseAttackSpeed:0.00}";
        }

        private static string BuildFirepowerMetaText(GaragePanelCatalog.FirepowerOption part)
        {
            return $"ATK {part.AttackDamage:0} | RNG {part.Range:0.0}";
        }

        private static string BuildMobilityMetaText(GaragePanelCatalog.MobilityOption part)
        {
            return part.HpBonus > 0f
                ? $"HP+ {part.HpBonus:0} | MOV {part.MoveRange:0.0}"
                : $"MOV {part.MoveRange:0.0}";
        }

        private static string BuildEnergyText(int energyCost)
        {
            return energyCost == 0 ? string.Empty : $"EN {energyCost}";
        }

        private static IReadOnlyList<GarageNovaPartStatViewModel> BuildFrameStats(
            GaragePanelCatalog catalog,
            GaragePanelCatalog.FrameOption part)
        {
            var scale = catalog?.RadarScale ?? new GaragePanelCatalog.StatRadarScale();
            var stats = new List<GarageNovaPartStatViewModel>(3);
            AddStat(stats, "HP", part.BaseHp, "0", scale.HpMax);
            AddStat(stats, "DEF", part.Defense, "0", scale.DefenseMax);
            AddStat(stats, "ASPD", part.BaseAttackSpeed, "0.00", scale.AttackSpeedMax);
            return stats;
        }

        private static IReadOnlyList<GarageNovaPartStatViewModel> BuildFirepowerStats(
            GaragePanelCatalog catalog,
            GaragePanelCatalog.FirepowerOption part)
        {
            var scale = catalog?.RadarScale ?? new GaragePanelCatalog.StatRadarScale();
            var stats = new List<GarageNovaPartStatViewModel>(3);
            AddStat(stats, "ATK", part.AttackDamage, "0", scale.AttackDamageMax);
            AddStat(stats, "RNG", part.Range, "0.0", scale.RangeMax);
            AddStat(stats, "ASPD", part.AttackSpeed, "0.00", scale.AttackSpeedMax);
            return stats;
        }

        private static IReadOnlyList<GarageNovaPartStatViewModel> BuildMobilityStats(
            GaragePanelCatalog catalog,
            GaragePanelCatalog.MobilityOption part)
        {
            var scale = catalog?.RadarScale ?? new GaragePanelCatalog.StatRadarScale();
            var stats = new List<GarageNovaPartStatViewModel>(3);
            AddStat(stats, "MOV", part.MoveRange, "0.0", scale.MoveRangeMax);
            AddStat(stats, "SPD", part.MoveSpeed, "0.0", scale.MoveSpeedMax);
            AddStat(stats, "HP", part.HpBonus, "0", scale.HpMax);
            return stats;
        }

        private static void AddStat(
            List<GarageNovaPartStatViewModel> stats,
            string label,
            float value,
            string format,
            float max)
        {
            if (stats == null || stats.Count >= 3 || Mathf.Approximately(value, 0f))
                return;

            float safeMax = Mathf.Max(0.0001f, max);
            stats.Add(new GarageNovaPartStatViewModel(
                label,
                value.ToString(format),
                value / safeMax * 100f));
        }

        private readonly struct Candidate
        {
            public Candidate(
                GarageNovaPartPanelSlot slot,
                string id,
                string displayName,
                string detailText,
                string sourcePath,
                bool needsNameReview,
                GameObject previewPrefab,
                GaragePanelCatalog.PartAlignment alignment,
                string energyText,
                IReadOnlyList<GarageNovaPartStatViewModel> stats)
            {
                Slot = slot;
                Id = id;
                DisplayName = displayName;
                DetailText = detailText;
                SourcePath = sourcePath;
                NeedsNameReview = needsNameReview;
                PreviewPrefab = previewPrefab;
                Alignment = alignment;
                EnergyText = energyText ?? string.Empty;
                Stats = stats ?? Array.Empty<GarageNovaPartStatViewModel>();
            }

            private GarageNovaPartPanelSlot Slot { get; }
            public string Id { get; }
            private string DisplayName { get; }
            private string DetailText { get; }
            private string SourcePath { get; }
            private bool NeedsNameReview { get; }
            private GameObject PreviewPrefab { get; }
            private GaragePanelCatalog.PartAlignment Alignment { get; }
            private string MetaText => DetailText;
            private string EnergyText { get; }
            private IReadOnlyList<GarageNovaPartStatViewModel> Stats { get; }

            public GarageNovaPartOptionViewModel ToViewModel(bool isSelected)
            {
                return new GarageNovaPartOptionViewModel(
                    Slot,
                    Id,
                    DisplayName,
                    DetailText,
                    SourcePath,
                    isSelected,
                    NeedsNameReview,
                    PreviewPrefab,
                    Alignment,
                    MetaText,
                    EnergyText,
                    Stats);
            }
        }
    }
}
