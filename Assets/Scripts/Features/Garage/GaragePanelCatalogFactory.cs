using Features.Garage.Infrastructure;
using Features.Garage.Presentation;
using Features.Unit.Domain;
using Features.Unit.Infrastructure;

namespace Features.Garage
{
    internal sealed class GaragePanelCatalogFactory
    {
        public GaragePanelCatalog Build(
            ModuleCatalog unitCatalog,
            NovaPartVisualCatalog novaPartVisualCatalog = null,
            NovaPartAlignmentCatalog novaPartAlignmentCatalog = null)
        {
            var novaMetadata = BuildNovaMetadataByPartId(novaPartVisualCatalog);
            var novaAlignment = BuildNovaAlignmentByPartId(novaPartAlignmentCatalog);
            var costTuning = unitCatalog.StatTuning != null
                ? unitCatalog.StatTuning.ToCostTuning()
                : CostCalculator.StatCostTuning.Default;
            var frames = new System.Collections.Generic.List<GaragePanelCatalog.FrameOption>();
            for (int i = 0; i < unitCatalog.UnitFrames.Count; i++)
            {
                var frame = unitCatalog.UnitFrames[i];
                novaMetadata.TryGetValue(frame.FrameId, out var metadata);
                novaAlignment.TryGetValue(frame.FrameId, out var alignment);
                int energyCost = CostCalculator.CalculatePart(
                    costTuning.DispersionPenaltyFactor,
                    frame.PassiveTrait != null ? frame.PassiveTrait.CostBonus : 0,
                    frame.BaseHp * costTuning.HpWeight,
                    frame.Defense * costTuning.DefenseWeight);
                frames.Add(new GaragePanelCatalog.FrameOption
                {
                    Id = frame.FrameId,
                    DisplayName = frame.DisplayName,
                    BaseHp = frame.BaseHp,
                    Defense = frame.Defense,
                    EnergyCost = energyCost,
                    AssemblyForm = frame.AssemblyForm,
                    PreviewPrefab = ResolvePreviewPrefab(frame.PreviewPrefab, metadata),
                    AssemblyPrefab = ResolveAssemblyPrefab(metadata),
                    SourcePath = metadata?.SourceRelativePath,
                    Tier = metadata?.Tier ?? 0,
                    NeedsNameReview = metadata?.NeedsNameReview ?? false,
                    Alignment = CreateAlignment(alignment)
                });
            }

            var firepower = new System.Collections.Generic.List<GaragePanelCatalog.FirepowerOption>();
            for (int i = 0; i < unitCatalog.FirepowerModules.Count; i++)
            {
                var module = unitCatalog.FirepowerModules[i];
                novaMetadata.TryGetValue(module.ModuleId, out var metadata);
                novaAlignment.TryGetValue(module.ModuleId, out var alignment);
                int energyCost = CostCalculator.CalculatePart(
                    costTuning.DispersionPenaltyFactor,
                    fixedBonus: 0,
                    module.AttackDamage * costTuning.AttackDamageWeight,
                    module.AttackSpeed * costTuning.AttackSpeedWeight,
                    module.Range * costTuning.RangeWeight);
                firepower.Add(new GaragePanelCatalog.FirepowerOption
                {
                    Id = module.ModuleId,
                    DisplayName = module.DisplayName,
                    AttackDamage = module.AttackDamage,
                    AttackSpeed = module.AttackSpeed,
                    Range = module.Range,
                    EnergyCost = energyCost,
                    AssemblyForm = module.AssemblyForm,
                    PreviewPrefab = ResolvePreviewPrefab(module.PreviewPrefab, metadata),
                    AssemblyPrefab = ResolveAssemblyPrefab(metadata),
                    SourcePath = metadata?.SourceRelativePath,
                    Tier = metadata?.Tier ?? 0,
                    NeedsNameReview = metadata?.NeedsNameReview ?? false,
                    Alignment = CreateAlignment(alignment)
                });
            }

            var mobility = new System.Collections.Generic.List<GaragePanelCatalog.MobilityOption>();
            for (int i = 0; i < unitCatalog.MobilityModules.Count; i++)
            {
                var module = unitCatalog.MobilityModules[i];
                novaMetadata.TryGetValue(module.ModuleId, out var metadata);
                novaAlignment.TryGetValue(module.ModuleId, out var alignment);
                int energyCost = CostCalculator.CalculatePart(
                    costTuning.DispersionPenaltyFactor,
                    fixedBonus: 0,
                    module.MoveSpeed * costTuning.MoveSpeedWeight,
                    module.MoveRange * costTuning.MoveRangeWeight);
                mobility.Add(new GaragePanelCatalog.MobilityOption
                {
                    Id = module.ModuleId,
                    DisplayName = module.DisplayName,
                    MoveSpeed = module.MoveSpeed,
                    MoveRange = module.MoveRange,
                    EnergyCost = energyCost,
                    MobilitySurface = module.MobilitySurface,
                    PreviewPrefab = ResolvePreviewPrefab(module.PreviewPrefab, metadata),
                    AssemblyPrefab = ResolveAssemblyPrefab(metadata),
                    // Generated assembly prefabs keep the source model pivot; catalog alignment owns the body socket.
                    UseAssemblyPivot = false,
                    SourcePath = metadata?.SourceRelativePath,
                    Tier = metadata?.Tier ?? 0,
                    NeedsNameReview = metadata?.NeedsNameReview ?? false,
                    Alignment = CreateAlignment(alignment)
                });
            }

            return new GaragePanelCatalog(frames, firepower, mobility, BuildRadarScale(unitCatalog.StatTuning));
        }

