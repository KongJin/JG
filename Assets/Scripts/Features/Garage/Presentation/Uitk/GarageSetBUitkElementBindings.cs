using Shared.Ui;
using UnityEngine.UIElements;

namespace Features.Garage.Presentation
{
    internal sealed class GarageSetBUitkElementBindings
    {
        public VisualElement SurfaceRoot { get; private set; }
        public VisualElement ScreenRoot { get; private set; }
        public VisualElement HostScreenRoot { get; private set; }

        // Layout Elements
        public VisualElement WorkspaceScroll { get; private set; }
        public VisualElement SlotStrip { get; private set; }
        public VisualElement PartFocusBar { get; private set; }
        public VisualElement PartSelectionPane { get; private set; }
        public VisualElement PartListCard { get; private set; }
        public VisualElement PreviewCard { get; private set; }
        public VisualElement SelectedPartPreviewCard { get; private set; }
        public VisualElement SelectedPartPreviewHost { get; private set; }
        public ScrollView PartListRowsScroll { get; private set; }

        // Status Bar
        public Label CommandStatusLabel { get; private set; }

        // Buttons
        public Button SettingsButton { get; private set; }
        public Button SaveButton { get; private set; }

        // Preview
        public VisualElement UnitPreviewHost { get; private set; }
        public Label PreviewTitleLabel { get; private set; }
        public VisualElement PreviewTagRow { get; private set; }
        public Label UnitPreviewLabel { get; private set; }
        public VisualElement PreviewPowerBar { get; private set; }
        public VisualElement PreviewPowerFill { get; private set; }
        public Label PreviewPowerLabel { get; private set; }

        // Save Dock
        public VisualElement SaveDock { get; private set; }
        public Label SaveValidationLabel { get; private set; }

        public bool TryBind(VisualElement root)
        {
            if (root == null)
                return false;

            SurfaceRoot = root;
            ScreenRoot = root.Q<VisualElement>("GarageSetBScreen") ?? root;

            WorkspaceScroll = UitkElementUtility.Required<VisualElement>(root, "WorkspaceScroll");
            SlotStrip = UitkElementUtility.Required<VisualElement>(root, "SlotStrip");
            PartFocusBar = UitkElementUtility.Required<VisualElement>(root, "PartFocusBar");
            PartSelectionPane = UitkElementUtility.Required<VisualElement>(root, "PartSelectionPane");
            PartListCard = UitkElementUtility.Required<VisualElement>(root, "PartListCard");
            PreviewCard = UitkElementUtility.Required<VisualElement>(root, "PreviewCard");
            SelectedPartPreviewCard = UitkElementUtility.Required<VisualElement>(root, "SelectedPartPreviewCard");
            SelectedPartPreviewHost = UitkElementUtility.Required<VisualElement>(root, "SelectedPartPreviewHost");
            PartListRowsScroll = UitkElementUtility.Required<ScrollView>(root, "PartListRows");
            CommandStatusLabel = UitkElementUtility.Required<Label>(root, "CommandStatusLabel");
            SettingsButton = UitkElementUtility.Required<Button>(root, "SettingsButton");
            UnitPreviewHost = UitkElementUtility.Required<VisualElement>(root, "UnitPreviewHost");
            PreviewTitleLabel = UitkElementUtility.Required<Label>(root, "PreviewTitleLabel");
            PreviewTagRow = root.Q<VisualElement>("PreviewTagRow");
            UnitPreviewLabel = UitkElementUtility.Required<Label>(root, "UnitPreviewLabel");
            PreviewPowerBar = UitkElementUtility.Required<VisualElement>(root, "PreviewPowerBar");
            PreviewPowerFill = UitkElementUtility.Required<VisualElement>(root, "PreviewPowerFill");
            PreviewPowerLabel = UitkElementUtility.Required<Label>(root, "PreviewPowerLabel");
            SaveDock = UitkElementUtility.Required<VisualElement>(root, "SaveDock");
            SaveValidationLabel = UitkElementUtility.Required<Label>(root, "SaveValidationLabel");
            SaveButton = UitkElementUtility.Required<Button>(root, "SaveButton");

            return true;
        }

        public void Clear()
        {
            SurfaceRoot = null;
            ScreenRoot = null;
            HostScreenRoot = null;
            WorkspaceScroll = null;
            SlotStrip = null;
            PartFocusBar = null;
            PartSelectionPane = null;
            PartListCard = null;
            PreviewCard = null;
            SelectedPartPreviewCard = null;
            SelectedPartPreviewHost = null;
            PartListRowsScroll = null;
            CommandStatusLabel = null;
            SettingsButton = null;
            SaveButton = null;
            UnitPreviewHost = null;
            PreviewTitleLabel = null;
            PreviewTagRow = null;
            UnitPreviewLabel = null;
            PreviewPowerBar = null;
            PreviewPowerFill = null;
            PreviewPowerLabel = null;
            SaveDock = null;
            SaveValidationLabel = null;
        }

        public bool IsBound => SurfaceRoot != null;
    }
}
