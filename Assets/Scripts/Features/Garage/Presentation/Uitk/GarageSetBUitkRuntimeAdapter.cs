using System;
using System.Collections.Generic;
using Shared.Runtime;
using Shared.Ui;
using UnityEngine;
using UnityEngine.UIElements;

namespace Features.Garage.Presentation
{
    public sealed class GarageSetBUitkRuntimeAdapter : MonoBehaviour
    {
        [SerializeField]
        private UIDocument _document;

        [SerializeField]
        private GarageSetBUitkPreviewRenderer _previewRenderer;

        [SerializeField]
        private GarageSetBUitkPreviewRenderer _partPreviewRenderer;

        private VisualElement _surfaceRoot;
        private VisualElement _screenRoot;
        private VisualElement _hostScreenRoot;
        private VisualElement _workspaceScroll;
        private VisualElement _slotStrip;
        private VisualElement _partFocusBar;
        private VisualElement _partSelectionPane;
        private VisualElement _partListCard;
        private VisualElement _previewCard;
        private VisualElement _selectedPartPreviewCard;
        private VisualElement _selectedPartPreviewHost;
        private ScrollView _partListRowsScroll;
        private Label _commandStatusLabel;
        private Button _settingsButton;
        private GarageSetBSlotSurface _slotSurface;
        private GarageSetBPartListSurface _partListSurface;
        private VisualElement _unitPreviewHost;
        private VisualElement _previewPowerBar;
        private VisualElement _previewPowerFill;
        private Label _previewPowerLabel;
        private GarageStatRadarElement _statRadar;
        private Button _saveButton;
        private Label _saveValidationLabel;
        private GarageSetBUitkLayoutController _layoutController;
        private GarageSetBUitkPreviewController _previewController;
        private bool _isHostBound;
        private IReadOnlyList<GarageSlotViewModel> _lastSlots;
        private GarageNovaPartsPanelViewModel _lastPartList;
        private GarageResultViewModel _lastResult;
        private GarageEditorFocus _lastFocusedPart;
        private bool _lastIsSaving;
        private bool _hasLastRender;

        public event Action<int> SlotSelected;
        public event Action<GarageEditorFocus> PartFocusSelected;
        public event Action<string> PartSearchChanged;
        public event Action<GarageNovaPartSelection> PartOptionSelected;
        public event Action SaveRequested;
        public event Action SettingsRequested;

        public bool Bind()
        {
            if (_surfaceRoot != null && _slotSurface != null && _partListSurface != null)
                return true;

            if (_document == null)
                return false;

            var root = _document.rootVisualElement;
            if (root == null)
                return false;

            _isHostBound = false;
            _hostScreenRoot = null;
            return Bind(root);
        }

        public bool BindToHost(VisualElement host)
        {
            if (host == null)
                return false;

            if (host.Q<VisualElement>("GarageSetBScreen") == null)
            {
                var source = _document != null ? _document.visualTreeAsset : null;
                if (source == null)
                    return false;

                host.Clear();
                source.CloneTree(host);
            }

            var screenRoot = host.Q<VisualElement>("GarageSetBScreen");
            if (screenRoot == null)
                return false;

            _isHostBound = true;
            _hostScreenRoot = screenRoot;
            _hostScreenRoot.style.display = DisplayStyle.Flex;
            HideStandaloneDocumentRoot();
            return Bind(host);
        }

        public bool SetDocumentRootVisible(bool isVisible)
        {
            if (_isHostBound)
            {
                HideStandaloneDocumentRoot();
                if (_hostScreenRoot != null)
                    _hostScreenRoot.style.display = DisplayStyle.Flex;
                return true;
            }

            if (_document == null)
                return false;

            if (!_document.gameObject.activeSelf)
                _document.gameObject.SetActive(true);

            _document.sortingOrder = 10;
            var root = _document.rootVisualElement;
            if (root != null)
                root.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;

            return true;
        }

        private void HideStandaloneDocumentRoot()
        {
            if (_document == null)
                return;

            if (!_document.gameObject.activeSelf)
                _document.gameObject.SetActive(true);

            _document.sortingOrder = 10;
            var root = _document.rootVisualElement;
            if (root != null)
                root.style.display = DisplayStyle.None;
        }