        private static GaragePanelCatalog.StatRadarScale BuildRadarScale(UnitStatTuningData statTuning)
        {
            if (statTuning == null)
                return new GaragePanelCatalog.StatRadarScale();

            return new GaragePanelCatalog.StatRadarScale
            {
                AttackDamageMax = statTuning.AttackDamageRadarMax,
                AttackSpeedMax = statTuning.AttackSpeedRadarMax,
                RangeMax = statTuning.RangeRadarMax,
                HpMax = statTuning.HpRadarMax,
                DefenseMax = statTuning.DefenseRadarMax,
                MoveSpeedMax = statTuning.MoveSpeedRadarMax,
                MoveRangeMax = statTuning.MoveRangeRadarMax
            };
        }

        private static System.Collections.Generic.Dictionary<string, NovaPartVisualCatalog.Entry> BuildNovaMetadataByPartId(
            NovaPartVisualCatalog novaPartVisualCatalog)
        {
            var byPartId = new System.Collections.Generic.Dictionary<string, NovaPartVisualCatalog.Entry>();
            if (novaPartVisualCatalog == null)
                return byPartId;

            for (int i = 0; i < novaPartVisualCatalog.Entries.Count; i++)
            {
                var entry = novaPartVisualCatalog.Entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.PartId))
                    continue;

                byPartId[entry.PartId] = entry;
                var legacyPartId = ResolveLegacySamplePartId(entry.PartId);
                if (!string.IsNullOrWhiteSpace(legacyPartId))
                    byPartId[legacyPartId] = entry;
            }

