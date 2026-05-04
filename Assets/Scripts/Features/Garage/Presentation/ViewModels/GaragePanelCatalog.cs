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
            public PartVisualBounds VisualBounds { get; } = new();
            public PartSocketAlignment Socket { get; } = new();
            public PartXfiMetadata Xfi { get; } = new();
            public PartAssemblyAlignment Assembly { get; } = new();

            public float NormalizedScale { get => VisualBounds.NormalizedScale; set => VisualBounds.NormalizedScale = value; }
            public Vector3 PivotOffset { get => VisualBounds.PivotOffset; set => VisualBounds.PivotOffset = value; }
            public bool HasVisualBounds { get => VisualBounds.HasBounds; set => VisualBounds.HasBounds = value; }
            public Vector3 VisualBoundsCenter { get => VisualBounds.Center; set => VisualBounds.Center = value; }
            public Vector3 VisualBoundsMin { get => VisualBounds.Min; set => VisualBounds.Min = value; }
            public Vector3 VisualBoundsMax { get => VisualBounds.Max; set => VisualBounds.Max = value; }
            public Vector3 SocketOffset { get => Socket.Offset; set => Socket.Offset = value; }
            public Vector3 SocketEuler { get => Socket.Euler; set => Socket.Euler = value; }
            public bool HasGxTreeSocket { get => Socket.HasGxTreeSocket; set => Socket.HasGxTreeSocket = value; }
            public Vector3 GxTreeSocketOffset { get => Socket.GxTreeSocketOffset; set => Socket.GxTreeSocketOffset = value; }
            public string GxTreeSocketName { get => Socket.GxTreeSocketName; set => Socket.GxTreeSocketName = value; }
            public bool HasXfiMetadata { get => Xfi.HasMetadata; set => Xfi.HasMetadata = value; }
            public string XfiPath { get => Xfi.Path; set => Xfi.Path = value; }
            public string XfiHeader { get => Xfi.Header; set => Xfi.Header = value; }
            public string XfiHeaderKind { get => Xfi.HeaderKind; set => Xfi.HeaderKind = value; }
            public string XfiAttachSlot { get => Xfi.AttachSlot; set => Xfi.AttachSlot = value; }
            public string XfiAttachVariant { get => Xfi.AttachVariant; set => Xfi.AttachVariant = value; }
            public int XfiTransformCount { get => Xfi.TransformCount; set => Xfi.TransformCount = value; }
            public string XfiTransformTranslations { get => Xfi.TransformTranslations; set => Xfi.TransformTranslations = value; }
            public int XfiDirectionRangeCount { get => Xfi.DirectionRangeCount; set => Xfi.DirectionRangeCount = value; }
            public string XfiDirectionRanges { get => Xfi.DirectionRanges; set => Xfi.DirectionRanges = value; }
            public bool HasXfiAttachSocket { get => Xfi.HasAttachSocket; set => Xfi.HasAttachSocket = value; }
            public Vector3 XfiAttachSocketOffset { get => Xfi.AttachSocketOffset; set => Xfi.AttachSocketOffset = value; }
            public bool HasFrameTopSocket { get => Socket.HasFrameTopSocket; set => Socket.HasFrameTopSocket = value; }
            public Vector3 FrameTopSocketOffset { get => Socket.FrameTopSocketOffset; set => Socket.FrameTopSocketOffset = value; }
            public string XfiSocketQuality { get => Xfi.SocketQuality; set => Xfi.SocketQuality = value; }
            public string XfiSocketName { get => Xfi.SocketName; set => Xfi.SocketName = value; }
            public string QualityFlag { get => Assembly.QualityFlag; set => Assembly.QualityFlag = value; }
            public string ReviewReason { get => Assembly.ReviewReason; set => Assembly.ReviewReason = value; }
            public string AssemblySourceSlotCode { get => Assembly.SourceSlotCode; set => Assembly.SourceSlotCode = value; }
            public string AssemblySlotMode { get => Assembly.SlotMode; set => Assembly.SlotMode = value; }
            public string AssemblyAnchorMode { get => Assembly.AnchorMode; set => Assembly.AnchorMode = value; }
            public Vector3 AssemblyLocalOffset { get => Assembly.LocalOffset; set => Assembly.LocalOffset = value; }
            public Vector3 AssemblyLocalEuler { get => Assembly.LocalEuler; set => Assembly.LocalEuler = value; }
            public Vector3 AssemblyLocalScale { get => Assembly.LocalScale; set => Assembly.LocalScale = value; }
            public string AssemblyConfidence { get => Assembly.Confidence; set => Assembly.Confidence = value; }
            public string AssemblyEvidencePath { get => Assembly.EvidencePath; set => Assembly.EvidencePath = value; }
            public string AssemblyReviewResult { get => Assembly.ReviewResult; set => Assembly.ReviewResult = value; }

            public bool CanApply => QualityFlag == "auto_ok";
        }

        public sealed class PartVisualBounds
        {
            public float NormalizedScale { get; set; }
            public Vector3 PivotOffset { get; set; }
            public bool HasBounds { get; set; }
            public Vector3 Center { get; set; }
            public Vector3 Min { get; set; }
            public Vector3 Max { get; set; }
        }

        public sealed class PartSocketAlignment
        {
            public Vector3 Offset { get; set; }
            public Vector3 Euler { get; set; }
            public bool HasGxTreeSocket { get; set; }
            public Vector3 GxTreeSocketOffset { get; set; }
            public string GxTreeSocketName { get; set; }
            public bool HasFrameTopSocket { get; set; }
            public Vector3 FrameTopSocketOffset { get; set; }
        }

        public sealed class PartXfiMetadata
        {
            public bool HasMetadata { get; set; }
            public string Path { get; set; }
            public string Header { get; set; }
            public string HeaderKind { get; set; }
            public string AttachSlot { get; set; }
            public string AttachVariant { get; set; }
            public int TransformCount { get; set; }
            public string TransformTranslations { get; set; }
            public int DirectionRangeCount { get; set; }
            public string DirectionRanges { get; set; }
            public bool HasAttachSocket { get; set; }
            public Vector3 AttachSocketOffset { get; set; }
            public string SocketQuality { get; set; }
            public string SocketName { get; set; }
        }

        public sealed class PartAssemblyAlignment
        {
            public string QualityFlag { get; set; }
            public string ReviewReason { get; set; }
            public string SourceSlotCode { get; set; }
            public string SlotMode { get; set; }
            public string AnchorMode { get; set; }
            public Vector3 LocalOffset { get; set; }
            public Vector3 LocalEuler { get; set; }
            public Vector3 LocalScale { get; set; }
            public string Confidence { get; set; }
            public string EvidencePath { get; set; }
            public string ReviewResult { get; set; }
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