        private bool Bind(VisualElement root)
        {
            if (root == null)
                return false;

            if (_surfaceRoot == root && _slotSurface != null && _partListSurface != null)
            {
                _layoutController?.Apply();
                return true;
            }

            _screenRoot = root.Q<VisualElement>("GarageSetBScreen") ?? root;
            _workspaceScroll = UitkElementUtility.Required<VisualElement>(root, "WorkspaceScroll");
            _slotStrip = UitkElementUtility.Required<VisualElement>(root, "SlotStrip");
            _partFocusBar = UitkElementUtility.Required<VisualElement>(root, "PartFocusBar");
            _partSelectionPane = UitkElementUtility.Required<VisualElement>(
                root,
                "PartSelectionPane"
            );
            _partListCard = UitkElementUtility.Required<VisualElement>(root, "PartListCard");
            _previewCard = UitkElementUtility.Required<VisualElement>(root, "PreviewCard");
            _selectedPartPreviewCard = UitkElementUtility.Required<VisualElement>(
                root,
                "SelectedPartPreviewCard"
            );
            _selectedPartPreviewHost = UitkElementUtility.Required<VisualElement>(
                root,
                "SelectedPartPreviewHost"
            );
            _partListRowsScroll = UitkElementUtility.Required<ScrollView>(root, "PartListRows");
            _commandStatusLabel = UitkElementUtility.Required<Label>(root, "CommandStatusLabel");
            _settingsButton = UitkElementUtility.Required<Button>(root, "SettingsButton");
            _slotSurface = new GarageSetBSlotSurface(root);
            _partListSurface = new GarageSetBPartListSurface(root);
            var previewTitleLabel = UitkElementUtility.Required<Label>(root, "PreviewTitleLabel");
            var previewTagRow = root.Q<VisualElement>("PreviewTagRow");
            _unitPreviewHost = UitkElementUtility.Required<VisualElement>(root, "UnitPreviewHost");
            var unitPreviewLabel = UitkElementUtility.Required<Label>(root, "UnitPreviewLabel");
            _previewPowerBar = UitkElementUtility.Required<VisualElement>(root, "PreviewPowerBar");
            _previewPowerFill = UitkElementUtility.Required<VisualElement>(
                root,
                "PreviewPowerFill"
            );
            _previewPowerLabel = UitkElementUtility.Required<Label>(root, "PreviewPowerLabel");
            _statRadar = new GarageStatRadarElement { name = "StatRadarGraph" };
            _statRadar.AddToClassList("stat-radar-graph");
            _previewCard.Add(_statRadar);
            var saveDock = UitkElementUtility.Required<VisualElement>(root, "SaveDock");
            _saveValidationLabel = UitkElementUtility.Required<Label>(root, "SaveValidationLabel");
            _saveButton = UitkElementUtility.Required<Button>(root, "SaveButton");
            _surfaceRoot = root;
            _previewController?.Dispose();
            _previewController = new GarageSetBUitkPreviewController(
                transform,
                _previewRenderer,
                _partPreviewRenderer,
                _partListSurface,
                previewTitleLabel,
                previewTagRow,
                _unitPreviewHost,
                unitPreviewLabel);
            _layoutController = new GarageSetBUitkLayoutController(
                _surfaceRoot,
                _screenRoot,
                _workspaceScroll,
                _slotStrip,
                _partFocusBar,
                _partSelectionPane,
                _partListCard,
                _previewCard,
                _unitPreviewHost,
                _selectedPartPreviewCard,
                _selectedPartPreviewHost,
                _partListRowsScroll,
                _statRadar,
                saveDock,
                _isHostBound,
                deltaRows => _partListSurface?.ScrollVisibleOptions(deltaRows) == true
            );

            _layoutController.Apply();
            BindCallbacks();

            if (_hasLastRender)
                RenderToSurface();

            return true;
        }

        public void Render(
            IReadOnlyList<GarageSlotViewModel> slots,
            GarageNovaPartsPanelViewModel partList,
            GarageEditorViewModel editor,
            GarageResultViewModel result,
            GarageEditorFocus focusedPart,
            bool isSaving
        )
        {
            _lastSlots = slots;
            _lastPartList = partList;
            _lastResult = result;
            _lastFocusedPart = focusedPart;
            _lastIsSaving = isSaving;
            _hasLastRender = true;

            if (!Bind())
                return;

            RenderToSurface();
        }

        private void OnEnable()
        {
            Bind();
        }

        private void Reset()
        {
            if (_document == null)
                _document = ComponentAccess.Get<UIDocument>(gameObject);
        }

        private void RenderToSurface()
        {
            if (_slotSurface == null || _partListSurface == null)
                return;

            _partListSurface.Render(_lastPartList, _lastFocusedPart);
            RenderResult(_lastResult, _lastIsSaving);
            _previewController?.Render(_lastSlots, _lastPartList);
            _slotSurface.Render(_lastSlots, _previewController?.RenderSlotPreviews(_lastSlots));
        }

        private void BindCallbacks()
        {
            _slotSurface.SlotSelected += slotIndex => SlotSelected?.Invoke(slotIndex);
            _partListSurface.FocusSelected += focus => PartFocusSelected?.Invoke(focus);
            _partListSurface.SearchChanged += value => PartSearchChanged?.Invoke(value);
            _partListSurface.OptionSelected += selection => PartOptionSelected?.Invoke(selection);
            _saveButton.clicked += () => SaveRequested?.Invoke();
            _settingsButton.clicked += () => SettingsRequested?.Invoke();
        }

        private void OnDestroy()
        {
            _previewController?.Dispose();
            _previewController = null;
        }

        private void RenderResult(GarageResultViewModel result, bool isSaving)
        {
            _commandStatusLabel.text = result?.RosterStatusText ?? "편성 상태 대기";
            string validationText = result?.ValidationText ?? string.Empty;
            _saveValidationLabel.text = validationText;
            _saveValidationLabel.style.display = string.IsNullOrWhiteSpace(validationText)
                ? DisplayStyle.None
                : DisplayStyle.Flex;
            _saveButton.text = isSaving
                ? "저장 중..."
                : result?.PrimaryActionLabel ?? "저장 및 배치";
            _saveButton.SetEnabled(!isSaving && result?.CanSave == true);
            bool showSaveDock =
                result != null && (isSaving || result.CanSave || result.IsDirty || !result.IsReady);
            _layoutController?.SetSaveDockVisible(showSaveDock);
            _statRadar?.Render(result?.Radar);
            bool showValidation =
                result != null
                && (result.IsDirty || result.CanSave)
                && !string.IsNullOrWhiteSpace(validationText);
            _saveValidationLabel.style.display = showValidation
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            bool hasPower = result?.Radar != null;
            if (_previewPowerBar != null)
                _previewPowerBar.style.display = hasPower ? DisplayStyle.Flex : DisplayStyle.None;
            if (_previewPowerLabel != null)
                _previewPowerLabel.text = hasPower ? $"EN {result.Radar.SummonCost}" : string.Empty;
            if (_previewPowerFill != null)
                _previewPowerFill.style.width = Length.Percent(
                    hasPower ? Mathf.Clamp(result.Radar.SummonCost, 0, 100) : 0
                );
        }
    }
}