            return byPartId;
        }

        private static UnityEngine.GameObject ResolvePreviewPrefab(
            UnityEngine.GameObject directPrefab,
            NovaPartVisualCatalog.Entry metadata)
        {
            if (directPrefab != null)
                return directPrefab;

            if (metadata?.PreviewPrefab != null)
                return metadata.PreviewPrefab;

#if UNITY_EDITOR
            if (metadata != null && !string.IsNullOrWhiteSpace(metadata.ModelPath))
                return UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(metadata.ModelPath);
#endif

            return null;
        }

        private static UnityEngine.GameObject ResolveAssemblyPrefab(NovaPartVisualCatalog.Entry metadata)
        {
            return metadata?.AssemblyPrefab;
        }

        private static System.Collections.Generic.Dictionary<string, NovaPartAlignmentCatalog.Entry> BuildNovaAlignmentByPartId(
            NovaPartAlignmentCatalog novaPartAlignmentCatalog)
        {
            var byPartId = new System.Collections.Generic.Dictionary<string, NovaPartAlignmentCatalog.Entry>();
            if (novaPartAlignmentCatalog == null)
                return byPartId;

            for (int i = 0; i < novaPartAlignmentCatalog.Entries.Count; i++)
            {
                var entry = novaPartAlignmentCatalog.Entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.PartId))
                    continue;

                byPartId[entry.PartId] = entry;
                var legacyPartId = ResolveLegacySamplePartId(entry.PartId);
                if (!string.IsNullOrWhiteSpace(legacyPartId))
                    byPartId[legacyPartId] = entry;
            }

            return byPartId;
        }

        private static string ResolveLegacySamplePartId(string novaPartId)
        {
            return novaPartId switch
            {
                "nova_frame_body25_bosro" => "frame_bastion",
                "nova_frame_body1_sz" => "frame_striker",
                "nova_frame_body11_kn" => "frame_relay",
                "nova_fire_arm10_broz" => "fire_scatter",
                "nova_fire_arm1_sz" => "fire_pulse",
                "nova_fire_arm13_prs" => "fire_rail",
                "nova_mob_g_legs35_prg" => "mob_burst",
                "nova_mob_legs1_rdrn" => "mob_vector",
                "nova_mob_legs19_tower" => "mob_treads",
                _ => null
            };
        }

        private static GaragePanelCatalog.PartAlignment CreateAlignment(NovaPartAlignmentCatalog.Entry entry)
        {
            if (entry == null)
                return null;

            return new GaragePanelCatalog.PartAlignment
            {
                NormalizedScale = entry.NormalizedScale,
                PivotOffset = entry.PivotOffset,
                HasVisualBounds = entry.HasVisualBounds,
                VisualBoundsCenter = entry.VisualBoundsCenter,
                VisualBoundsMin = entry.VisualBoundsMin,
                VisualBoundsMax = entry.VisualBoundsMax,
                SocketOffset = entry.SocketOffset,
                SocketEuler = entry.SocketEuler,
                HasGxTreeSocket = entry.HasGxTreeSocket,
                GxTreeSocketOffset = entry.GxTreeSocketOffset,
                GxTreeSocketName = entry.GxTreeSocketName,
                HasXfiMetadata = entry.HasXfiMetadata,
                XfiPath = entry.XfiPath,
                XfiHeader = entry.XfiHeader,
                XfiHeaderKind = entry.XfiHeaderKind,
                XfiAttachSlot = entry.XfiAttachSlot,
                XfiAttachVariant = entry.XfiAttachVariant,
                XfiTransformCount = entry.XfiTransformCount,
                XfiTransformTranslations = entry.XfiTransformTranslations,
                XfiDirectionRangeCount = entry.XfiDirectionRangeCount,
                XfiDirectionRanges = entry.XfiDirectionRanges,
                HasXfiAttachSocket = entry.HasXfiAttachSocket,
                XfiAttachSocketOffset = entry.XfiAttachSocketOffset,
                HasFrameTopSocket = entry.HasFrameTopSocket,
                FrameTopSocketOffset = entry.FrameTopSocketOffset,
                XfiSocketQuality = entry.XfiSocketQuality,
                XfiSocketName = entry.XfiSocketName,
                QualityFlag = entry.QualityFlag,
                ReviewReason = entry.ReviewReason,
                AssemblySourceSlotCode = entry.AssemblySourceSlotCode,
                AssemblySlotMode = entry.AssemblySlotMode,
                AssemblyAnchorMode = entry.AssemblyAnchorMode,
                AssemblyLocalOffset = entry.AssemblyLocalOffset,
                AssemblyLocalEuler = entry.AssemblyLocalEuler,
                AssemblyLocalScale = entry.AssemblyLocalScale,
                AssemblyConfidence = entry.AssemblyConfidence,
                AssemblyEvidencePath = entry.AssemblyEvidencePath,
                AssemblyReviewResult = entry.AssemblyReviewResult
            };
        }
    }
}
