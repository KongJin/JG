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
            string validationText,
            bool isLoading = false,
            bool isSaving = false,
            string operationName = null)
        {
            RenderStatus = renderStatus ?? string.Empty;
            SelectedSlotIndex = selectedSlotIndex;
            FocusedPart = focusedPart;
            PartSearchText = partSearchText ?? string.Empty;
            IsSettingsOpen = isSettingsOpen;
            HasDraftChanges = hasDraftChanges;
            CanSave = canSave;
            ValidationText = validationText ?? string.Empty;
            IsLoading = isLoading;
            IsSaving = isSaving;
            OperationName = operationName ?? string.Empty;
        }

        public string RenderStatus { get; }
        public int SelectedSlotIndex { get; }
        public GarageEditorFocus FocusedPart { get; }
        public string PartSearchText { get; }
        public bool IsSettingsOpen { get; }
        public bool HasDraftChanges { get; }
        public bool CanSave { get; }
        public string ValidationText { get; }
        public bool IsLoading { get; }
        public bool IsSaving { get; }
        public string OperationName { get; }

        /// <summary>
        /// 빈 스냅샷 인스턴스
        /// </summary>
        public static GarageSetBUitkPageSnapshot Empty => new(
            renderStatus: "empty",
            selectedSlotIndex: 0,
            focusedPart: GarageEditorFocus.Mobility,
            partSearchText: string.Empty,
            isSettingsOpen: false,
            hasDraftChanges: false,
            canSave: false,
            validationText: string.Empty);
    }
}
