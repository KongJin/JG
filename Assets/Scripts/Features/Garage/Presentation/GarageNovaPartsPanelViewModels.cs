using System.Collections.Generic;

namespace Features.Garage.Presentation
{
    public enum GarageNovaPartPanelSlot
    {
        Frame,
        Firepower,
        Mobility,
    }

    public readonly struct GarageNovaPartSelection
    {
        public GarageNovaPartSelection(GarageNovaPartPanelSlot slot, string partId)
        {
            Slot = slot;
            PartId = partId;
        }

        public GarageNovaPartPanelSlot Slot { get; }
        public string PartId { get; }
    }

    public readonly struct GarageNovaPartsDraftSelection
    {
        public GarageNovaPartsDraftSelection(string frameId, string firepowerId, string mobilityId)
        {
            FrameId = frameId;
            FirepowerId = firepowerId;
            MobilityId = mobilityId;
        }

        public string FrameId { get; }
        public string FirepowerId { get; }
        public string MobilityId { get; }
    }

    public sealed class GarageNovaPartOptionViewModel
    {
        public GarageNovaPartOptionViewModel(
            GarageNovaPartPanelSlot slot,
            string id,
            string displayName,
            string detailText,
            string sourcePath,
            bool isSelected,
            bool needsNameReview)
        {
            Slot = slot;
            Id = id;
            DisplayName = displayName;
            DetailText = detailText;
            SourcePath = sourcePath;
            IsSelected = isSelected;
            NeedsNameReview = needsNameReview;
        }

        public GarageNovaPartPanelSlot Slot { get; }
        public string Id { get; }
        public string DisplayName { get; }
        public string DetailText { get; }
        public string SourcePath { get; }
        public bool IsSelected { get; }
        public bool NeedsNameReview { get; }
    }

    public sealed class GarageNovaPartsPanelViewModel
    {
        public GarageNovaPartsPanelViewModel(
            GarageNovaPartPanelSlot activeSlot,
            string searchText,
            string countText,
            string selectedNameText,
            string selectedDetailText,
            bool canApply,
            IReadOnlyList<GarageNovaPartOptionViewModel> options)
        {
            ActiveSlot = activeSlot;
            SearchText = searchText;
            CountText = countText;
            SelectedNameText = selectedNameText;
            SelectedDetailText = selectedDetailText;
            CanApply = canApply;
            Options = options;
        }

        public GarageNovaPartPanelSlot ActiveSlot { get; }
        public string SearchText { get; }
        public string CountText { get; }
        public string SelectedNameText { get; }
        public string SelectedDetailText { get; }
        public bool CanApply { get; }
        public IReadOnlyList<GarageNovaPartOptionViewModel> Options { get; }
    }
}
