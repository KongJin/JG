using Features.Unit.Domain;
using UnityEngine;

namespace Features.Garage.Presentation
{
    public sealed class GarageSlotViewModel
    {
        public GarageSlotViewModel(
            string slotLabel,
            string title,
            string summary,
            string statusBadgeText,
            bool hasCommittedLoadout,
            bool hasDraftChanges,
            bool isEmpty,
            bool isSelected,
            bool showArrow = false,
            string callsign = null,
            string roleLabel = null,
            string serviceTagText = null,
            string loadoutKey = null,
            string frameId = null,
            string firepowerId = null,
            string mobilityId = null,
            GameObject framePreviewPrefab = null,
            GameObject firepowerPreviewPrefab = null,
            GameObject mobilityPreviewPrefab = null,
            GaragePanelCatalog.PartAlignment frameAlignment = null,
            GaragePanelCatalog.PartAlignment firepowerAlignment = null,
            GaragePanelCatalog.PartAlignment mobilityAlignment = null,
            bool mobilityUsesAssemblyPivot = false,
            AssemblyForm frameAssemblyForm = AssemblyForm.Unspecified,
            AssemblyForm firepowerAssemblyForm = AssemblyForm.Unspecified)
        {
            SlotLabel = slotLabel;
            Title = title;
            Summary = summary;
            StatusBadgeText = statusBadgeText;
            HasCommittedLoadout = hasCommittedLoadout;
            HasDraftChanges = hasDraftChanges;
            IsEmpty = isEmpty;
            IsSelected = isSelected;
            ShowArrow = showArrow;
            Callsign = callsign;
            RoleLabel = roleLabel;
            ServiceTagText = serviceTagText;
            LoadoutKey = loadoutKey;
            FrameId = frameId;
            FirepowerId = firepowerId;
            MobilityId = mobilityId;
            FramePreviewPrefab = framePreviewPrefab;
            FirepowerPreviewPrefab = firepowerPreviewPrefab;
            MobilityPreviewPrefab = mobilityPreviewPrefab;
            FrameAlignment = frameAlignment;
            FirepowerAlignment = firepowerAlignment;
            MobilityAlignment = mobilityAlignment;
            MobilityUsesAssemblyPivot = mobilityUsesAssemblyPivot;
            FrameAssemblyForm = frameAssemblyForm;
            FirepowerAssemblyForm = firepowerAssemblyForm;
        }

        public string SlotLabel { get; }
        public string Title { get; }
        public string Summary { get; }
        public string StatusBadgeText { get; }
        public bool HasCommittedLoadout { get; }
        public bool HasDraftChanges { get; }
        public bool IsEmpty { get; }
        public bool IsSelected { get; }
        public bool ShowArrow { get; }
        public string Callsign { get; }
        public string RoleLabel { get; }
        public string ServiceTagText { get; }
        public string LoadoutKey { get; }
        public string FrameId { get; }
        public string FirepowerId { get; }
        public string MobilityId { get; }
        public GameObject FramePreviewPrefab { get; }
        public GameObject FirepowerPreviewPrefab { get; }
        public GameObject MobilityPreviewPrefab { get; }
        public GaragePanelCatalog.PartAlignment FrameAlignment { get; }
        public GaragePanelCatalog.PartAlignment FirepowerAlignment { get; }
        public GaragePanelCatalog.PartAlignment MobilityAlignment { get; }
        public bool MobilityUsesAssemblyPivot { get; }
        public AssemblyForm FrameAssemblyForm { get; }
        public AssemblyForm FirepowerAssemblyForm { get; }
    }

    public sealed class GarageEditorViewModel
    {
        public GarageEditorViewModel(
            string title,
            string subtitle,
            string frameValueText,
            string frameHintText,
            string firepowerValueText,
            string firepowerHintText,
            string mobilityValueText,
            string mobilityHintText,
            bool isClearInteractable)
        {
            Title = title;
            Subtitle = subtitle;
            FrameValueText = frameValueText;
            FrameHintText = frameHintText;
            FirepowerValueText = firepowerValueText;
            FirepowerHintText = firepowerHintText;
            MobilityValueText = mobilityValueText;
            MobilityHintText = mobilityHintText;
            IsClearInteractable = isClearInteractable;
        }

        public string Title { get; }
        public string Subtitle { get; }
        public string FrameValueText { get; }
        public string FrameHintText { get; }
        public string FirepowerValueText { get; }
        public string FirepowerHintText { get; }
        public string MobilityValueText { get; }
        public string MobilityHintText { get; }
        public bool IsClearInteractable { get; }
    }

    public sealed class GarageResultViewModel
    {
        public GarageResultViewModel(
            string rosterStatusText,
            string validationText,
            string statsText,
            bool isReady,
            bool isDirty,
            bool canSave,
            string primaryActionLabel,
            GarageStatRadarViewModel radar = null)
        {
            RosterStatusText = rosterStatusText;
            ValidationText = validationText;
            StatsText = statsText;
            IsReady = isReady;
            IsDirty = isDirty;
            CanSave = canSave;
            PrimaryActionLabel = primaryActionLabel;
            Radar = radar;
        }

        public string RosterStatusText { get; }
        public string ValidationText { get; }
        public string StatsText { get; }
        public bool IsReady { get; }
        public bool IsDirty { get; }
        public bool CanSave { get; }
        public string PrimaryActionLabel { get; }
        public GarageStatRadarViewModel Radar { get; }
    }

    public sealed class GarageStatRadarViewModel
    {
        public static readonly string[] AxisLabels =
        {
            "공격력",
            "공격속도",
            "사거리",
            "HP",
            "방어",
            "이동속도",
            "이동범위"
        };

        public GarageStatRadarViewModel(
            float[] currentValues,
            float[] previousValues,
            int summonCost)
        {
            CurrentValues = currentValues ?? new float[AxisLabels.Length];
            PreviousValues = previousValues;
            SummonCost = summonCost;
        }

        public float[] CurrentValues { get; }
        public float[] PreviousValues { get; }
        public int SummonCost { get; }
        public bool HasPrevious => PreviousValues != null && PreviousValues.Length == AxisLabels.Length;
    }
}
