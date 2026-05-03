using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Features.Garage.Presentation
{
    internal sealed class GarageSetBUitkLayoutController
    {
        private const float SlotCellHeight = 94f;
        private const float HeroPreviewHeight = 156f;
        private const float UnitPreviewHostSize = 108f;
        private const float MinPartListRowsHeight = 104f;
        private const float StablePartListRowsHeight = 252f;
        private const float SelectedPartPreviewWidth = 118f;
        private const float SelectedPartPreviewHostSize = 88f;
        private const float WorkspaceBottomPadding = 16f;
        private const float HostNavigationClearance = 62f;
        private const float PartListDragThreshold = 4f;
        private const float PartRowScrollStep = 50f;
        private const int NoPointerId = -1;

        private readonly VisualElement _surfaceRoot;
        private readonly VisualElement _screenRoot;
        private readonly VisualElement _workspaceScroll;
        private readonly VisualElement _slotStrip;
        private readonly VisualElement _partFocusBar;
        private readonly VisualElement _partSelectionPane;
        private readonly VisualElement _partListCard;
        private readonly VisualElement _previewCard;
        private readonly VisualElement _unitPreviewHost;
        private readonly VisualElement _selectedPartPreviewCard;
        private readonly VisualElement _selectedPartPreviewHost;
        private readonly ScrollView _partListRowsScroll;
        private readonly GarageStatRadarElement _statRadar;
        private readonly VisualElement _saveDock;
        private readonly Func<int, bool> _scrollVisibleOptions;
        private readonly bool _isHostBound;
        private bool _isPartListPointerDown;
        private bool _hasPartListDrag;
        private bool _hasCapturedPartListPointer;
        private bool _suppressNextPartListClick;
        private int _activePartListPointerId = NoPointerId;
        private Vector2 _partListPointerDownPosition;
        private Vector2 _lastPartListPointerPosition;

        public GarageSetBUitkLayoutController(
            VisualElement surfaceRoot,
            VisualElement screenRoot,
            VisualElement workspaceScroll,
            VisualElement slotStrip,
            VisualElement partFocusBar,
            VisualElement partSelectionPane,
            VisualElement partListCard,
            VisualElement previewCard,
            VisualElement unitPreviewHost,
            VisualElement selectedPartPreviewCard,
            VisualElement selectedPartPreviewHost,
            ScrollView partListRowsScroll,
            GarageStatRadarElement statRadar,
            VisualElement saveDock,
            bool isHostBound,
            Func<int, bool> scrollVisibleOptions)
        {
            _surfaceRoot = surfaceRoot;
            _screenRoot = screenRoot;
            _workspaceScroll = workspaceScroll;
            _slotStrip = slotStrip;
            _partFocusBar = partFocusBar;
            _partSelectionPane = partSelectionPane;
            _partListCard = partListCard;
            _previewCard = previewCard;
            _unitPreviewHost = unitPreviewHost;
            _selectedPartPreviewCard = selectedPartPreviewCard;
            _selectedPartPreviewHost = selectedPartPreviewHost;
            _partListRowsScroll = partListRowsScroll;
            _statRadar = statRadar;
            _saveDock = saveDock;
            _isHostBound = isHostBound;
            _scrollVisibleOptions = scrollVisibleOptions;
        }

        public void Apply()
        {
            if (_workspaceScroll == null)
                return;

            ApplyAssemblyOrder();
            ApplyHeroPreviewLayout();
            ApplyRadarLayout();
            ApplySelectedPartPreviewLayout();
            ApplyPartListFlexLayout();
            ApplyWorkspaceClearance();
            ApplySaveDockFooterLayout();
            RegisterPartListRowsResizeCallbacks();
            RegisterPartListScrollInput();
            ApplyPartListRowsHeightToPane();
        }

        public void SetSaveDockVisible(bool isVisible = true)
        {
            if (_saveDock != null)
                _saveDock.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
            if (_workspaceScroll != null)
                _workspaceScroll.style.paddingBottom = WorkspaceBottomPadding;

            ApplyPartListRowsHeightToPane();
        }

        private void ApplyAssemblyOrder()
        {
            MoveAfter(_previewCard, _slotStrip);

            if (_slotStrip == null)
                return;

            foreach (var child in _slotStrip.Children())
            {
                child.style.height = SlotCellHeight;
                child.style.minHeight = SlotCellHeight;
                child.style.maxHeight = SlotCellHeight;
                child.style.flexShrink = 0;
            }
        }

        private void ApplyHeroPreviewLayout()
        {
            if (_previewCard != null)
                _previewCard.style.height = HeroPreviewHeight;
            if (_unitPreviewHost != null)
            {
                _unitPreviewHost.style.alignSelf = Align.FlexStart;
                _unitPreviewHost.style.marginLeft = 24;
                _unitPreviewHost.style.marginTop = 22;
                _unitPreviewHost.style.width = UnitPreviewHostSize;
                _unitPreviewHost.style.height = UnitPreviewHostSize;
            }
        }

        private void ApplyRadarLayout()
        {
            if (_statRadar == null)
                return;

            _statRadar.style.position = Position.Absolute;
            _statRadar.style.right = 14;
            _statRadar.style.top = 24;
            _statRadar.style.width = 108;
            _statRadar.style.height = 108;
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

        private void ApplySelectedPartPreviewLayout()
        {
            if (_selectedPartPreviewCard != null)
            {
                _selectedPartPreviewCard.style.width = SelectedPartPreviewWidth;
                _selectedPartPreviewCard.style.minWidth = SelectedPartPreviewWidth;
                _selectedPartPreviewCard.style.maxWidth = SelectedPartPreviewWidth;
                _selectedPartPreviewCard.style.height = StyleKeyword.Auto;
                _selectedPartPreviewCard.style.minHeight = 0;
                _selectedPartPreviewCard.style.maxHeight = StyleKeyword.None;
                _selectedPartPreviewCard.style.flexShrink = 0f;
                _selectedPartPreviewCard.style.marginRight = 8f;
                _selectedPartPreviewCard.style.marginBottom = 0f;
                _selectedPartPreviewCard.style.alignSelf = Align.Stretch;
            }
            if (_selectedPartPreviewHost != null)
            {
                _selectedPartPreviewHost.style.width = SelectedPartPreviewHostSize;
                _selectedPartPreviewHost.style.height = SelectedPartPreviewHostSize;
                _selectedPartPreviewHost.style.minWidth = SelectedPartPreviewHostSize;
                _selectedPartPreviewHost.style.minHeight = SelectedPartPreviewHostSize;
            }
            if (_partListRowsScroll != null)
            {
                _partListRowsScroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;
                _partListRowsScroll.mouseWheelScrollSize = 0f;
                _partListRowsScroll.touchScrollBehavior = ScrollView.TouchScrollBehavior.Clamped;
                _partListRowsScroll.nestedInteractionKind = ScrollView.NestedInteractionKind.StopScrolling;
                _partListRowsScroll.elasticity = 0f;
                _partListRowsScroll.scrollDecelerationRate = 0f;
                _partListRowsScroll.style.minHeight = MinPartListRowsHeight;
                _partListRowsScroll.style.height = StyleKeyword.Auto;
                _partListRowsScroll.style.flexGrow = 0;
                _partListRowsScroll.style.flexShrink = 0;
                NormalizePartListScrollPosition();
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
                _partSelectionPane.style.flexDirection = FlexDirection.Row;
                _partSelectionPane.style.flexGrow = 1;
                _partSelectionPane.style.flexShrink = 1;
                _partSelectionPane.style.minHeight = 0;
                _partSelectionPane.style.marginBottom = 0;
                _partSelectionPane.style.width = Length.Percent(100f);
            }

            if (_partListCard != null)
            {
                _partListCard.style.flexDirection = FlexDirection.Column;
                _partListCard.style.flexGrow = 1;
                _partListCard.style.flexShrink = 1;
                _partListCard.style.width = StyleKeyword.Auto;
                _partListCard.style.minWidth = 0;
                _partListCard.style.minHeight = 0;
                _partListCard.style.marginBottom = 0;
                _partListCard.style.paddingLeft = 12f;
                _partListCard.style.paddingRight = 12f;
                _partListCard.style.paddingTop = 12f;
                _partListCard.style.paddingBottom = 10f;
                _partListCard.style.borderTopWidth = 1f;
                _partListCard.style.borderRightWidth = 1f;
                _partListCard.style.borderBottomWidth = 1f;
                _partListCard.style.borderLeftWidth = 1f;
                var partListBorder = new Color(0.37f, 0.71f, 1f, 0.55f);
                _partListCard.style.borderTopColor = partListBorder;
                _partListCard.style.borderRightColor = partListBorder;
                _partListCard.style.borderBottomColor = partListBorder;
                _partListCard.style.borderLeftColor = partListBorder;
            }
        }

        private void ApplyWorkspaceClearance()
        {
            if (_workspaceScroll == null)
                return;

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

        private void ApplySaveDockFooterLayout()
        {
            if (_saveDock != null)
            {
                _saveDock.style.position = Position.Relative;
                _saveDock.style.left = 0;
                _saveDock.style.right = 0;
                _saveDock.style.bottom = StyleKeyword.Auto;
                _saveDock.style.marginLeft = 0;
                _saveDock.style.marginRight = 0;
                _saveDock.style.marginTop = 6;
                _saveDock.style.marginBottom = 0;
                _saveDock.style.flexShrink = 0;
            }
        }

        private void RegisterPartListRowsResizeCallbacks()
        {
            _screenRoot?.UnregisterCallback<GeometryChangedEvent>(OnPartListRowsGeometryChanged);
            _partListCard?.UnregisterCallback<GeometryChangedEvent>(OnPartListRowsGeometryChanged);
            _saveDock?.UnregisterCallback<GeometryChangedEvent>(OnPartListRowsGeometryChanged);
            _partListRowsScroll?.UnregisterCallback<GeometryChangedEvent>(OnPartListRowsGeometryChanged);

            _screenRoot?.RegisterCallback<GeometryChangedEvent>(OnPartListRowsGeometryChanged);
            _partListCard?.RegisterCallback<GeometryChangedEvent>(OnPartListRowsGeometryChanged);
            _saveDock?.RegisterCallback<GeometryChangedEvent>(OnPartListRowsGeometryChanged);
            _partListRowsScroll?.RegisterCallback<GeometryChangedEvent>(OnPartListRowsGeometryChanged);
        }

        private void OnPartListRowsGeometryChanged(GeometryChangedEvent evt)
        {
            ApplyPartListRowsHeightToPane();
        }

        private void ApplyPartListRowsHeightToPane()
        {
            if (_partListRowsScroll == null)
                return;

            var rowsBounds = _partListRowsScroll.worldBound;

            if (!IsUsableBound(rowsBounds))
                return;

            float targetHeight = StablePartListRowsHeight;
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
            _partListRowsScroll.UnregisterCallback<PointerLeaveEvent>(OnPartListPointerLeave, TrickleDown.TrickleDown);
            _partListRowsScroll.UnregisterCallback<PointerCaptureOutEvent>(OnPartListPointerCaptureOut);
            _partListRowsScroll.UnregisterCallback<ClickEvent>(OnPartListClick, TrickleDown.TrickleDown);

            _partListRowsScroll.RegisterCallback<WheelEvent>(OnPartListWheel, TrickleDown.TrickleDown);
            _partListRowsScroll.RegisterCallback<PointerDownEvent>(OnPartListPointerDown, TrickleDown.TrickleDown);
            _partListRowsScroll.RegisterCallback<PointerMoveEvent>(OnPartListPointerMove, TrickleDown.TrickleDown);
            _partListRowsScroll.RegisterCallback<PointerUpEvent>(OnPartListPointerUp, TrickleDown.TrickleDown);
            _partListRowsScroll.RegisterCallback<PointerCancelEvent>(OnPartListPointerCancel, TrickleDown.TrickleDown);
            _partListRowsScroll.RegisterCallback<PointerLeaveEvent>(OnPartListPointerLeave, TrickleDown.TrickleDown);
            _partListRowsScroll.RegisterCallback<PointerCaptureOutEvent>(OnPartListPointerCaptureOut);
            _partListRowsScroll.RegisterCallback<ClickEvent>(OnPartListClick, TrickleDown.TrickleDown);
        }

        private void OnPartListWheel(WheelEvent evt)
        {
            ScrollPartList(evt.delta.y * 18f);
            NormalizePartListScrollPosition();
            evt.StopImmediatePropagation();
        }

        private void OnPartListPointerDown(PointerDownEvent evt)
        {
            if (!evt.isPrimary || (evt.pointerType == UnityEngine.UIElements.PointerType.mouse && evt.button != 0))
                return;

            _isPartListPointerDown = true;
            _hasPartListDrag = false;
            _hasCapturedPartListPointer = false;
            _suppressNextPartListClick = false;
            _activePartListPointerId = evt.pointerId;
            _partListPointerDownPosition = evt.position;
            _lastPartListPointerPosition = evt.position;
            NormalizePartListScrollPosition();
        }

        private void OnPartListPointerMove(PointerMoveEvent evt)
        {
            if (!_isPartListPointerDown || evt.pointerId != _activePartListPointerId)
                return;

            if (evt.pointerType == UnityEngine.UIElements.PointerType.mouse && evt.pressedButtons == 0)
            {
                ResetPartListPointerTracking(evt.pointerId);
                return;
            }

            float deltaY = _lastPartListPointerPosition.y - evt.position.y;
            _lastPartListPointerPosition = evt.position;
            float totalDragY = Mathf.Abs(evt.position.y - _partListPointerDownPosition.y);
            if (totalDragY < PartListDragThreshold && !_hasPartListDrag)
                return;

            _hasPartListDrag = true;
            _suppressNextPartListClick = true;
            CapturePartListPointer(evt.pointerId);
            ScrollPartList(deltaY);
            NormalizePartListScrollPosition();
            evt.StopImmediatePropagation();
        }

        private void OnPartListPointerUp(PointerUpEvent evt)
        {
            if (evt.pointerId != _activePartListPointerId)
                return;

            bool wasDrag = _hasPartListDrag;
            ResetPartListPointerTracking(evt.pointerId);
            NormalizePartListScrollPosition();
            if (wasDrag)
            {
                evt.StopImmediatePropagation();
            }
        }

        private void OnPartListPointerCancel(PointerCancelEvent evt)
        {
            if (evt.pointerId == _activePartListPointerId)
                ResetPartListPointerTracking(evt.pointerId);
        }

        private void OnPartListPointerLeave(PointerLeaveEvent evt)
        {
            if (evt.pointerId == _activePartListPointerId && !_hasCapturedPartListPointer)
                ResetPartListPointerTracking(evt.pointerId);
        }

        private void OnPartListPointerCaptureOut(PointerCaptureOutEvent evt)
        {
            if (evt.pointerId == _activePartListPointerId)
                ResetPartListPointerTracking(evt.pointerId);
        }

        private void OnPartListClick(ClickEvent evt)
        {
            if (!_suppressNextPartListClick)
                return;

            _suppressNextPartListClick = false;
            evt.StopImmediatePropagation();
        }

        private bool ScrollPartList(float deltaY)
        {
            if (_scrollVisibleOptions == null || Mathf.Abs(deltaY) < 1f)
                return false;

            int deltaRows = Mathf.Clamp(Mathf.RoundToInt(deltaY / PartRowScrollStep), -4, 4);
            if (deltaRows == 0)
                deltaRows = deltaY > 0f ? 1 : -1;

            return _scrollVisibleOptions(deltaRows);
        }

        private void CapturePartListPointer(int pointerId)
        {
            if (_partListRowsScroll == null || _hasCapturedPartListPointer)
                return;

            PointerCaptureHelper.CapturePointer(_partListRowsScroll, pointerId);
            _hasCapturedPartListPointer = true;
        }

        private void ResetPartListPointerTracking(int pointerId)
        {
            bool shouldReleasePointer = _partListRowsScroll != null
                && _hasCapturedPartListPointer
                && PointerCaptureHelper.HasPointerCapture(_partListRowsScroll, pointerId);

            _hasCapturedPartListPointer = false;

            if (shouldReleasePointer)
            {
                PointerCaptureHelper.ReleasePointer(_partListRowsScroll, pointerId);
            }

            _isPartListPointerDown = false;
            _hasPartListDrag = false;
            _activePartListPointerId = NoPointerId;
        }

        private void NormalizePartListScrollPosition()
        {
            if (_partListRowsScroll == null || _partListRowsScroll.scrollOffset == Vector2.zero)
                return;

            _partListRowsScroll.scrollOffset = Vector2.zero;
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

        private static void MoveBefore(VisualElement element, VisualElement anchor)
        {
            var parent = anchor?.parent;
            if (parent == null || element == null || anchor == null)
                return;

            if (element.parent != parent)
            {
                element.RemoveFromHierarchy();
                parent.Insert(parent.IndexOf(anchor), element);
                return;
            }

            element.PlaceBehind(anchor);
        }
    }
}
