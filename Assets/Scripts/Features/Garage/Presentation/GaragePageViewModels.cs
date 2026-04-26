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
            GameObject mobilityPreviewPrefab = null)
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
            string primaryActionLabel)
        {
            RosterStatusText = rosterStatusText;
            ValidationText = validationText;
            StatsText = statsText;
            IsReady = isReady;
            IsDirty = isDirty;
            CanSave = canSave;
            PrimaryActionLabel = primaryActionLabel;
        }

        public string RosterStatusText { get; }
        public string ValidationText { get; }
        public string StatsText { get; }
        public bool IsReady { get; }
        public bool IsDirty { get; }
        public bool CanSave { get; }
        public string PrimaryActionLabel { get; }
    }
}
