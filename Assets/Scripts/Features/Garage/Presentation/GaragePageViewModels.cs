namespace Features.Garage.Presentation
{
    public sealed class GarageSlotViewModel
    {
        public GarageSlotViewModel(
            string slotLabel,
            string title,
            string summary,
            bool hasCommittedLoadout,
            bool isSelected,
            bool showArrow = false,
            string frameId = null,
            string firepowerId = null,
            string mobilityId = null)
        {
            SlotLabel = slotLabel;
            Title = title;
            Summary = summary;
            HasCommittedLoadout = hasCommittedLoadout;
            IsSelected = isSelected;
            ShowArrow = showArrow;
            FrameId = frameId;
            FirepowerId = firepowerId;
            MobilityId = mobilityId;
        }

        public string SlotLabel { get; }
        public string Title { get; }
        public string Summary { get; }
        public bool HasCommittedLoadout { get; }
        public bool IsSelected { get; }
        public bool ShowArrow { get; }
        public string FrameId { get; }
        public string FirepowerId { get; }
        public string MobilityId { get; }
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
            string statsText)
        {
            RosterStatusText = rosterStatusText;
            ValidationText = validationText;
            StatsText = statsText;
        }

        public string RosterStatusText { get; }
        public string ValidationText { get; }
        public string StatsText { get; }
    }
}
