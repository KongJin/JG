using System;
using System.Collections.Generic;
using Shared.Runtime;
using Shared.Runtime.Pooling;
using Shared.Ui;
using UnityEngine;
using UnityEngine.UIElements;

namespace Features.Garage.Presentation
{
    public sealed class GarageSetBUitkRuntimeAdapter : MonoBehaviour
    {
        private const float HeroPreviewHeight = 250f;
        private const float UnitPreviewHostSize = 178f;
        private const float PartListRowsHeight = 124f;
        private const float SelectedPartPreviewHeight = 82f;
        private const float SelectedPartPreviewHostSize = 62f;

        [SerializeField]
        private UIDocument _document;

        [SerializeField]
        private GarageSetBUitkPreviewRenderer _previewRenderer;

        [SerializeField]
        private GarageSetBUitkPreviewRenderer _partPreviewRenderer;

        private VisualElement _surfaceRoot;
        private VisualElement _hostScreenRoot;
        private ScrollView _workspaceScroll;
        private VisualElement _slotStrip;
        private VisualElement _partFocusBar;
        private VisualElement _partSelectionPane;
        private VisualElement _previewCard;
        private VisualElement _selectedPartPreviewCard;
        private VisualElement _selectedPartPreviewHost;
        private ScrollView _partListRowsScroll;
        private Label _commandStatusLabel;
        private Button _settingsButton;
        private GarageSetBSlotSurface _slotSurface;
        private GarageSetBPartListSurface _partListSurface;
        private Label _previewTitleLabel;
        private VisualElement _unitPreviewHost;
        private Label _unitPreviewLabel;
        private Image _unitPreviewImage;
        private VisualElement _previewPowerFill;
        private Label _previewPowerLabel;
        private GarageStatRadarElement _statRadar;
        private Button _saveButton;
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
                ApplyCompactAssemblyLayout();
                return true;
            }

            _workspaceScroll = UitkElementUtility.Required<ScrollView>(root, "WorkspaceScroll");
            _slotStrip = UitkElementUtility.Required<VisualElement>(root, "SlotStrip");
            _partFocusBar = UitkElementUtility.Required<VisualElement>(root, "PartFocusBar");
            _partSelectionPane = UitkElementUtility.Required<VisualElement>(root, "PartSelectionPane");
            _previewCard = UitkElementUtility.Required<VisualElement>(root, "PreviewCard");
            _selectedPartPreviewCard = UitkElementUtility.Required<VisualElement>(root, "SelectedPartPreviewCard");
            _selectedPartPreviewHost = UitkElementUtility.Required<VisualElement>(root, "SelectedPartPreviewHost");
            _partListRowsScroll = UitkElementUtility.Required<ScrollView>(root, "PartListRows");
            _commandStatusLabel = UitkElementUtility.Required<Label>(root, "CommandStatusLabel");
            _settingsButton = UitkElementUtility.Required<Button>(root, "SettingsButton");
            _slotSurface = new GarageSetBSlotSurface(root);
            _partListSurface = new GarageSetBPartListSurface(root);
            _previewTitleLabel = UitkElementUtility.Required<Label>(root, "PreviewTitleLabel");
            _unitPreviewHost = UitkElementUtility.Required<VisualElement>(root, "UnitPreviewHost");
            _unitPreviewLabel = UitkElementUtility.Required<Label>(root, "UnitPreviewLabel");
            _unitPreviewImage = UitkElementUtility.CreateAbsoluteImage();
            _unitPreviewHost.Insert(0, _unitPreviewImage);
            _previewPowerFill = UitkElementUtility.Required<VisualElement>(root, "PreviewPowerFill");
            _previewPowerLabel = UitkElementUtility.Required<Label>(root, "PreviewPowerLabel");
            _statRadar = new GarageStatRadarElement { name = "StatRadarGraph" };
            _statRadar.AddToClassList("stat-radar-graph");
            _previewCard.Add(_statRadar);
            _saveButton = UitkElementUtility.Required<Button>(root, "SaveButton");
            _surfaceRoot = root;

