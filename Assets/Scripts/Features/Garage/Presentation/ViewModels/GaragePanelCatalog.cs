using System.Collections.Generic;
using Features.Unit.Domain;
using UnityEngine;

namespace Features.Garage.Presentation
{
    public sealed class GaragePanelCatalog
    {
        public sealed class StatRadarScale
        {
            public float AttackDamageMax { get; set; } = 60f;
            public float AttackSpeedMax { get; set; } = 1.5f;
            public float RangeMax { get; set; } = 8f;
            public float HpMax { get; set; } = 700f;
            public float DefenseMax { get; set; } = 8f;
            public float MoveSpeedMax { get; set; } = 5f;
            public float MoveRangeMax { get; set; } = 6f;
        }

        public sealed class PartAlignment
        {
            public float NormalizedScale { get; set; }
            public Vector3 PivotOffset { get; set; }
            public bool HasVisualBounds { get; set; }
            public Vector3 VisualBoundsCenter { get; set; }
            public Vector3 VisualBoundsMin { get; set; }
            public Vector3 VisualBoundsMax { get; set; }
            public Vector3 SocketOffset { get; set; }
            public Vector3 SocketEuler { get; set; }
            public bool HasGxTreeSocket { get; set; }
            public Vector3 GxTreeSocketOffset { get; set; }
            public string GxTreeSocketName { get; set; }
            public bool HasXfiMetadata { get; set; }
            public string XfiPath { get; set; }
            public string XfiHeader { get; set; }
            public string XfiHeaderKind { get; set; }
            public string XfiAttachSlot { get; set; }
            public string XfiAttachVariant { get; set; }
            public int XfiTransformCount { get; set; }
            public string XfiTransformTranslations { get; set; }
            public int XfiDirectionRangeCount { get; set; }
            public string XfiDirectionRanges { get; set; }
            public bool HasXfiAttachSocket { get; set; }
            public Vector3 XfiAttachSocketOffset { get; set; }
            public bool HasFrameTopSocket { get; set; }
            public Vector3 FrameTopSocketOffset { get; set; }
            public string XfiSocketQuality { get; set; }
            public string XfiSocketName { get; set; }
            public string QualityFlag { get; set; }
            public string ReviewReason { get; set; }
            public string AssemblySourceSlotCode { get; set; }
            public string AssemblySlotMode { get; set; }
            public string AssemblyAnchorMode { get; set; }
            public Vector3 AssemblyLocalOffset { get; set; }
            public Vector3 AssemblyLocalEuler { get; set; }
            public Vector3 AssemblyLocalScale { get; set; }
            public string AssemblyConfidence { get; set; }
            public string AssemblyEvidencePath { get; set; }
            public string AssemblyReviewResult { get; set; }

            public bool CanApply => QualityFlag == "auto_ok";
        }

        public sealed class FrameOption
        {
            public string Id { get; set; }
            public string DisplayName { get; set; }
            public float BaseHp { get; set; }
            public float Defense { get; set; }
            public int EnergyCost { get; set; }
            public float BaseAttackSpeed { get; set; }
            public AssemblyForm AssemblyForm { get; set; }
            public GameObject PreviewPrefab { get; set; }
            public GameObject AssemblyPrefab { get; set; }
            public bool UseAssemblyPivot { get; set; }
            public string SourcePath { get; set; }
            public int Tier { get; set; }
            public bool NeedsNameReview { get; set; }
            public PartAlignment Alignment { get; set; }
        }

        public sealed class FirepowerOption
        {
            public string Id { get; set; }
            public string DisplayName { get; set; }
            public float AttackDamage { get; set; }
            public float AttackSpeed { get; set; }
            public float Range { get; set; }
            public int EnergyCost { get; set; }
            public AssemblyForm AssemblyForm { get; set; }
            public GameObject PreviewPrefab { get; set; }
            public GameObject AssemblyPrefab { get; set; }
            public string SourcePath { get; set; }
            public int Tier { get; set; }
            public bool NeedsNameReview { get; set; }
            public PartAlignment Alignment { get; set; }
        }

        public sealed class MobilityOption
        {
            public string Id { get; set; }
            public string DisplayName { get; set; }
            public float MoveSpeed { get; set; }
            public float MoveRange { get; set; }
            public int EnergyCost { get; set; }
            public float HpBonus { get; set; }
            public float AnchorRange
            {
                get => MoveRange;
                set
                {
                    if (MoveRange == 0f)
                        MoveRange = value;
                }
            }
            public GameObject PreviewPrefab { get; set; }
            public GameObject AssemblyPrefab { get; set; }
            public bool UseAssemblyPivot { get; set; }
            public string SourcePath { get; set; }
            public int Tier { get; set; }
            public bool NeedsNameReview { get; set; }
            public PartAlignment Alignment { get; set; }
        }

        private readonly Dictionary<string, FrameOption> _framesById;
        private readonly Dictionary<string, FirepowerOption> _firepowerById;
        private readonly Dictionary<string, MobilityOption> _mobilityById;

        public GaragePanelCatalog(
            IReadOnlyList<FrameOption> frames,
            IReadOnlyList<FirepowerOption> firepower,
            IReadOnlyList<MobilityOption> mobility,
            StatRadarScale radarScale = null)
        {
            Frames = frames ?? new List<FrameOption>();
            Firepower = firepower ?? new List<FirepowerOption>();
            Mobility = mobility ?? new List<MobilityOption>();
            RadarScale = radarScale ?? new StatRadarScale();

            _framesById = new Dictionary<string, FrameOption>();
            _firepowerById = new Dictionary<string, FirepowerOption>();
            _mobilityById = new Dictionary<string, MobilityOption>();

            for (int i = 0; i < Frames.Count; i++)
                _framesById[Frames[i].Id] = Frames[i];
            for (int i = 0; i < Firepower.Count; i++)
                _firepowerById[Firepower[i].Id] = Firepower[i];
            for (int i = 0; i < Mobility.Count; i++)
                _mobilityById[Mobility[i].Id] = Mobility[i];
        }

        public IReadOnlyList<FrameOption> Frames { get; }
        public IReadOnlyList<FirepowerOption> Firepower { get; }
        public IReadOnlyList<MobilityOption> Mobility { get; }
        public StatRadarScale RadarScale { get; }

        public FrameOption FindFrame(string id) =>
            !string.IsNullOrWhiteSpace(id) && _framesById.TryGetValue(id, out var value) ? value : null;

        public FirepowerOption FindFirepower(string id) =>
            !string.IsNullOrWhiteSpace(id) && _firepowerById.TryGetValue(id, out var value) ? value : null;

        public MobilityOption FindMobility(string id) =>
            !string.IsNullOrWhiteSpace(id) && _mobilityById.TryGetValue(id, out var value) ? value : null;
    }
}
