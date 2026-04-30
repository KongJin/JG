namespace Features.Garage.Presentation
{
    public readonly struct GarageSetBUitkPageSnapshot
    {
        public GarageSetBUitkPageSnapshot(
            string renderStatus,
            int selectedSlotIndex,
            GarageEditorFocus focusedPart,
            string partSearchText,
            bool isSettingsOpen)
        {
            RenderStatus = renderStatus ?? string.Empty;
            SelectedSlotIndex = selectedSlotIndex;
            FocusedPart = focusedPart;
            PartSearchText = partSearchText ?? string.Empty;
            IsSettingsOpen = isSettingsOpen;
        }

        public string RenderStatus { get; }
        public int SelectedSlotIndex { get; }
        public GarageEditorFocus FocusedPart { get; }
        public string PartSearchText { get; }
        public bool IsSettingsOpen { get; }
    }
}