            ApplyCompactAssemblyLayout();
            BindCallbacks();
            SetPreviewTexture(null, false);
            SetPartPreviewTexture(null, false);

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
            bool isSaving)
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

        private void RenderPreview(IReadOnlyList<GarageSlotViewModel> slots)
        {
            var selectedSlot = FindSelectedSlot(slots);
            if (_previewRenderer == null)
            {
                SetPreviewTexture(null, false);
            }
            else
            {
                bool hasPreview = _previewRenderer.Render(selectedSlot);
                SetPreviewTexture(_previewRenderer.PreviewTexture, hasPreview);
            }

            RenderPartPreview(selectedSlot);
        }

        private void RenderPartPreview(GarageSlotViewModel selectedSlot)
        {
            if (_partPreviewRenderer == null)
            {
                SetPartPreviewTexture(null, false);
                return;
            }

            bool hasPreview = _partPreviewRenderer.RenderPart(_lastPartList);
            SetPartPreviewTexture(_partPreviewRenderer.PreviewTexture, hasPreview);
        }

        private static GarageSlotViewModel FindSelectedSlot(IReadOnlyList<GarageSlotViewModel> slots)
        {
            if (slots == null)
                return null;

            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].IsSelected)
                    return slots[i];
            }

            return slots.Count > 0 ? slots[0] : null;
        }

        private void RenderToSurface()
        {
            if (_slotSurface == null || _partListSurface == null)
                return;

            _slotSurface.Render(_lastSlots);
            _partListSurface.Render(_lastPartList, _lastFocusedPart);
            RenderResult(_lastResult, _lastIsSaving);
            RenderPreview(_lastSlots);
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

        private void SetPreviewTexture(Texture texture, bool isVisible)
        {
            if (_unitPreviewImage == null || _unitPreviewLabel == null)
                return;

            _unitPreviewImage.image = isVisible ? texture : null;
            _unitPreviewImage.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
            _unitPreviewLabel.style.display = isVisible ? DisplayStyle.None : DisplayStyle.Flex;
            _previewTitleLabel.text = isVisible ? "UNIT PREVIEW" : "BLUEPRINT VIEW";
        }

        private void SetPartPreviewTexture(Texture texture, bool isVisible)
        {
            _partListSurface?.SetPreviewTexture(texture, isVisible);
        }

        private void RenderResult(GarageResultViewModel result, bool isSaving)
        {
            _commandStatusLabel.text = result?.RosterStatusText ?? "COMMAND_STATUS: 대기";
            _saveButton.text = isSaving ? "저장 중..." : result?.PrimaryActionLabel ?? "저장 및 배치";
            _saveButton.SetEnabled(!isSaving && result?.CanSave == true);
            _statRadar?.Render(result?.Radar);

            if (_previewPowerLabel != null)
                _previewPowerLabel.text = result?.Radar != null ? $"EN {result.Radar.SummonCost}" : "EN --";
            if (_previewPowerFill != null)
                _previewPowerFill.style.width = Length.Percent(result?.Radar != null ? Mathf.Clamp(result.Radar.SummonCost, 0, 100) : 0);
        }

        private void ApplyCompactAssemblyLayout()
        {
            if (_workspaceScroll?.contentContainer == null)
                return;

            MoveAfter(_previewCard, _slotStrip);
            MoveAfter(_selectedPartPreviewCard, _partFocusBar);

            if (_previewCard != null)
                _previewCard.style.height = HeroPreviewHeight;
            if (_unitPreviewHost != null)
            {
                _unitPreviewHost.style.alignSelf = Align.FlexStart;
                _unitPreviewHost.style.marginLeft = 18;
                _unitPreviewHost.style.marginTop = 16;
                _unitPreviewHost.style.width = UnitPreviewHostSize;
                _unitPreviewHost.style.height = UnitPreviewHostSize;
            }
            if (_statRadar != null)
            {
                _statRadar.style.position = Position.Absolute;
                _statRadar.style.right = 14;
                _statRadar.style.top = 46;
                _statRadar.style.width = 118;
                _statRadar.style.height = 118;
                _statRadar.style.backgroundColor = new Color(0.035f, 0.035f, 0.045f, 0.34f);
                _statRadar.style.borderTopWidth = 1;
                _statRadar.style.borderRightWidth = 1;
                _statRadar.style.borderBottomWidth = 1;
                _statRadar.style.borderLeftWidth = 1;
                var radarBorder = new Color(0.37f, 0.71f, 1f, 0.24f);
                _statRadar.style.borderTopColor = radarBorder;
                _statRadar.style.borderRightColor = radarBorder;
                _statRadar.style.borderBottomColor = radarBorder;
                _statRadar.style.borderLeftColor = radarBorder;
                _statRadar.style.borderTopLeftRadius = 6;
                _statRadar.style.borderTopRightRadius = 6;
                _statRadar.style.borderBottomLeftRadius = 6;
                _statRadar.style.borderBottomRightRadius = 6;
            }
            if (_selectedPartPreviewCard != null)
                _selectedPartPreviewCard.style.height = SelectedPartPreviewHeight;
            if (_selectedPartPreviewHost != null)
            {
                _selectedPartPreviewHost.style.width = SelectedPartPreviewHostSize;
                _selectedPartPreviewHost.style.height = SelectedPartPreviewHostSize;
            }
            if (_partListRowsScroll != null)
                _partListRowsScroll.style.height = PartListRowsHeight;
        }

        private static void MoveAfter(VisualElement element, VisualElement anchor)
        {
            var parent = anchor?.parent;
            if (parent == null || element == null || anchor == null || element.parent != parent || anchor.parent != parent)
                return;

            element.PlaceInFront(anchor);
        }
    }
}
