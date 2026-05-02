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
        private const float SlotCardHeight = 70f;
        private const float HeroPreviewHeight = 128f;
        private const float UnitPreviewHostSize = 76f;
        private const float MinPartListRowsHeight = 112f;
        private const float SelectedPartPreviewHeight = 64f;
        private const float SelectedPartPreviewHostSize = 44f;
        private const float WorkspaceBottomPadding = 78f;
        private const float SaveDockBottomClearance = 0f;
        private const float HostNavigationClearance = 62f;
        private const float PartListRowsDockGap = 10f;

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
        private Label _previewTitleLabel;
        private VisualElement _unitPreviewHost;
        private Label _unitPreviewLabel;
        private Image _unitPreviewImage;
        private VisualElement _previewPowerBar;
        private VisualElement _previewPowerFill;
        private Label _previewPowerLabel;
        private GarageStatRadarElement _statRadar;
        private VisualElement _saveDock;
        private Button _saveButton;
        private bool _isHostBound;
        private IReadOnlyList<GarageSlotViewModel> _lastSlots;
        private GarageNovaPartsPanelViewModel _lastPartList;
        private GarageResultViewModel _lastResult;
        private GarageEditorFocus _lastFocusedPart;
        private bool _lastIsSaving;
        private bool _hasLastRender;
        private bool _isPartListPointerDown;
        private bool _hasPartListDrag;
        private Vector2 _lastPartListPointerPosition;

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

            _screenRoot = root.Q<VisualElement>("GarageSetBScreen") ?? root;
            _workspaceScroll = UitkElementUtility.Required<VisualElement>(root, "WorkspaceScroll");
            _slotStrip = UitkElementUtility.Required<VisualElement>(root, "SlotStrip");
            _partFocusBar = UitkElementUtility.Required<VisualElement>(root, "PartFocusBar");
            _partSelectionPane = UitkElementUtility.Required<VisualElement>(root, "PartSelectionPane");
            _partListCard = UitkElementUtility.Required<VisualElement>(root, "PartListCard");
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
            _previewPowerBar = UitkElementUtility.Required<VisualElement>(root, "PreviewPowerBar");
            _previewPowerFill = UitkElementUtility.Required<VisualElement>(root, "PreviewPowerFill");
            _previewPowerLabel = UitkElementUtility.Required<Label>(root, "PreviewPowerLabel");
            _statRadar = new GarageStatRadarElement { name = "StatRadarGraph" };
            _statRadar.AddToClassList("stat-radar-graph");
            _previewCard.Add(_statRadar);
            _saveDock = UitkElementUtility.Required<VisualElement>(root, "SaveDock");
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

            RenderPartPreview();
        }

        private void RenderPartPreview()
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
            _previewTitleLabel.text = isVisible ? "기체 미리보기" : "설계도 확인";
        }

        private void SetPartPreviewTexture(Texture texture, bool isVisible)
        {
            _partListSurface?.SetPreviewTexture(texture, isVisible);
        }

        private void RenderResult(GarageResultViewModel result, bool isSaving)
        {
            _commandStatusLabel.text = result?.RosterStatusText ?? "편성 상태 대기";
            _saveButton.text = isSaving ? "저장 중..." : result?.PrimaryActionLabel ?? "저장 및 배치";
            _saveButton.SetEnabled(!isSaving && result?.CanSave == true);
            SetSaveDockVisible();
            _statRadar?.Render(result?.Radar);

            bool hasPower = result?.Radar != null;
            if (_previewPowerBar != null)
                _previewPowerBar.style.display = hasPower ? DisplayStyle.Flex : DisplayStyle.None;
            if (_previewPowerLabel != null)
                _previewPowerLabel.text = hasPower ? $"EN {result.Radar.SummonCost}" : string.Empty;
            if (_previewPowerFill != null)
                _previewPowerFill.style.width = Length.Percent(hasPower ? Mathf.Clamp(result.Radar.SummonCost, 0, 100) : 0);
        }

        private void ApplyCompactAssemblyLayout()
        {
            if (_workspaceScroll == null)
                return;

            ApplyAssemblyOrder();
            ApplyHeroPreviewLayout();
            ApplyRadarLayout();
            ApplySelectedPartPreviewLayout();
            ApplyPartListFlexLayout();
            ApplyWorkspaceClearance();
            ApplySaveDockLayout();
            RegisterPartListRowsResizeCallbacks();
            RegisterPartListScrollInput();
            ApplyPartListRowsHeightToSaveDock();
        }

        private void ApplyAssemblyOrder()
        {
            MoveAfter(_previewCard, _slotStrip);
            MoveAfter(_selectedPartPreviewCard, _partFocusBar);

            if (_slotStrip != null)
            {
                foreach (var child in _slotStrip.Children())
                    child.style.height = SlotCardHeight;
            }
        }

        private void ApplyHeroPreviewLayout()
        {
            if (_previewCard != null)
                _previewCard.style.height = HeroPreviewHeight;
            if (_unitPreviewHost != null)
            {
                _unitPreviewHost.style.alignSelf = Align.FlexStart;
                _unitPreviewHost.style.marginLeft = 22;
                _unitPreviewHost.style.marginTop = 10;
                _unitPreviewHost.style.width = UnitPreviewHostSize;
                _unitPreviewHost.style.height = UnitPreviewHostSize;
            }
        }

        private void ApplyRadarLayout()
        {
            if (_statRadar != null)
            {
                _statRadar.style.position = Position.Absolute;
                _statRadar.style.right = 14;
                _statRadar.style.top = 46;
                _statRadar.style.width = 80;
                _statRadar.style.height = 80;
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
        }

        private void ApplySelectedPartPreviewLayout()
        {
            if (_selectedPartPreviewCard != null)
                _selectedPartPreviewCard.style.height = SelectedPartPreviewHeight;
            if (_selectedPartPreviewHost != null)
            {
                _selectedPartPreviewHost.style.width = SelectedPartPreviewHostSize;
                _selectedPartPreviewHost.style.height = SelectedPartPreviewHostSize;
            }
            if (_partListRowsScroll != null)
            {
                _partListRowsScroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;
                _partListRowsScroll.style.minHeight = MinPartListRowsHeight;
                _partListRowsScroll.style.height = StyleKeyword.Auto;
                _partListRowsScroll.style.flexGrow = 0;
                _partListRowsScroll.style.flexShrink = 0;
            }
        }

        private void ApplyPartListFlexLayout()
        {
            if (_screenRoot != null)
            {
                _screenRoot.style.flexDirection = FlexDirection.Column;
                _screenRoot.style.minHeight = 0;
            }

            if (_isHostBound && _surfaceRoot != null && _surfaceRoot != _screenRoot)
                _surfaceRoot.style.marginBottom = HostNavigationClearance;

            if (_partSelectionPane != null)
            {
                _partSelectionPane.style.flexGrow = 1;
                _partSelectionPane.style.flexShrink = 1;
                _partSelectionPane.style.minHeight = 0;
                _partSelectionPane.style.marginBottom = 0;
            }

            if (_partListCard != null)
            {
                _partListCard.style.flexDirection = FlexDirection.Column;
                _partListCard.style.flexGrow = 1;
                _partListCard.style.flexShrink = 1;
                _partListCard.style.minHeight = 0;
                _partListCard.style.marginBottom = 0;
            }
        }

        private void ApplyWorkspaceClearance()
        {
            if (_workspaceScroll != null)
            {
                _workspaceScroll.style.flexGrow = 1;
                _workspaceScroll.style.flexShrink = 1;
                _workspaceScroll.style.minHeight = 0;
                _workspaceScroll.style.marginBottom = 0;
                _workspaceScroll.style.paddingBottom = WorkspaceBottomPadding;

                if (_workspaceScroll is ScrollView workspaceScrollView)
                {
                    workspaceScrollView.contentContainer.style.height = StyleKeyword.Auto;
                    workspaceScrollView.contentContainer.style.paddingBottom = WorkspaceBottomPadding;
                }
            }
        }

        private void ApplySaveDockLayout()
        {
            if (_screenRoot != null && _saveDock != null && _saveDock.parent != _screenRoot)
                _screenRoot.Add(_saveDock);
            if (_saveDock != null)
            {
                _saveDock.style.position = Position.Absolute;
                _saveDock.style.left = 0;
                _saveDock.style.right = 0;
                _saveDock.style.bottom = SaveDockBottomClearance;
                _saveDock.style.marginLeft = 12;
                _saveDock.style.marginRight = 12;
                _saveDock.style.marginTop = 0;
                _saveDock.style.marginBottom = 0;
            }
        }

        private void SetSaveDockVisible()
        {
            if (_saveDock != null)
                _saveDock.style.display = DisplayStyle.Flex;
            if (_workspaceScroll != null)
                _workspaceScroll.style.paddingBottom = WorkspaceBottomPadding;

            ApplyPartListRowsHeightToSaveDock();
        }

        private void RegisterPartListRowsResizeCallbacks()
        {
            _screenRoot?.UnregisterCallback<GeometryChangedEvent>(OnPartListRowsGeometryChanged);
            _saveDock?.UnregisterCallback<GeometryChangedEvent>(OnPartListRowsGeometryChanged);
            _partListRowsScroll?.UnregisterCallback<GeometryChangedEvent>(OnPartListRowsGeometryChanged);

            _screenRoot?.RegisterCallback<GeometryChangedEvent>(OnPartListRowsGeometryChanged);
            _saveDock?.RegisterCallback<GeometryChangedEvent>(OnPartListRowsGeometryChanged);
            _partListRowsScroll?.RegisterCallback<GeometryChangedEvent>(OnPartListRowsGeometryChanged);
        }

        private void OnPartListRowsGeometryChanged(GeometryChangedEvent evt)
        {
            ApplyPartListRowsHeightToSaveDock();
        }

        private void ApplyPartListRowsHeightToSaveDock()
        {
            if (_partListRowsScroll == null || _screenRoot == null)
                return;

            var rowsBounds = _partListRowsScroll.worldBound;
            var limitBounds = _saveDock != null && _saveDock.resolvedStyle.display != DisplayStyle.None
                ? _saveDock.worldBound
                : _screenRoot.worldBound;

            if (!IsUsableBound(rowsBounds) || !IsUsableBound(limitBounds))
                return;

            float targetHeight = Mathf.Max(
                MinPartListRowsHeight,
                limitBounds.yMin - rowsBounds.yMin - PartListRowsDockGap);
            float currentHeight = _partListRowsScroll.resolvedStyle.height;
            if (Mathf.Abs(currentHeight - targetHeight) < 0.5f)
                return;

            _partListRowsScroll.style.flexGrow = 0;
            _partListRowsScroll.style.flexShrink = 0;
            _partListRowsScroll.style.height = targetHeight;
        }

        private void RegisterPartListScrollInput()
        {
            if (_partListRowsScroll == null)
                return;

            _partListRowsScroll.UnregisterCallback<WheelEvent>(OnPartListWheel, TrickleDown.TrickleDown);
            _partListRowsScroll.UnregisterCallback<PointerDownEvent>(OnPartListPointerDown, TrickleDown.TrickleDown);
            _partListRowsScroll.UnregisterCallback<PointerMoveEvent>(OnPartListPointerMove, TrickleDown.TrickleDown);
            _partListRowsScroll.UnregisterCallback<PointerUpEvent>(OnPartListPointerUp, TrickleDown.TrickleDown);
            _partListRowsScroll.UnregisterCallback<PointerCancelEvent>(OnPartListPointerCancel, TrickleDown.TrickleDown);

            _partListRowsScroll.RegisterCallback<WheelEvent>(OnPartListWheel, TrickleDown.TrickleDown);
            _partListRowsScroll.RegisterCallback<PointerDownEvent>(OnPartListPointerDown, TrickleDown.TrickleDown);
            _partListRowsScroll.RegisterCallback<PointerMoveEvent>(OnPartListPointerMove, TrickleDown.TrickleDown);
            _partListRowsScroll.RegisterCallback<PointerUpEvent>(OnPartListPointerUp, TrickleDown.TrickleDown);
            _partListRowsScroll.RegisterCallback<PointerCancelEvent>(OnPartListPointerCancel, TrickleDown.TrickleDown);
        }

        private void OnPartListWheel(WheelEvent evt)
        {
            ScrollPartList(evt.delta.y * 18f);
            evt.StopPropagation();
        }

        private void OnPartListPointerDown(PointerDownEvent evt)
        {
            _isPartListPointerDown = true;
            _hasPartListDrag = false;
            _lastPartListPointerPosition = evt.position;
        }

        private void OnPartListPointerMove(PointerMoveEvent evt)
        {
            if (!_isPartListPointerDown)
                return;

            float deltaY = _lastPartListPointerPosition.y - evt.position.y;
            _lastPartListPointerPosition = evt.position;
            if (Mathf.Abs(deltaY) < 1f && !_hasPartListDrag)
                return;

            _hasPartListDrag = true;
            ScrollPartList(deltaY);
            evt.StopPropagation();
        }

        private void OnPartListPointerUp(PointerUpEvent evt)
        {
            if (_hasPartListDrag)
                evt.StopPropagation();

            _isPartListPointerDown = false;
            _hasPartListDrag = false;
        }

        private void OnPartListPointerCancel(PointerCancelEvent evt)
        {
            _isPartListPointerDown = false;
            _hasPartListDrag = false;
        }

        private void ScrollPartList(float deltaY)
        {
            if (_partListRowsScroll == null || Mathf.Abs(deltaY) < 1f)
                return;

            int deltaRows = Mathf.Clamp(Mathf.RoundToInt(deltaY / 58f), -4, 4);
            if (deltaRows == 0)
                deltaRows = deltaY > 0f ? 1 : -1;

            _partListSurface?.ScrollVisibleOptions(deltaRows);
        }

        private static bool IsUsableBound(Rect rect)
        {
            return rect.width > 0f
                && rect.height > 0f
                && !float.IsNaN(rect.yMin)
                && !float.IsNaN(rect.yMax)
                && !float.IsInfinity(rect.yMin)
                && !float.IsInfinity(rect.yMax);
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
