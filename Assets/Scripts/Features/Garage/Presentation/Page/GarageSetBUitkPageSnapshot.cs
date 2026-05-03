namespace Features.Garage.Presentation
{
    public readonly struct GarageSetBUitkPageSnapshot
    {
        public GarageSetBUitkPageSnapshot(
            string renderStatus,
            int selectedSlotIndex,
            GarageEditorFocus focusedPart,
            string partSearchText,
            bool isSettingsOpen,
            bool hasDraftChanges,
            bool canSave,
            string validationText)
        {
            RenderStatus = renderStatus ?? string.Empty;
            SelectedSlotIndex = selectedSlotIndex;
            FocusedPart = focusedPart;
            PartSearchText = partSearchText ?? string.Empty;
            IsSettingsOpen = isSettingsOpen;
            HasDraftChanges = hasDraftChanges;
            CanSave = canSave;
            ValidationText = validationText ?? string.Empty;
        }

        public string RenderStatus { get; }
        public int SelectedSlotIndex { get; }
        public GarageEditorFocus FocusedPart { get; }
        public string PartSearchText { get; }
        public bool IsSettingsOpen { get; }
        public bool HasDraftChanges { get; }
        public bool CanSave { get; }
        public string ValidationText { get; }
    }
}
