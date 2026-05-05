using System;
using Shared.Ui;
using UnityEngine;
using UnityEngine.UIElements;

namespace Features.Garage.Presentation
{
    internal sealed class GarageSetBFocusSwipeHandler : BaseSurface<VisualElement>
    {
        private const float FocusSwipeThreshold = 42f;
        private const float FocusSwipeAxisBias = 1.25f;
        private static readonly GarageEditorFocus[] FocusOrder =
        {
            GarageEditorFocus.Mobility,
            GarageEditorFocus.Frame,
            GarageEditorFocus.Firepower
        };

        private readonly FocusTabBinding[] _tabs;
        private readonly VisualElement _swipeHost;
        private readonly VisualElement _ignoredElement;
        private readonly Action _suppressNextClick;
        private GarageEditorFocus _lastFocusedPart = GarageEditorFocus.Mobility;
        private int _swipePointerId = -1;
        private Vector2 _swipeStartPosition;
        private bool _isSwipeActive;
        private EventCallback<PointerDownEvent> _swipePointerDown;
        private EventCallback<PointerMoveEvent> _swipePointerMove;
        private EventCallback<PointerUpEvent> _swipePointerUp;
        private EventCallback<PointerCancelEvent> _swipePointerCancel;

        public GarageSetBFocusSwipeHandler(
            VisualElement root,
            VisualElement ignoredElement,
            Action suppressNextClick)
            : base(root)
        {
            _tabs = new[]
            {
                new FocusTabBinding(
                    UitkElementUtility.Required<Button>(root, "FrameTabButton"),
                    GarageEditorFocus.Frame,
                    () => SelectFocus(GarageEditorFocus.Frame)),
                new FocusTabBinding(
                    UitkElementUtility.Required<Button>(root, "FirepowerTabButton"),
                    GarageEditorFocus.Firepower,
                    () => SelectFocus(GarageEditorFocus.Firepower)),
                new FocusTabBinding(
                    UitkElementUtility.Required<Button>(root, "MobilityTabButton"),
                    GarageEditorFocus.Mobility,
                    () => SelectFocus(GarageEditorFocus.Mobility)),
            };
            _swipeHost = UitkElementUtility.Required<VisualElement>(root, "PartSelectionPane");
            _ignoredElement = ignoredElement;
            _suppressNextClick = suppressNextClick;
            BindCallbacks();
        }

        public event Action<GarageEditorFocus> FocusSelected;

        public void Render(GarageEditorFocus focusedPart)
        {
            _lastFocusedPart = focusedPart;
            for (int i = 0; i < _tabs.Length; i++)
                UitkElementUtility.SetClass(_tabs[i].Button, "focus-tab--active", focusedPart == _tabs[i].Focus);
        }

        internal bool TrySelectFocusFromHorizontalDrag(Vector2 dragDelta)
        {
            float absoluteX = Mathf.Abs(dragDelta.x);
            float absoluteY = Mathf.Abs(dragDelta.y);
            if (absoluteX < FocusSwipeThreshold || absoluteX < absoluteY * FocusSwipeAxisBias)
                return false;

            int direction = dragDelta.x < 0f ? 1 : -1;
            if (!TryResolveAdjacentFocus(_lastFocusedPart, direction, out var nextFocus))
                return false;

            FocusSelected?.Invoke(nextFocus);
            return true;
        }

        private void BindCallbacks()
        {
            if (IsDisposed)
                return;

            for (int i = 0; i < _tabs.Length; i++)
                _tabs[i].Button.clicked += _tabs[i].Clicked;

            _swipePointerDown = BeginFocusSwipe;
            _swipePointerMove = UpdateFocusSwipe;
            _swipePointerUp = EndFocusSwipe;
            _swipePointerCancel = CancelFocusSwipe;
            _swipeHost.RegisterCallback(_swipePointerDown);
            _swipeHost.RegisterCallback(_swipePointerMove);
            _swipeHost.RegisterCallback(_swipePointerUp);
            _swipeHost.RegisterCallback(_swipePointerCancel);
        }

        private void SelectFocus(GarageEditorFocus focus)
        {
            FocusSelected?.Invoke(focus);
        }

        private void BeginFocusSwipe(PointerDownEvent evt)
        {
            if (evt == null ||
                _swipePointerId >= 0 ||
                IsInsideElement(evt.target as VisualElement, _ignoredElement))
                return;

            _swipePointerId = evt.pointerId;
            _swipeStartPosition = ToVector2(evt.position);
            _isSwipeActive = false;
            _swipeHost.CapturePointer(evt.pointerId);
        }

        private void UpdateFocusSwipe(PointerMoveEvent evt)
        {
            if (evt == null || evt.pointerId != _swipePointerId)
                return;

            var delta = ToVector2(evt.position) - _swipeStartPosition;
            if (!_isSwipeActive &&
                Mathf.Abs(delta.x) >= FocusSwipeThreshold &&
                Mathf.Abs(delta.x) >= Mathf.Abs(delta.y) * FocusSwipeAxisBias)
            {
                _isSwipeActive = true;
                _suppressNextClick?.Invoke();
            }

            if (_isSwipeActive)
                evt.StopPropagation();
        }

        private void EndFocusSwipe(PointerUpEvent evt)
        {
            if (evt == null || evt.pointerId != _swipePointerId)
                return;

            var delta = ToVector2(evt.position) - _swipeStartPosition;
            ReleaseSwipePointer(evt.pointerId);
            ResetFocusSwipe();

            if (!TrySelectFocusFromHorizontalDrag(delta))
                return;

            _suppressNextClick?.Invoke();
            evt.StopImmediatePropagation();
        }

        private void CancelFocusSwipe(PointerCancelEvent evt)
        {
            if (evt == null || evt.pointerId != _swipePointerId)
                return;

            ReleaseSwipePointer(evt.pointerId);
            ResetFocusSwipe();
        }

        private void ReleaseSwipePointer(int pointerId)
        {
            if (_swipeHost.HasPointerCapture(pointerId))
                _swipeHost.ReleasePointer(pointerId);
        }

        private void ResetFocusSwipe()
        {
            _swipePointerId = -1;
            _swipeStartPosition = Vector2.zero;
            _isSwipeActive = false;
        }

        private static bool TryResolveAdjacentFocus(
            GarageEditorFocus currentFocus,
            int direction,
            out GarageEditorFocus nextFocus)
        {
            nextFocus = currentFocus;
            int currentIndex = 0;
            for (int i = 0; i < FocusOrder.Length; i++)
            {
                if (FocusOrder[i] == currentFocus)
                {
                    currentIndex = i;
                    break;
                }
            }

            int nextIndex = currentIndex + direction;
            if (nextIndex < 0 || nextIndex >= FocusOrder.Length)
                return false;

            nextFocus = FocusOrder[nextIndex];
            return true;
        }

        private static bool IsInsideElement(VisualElement target, VisualElement ancestor)
        {
            while (target != null)
            {
                if (ReferenceEquals(target, ancestor))
                    return true;

                target = target.parent;
            }

            return false;
        }

        private static Vector2 ToVector2(Vector3 position)
        {
            return new Vector2(position.x, position.y);
        }

        protected override void DisposeSurface()
        {
            for (int i = 0; i < _tabs.Length; i++)
            {
                if (_tabs[i].Button != null)
                    _tabs[i].Button.clicked -= _tabs[i].Clicked;
            }

            if (_swipeHost != null && _swipePointerDown != null)
                _swipeHost.UnregisterCallback(_swipePointerDown);

            if (_swipeHost != null && _swipePointerMove != null)
                _swipeHost.UnregisterCallback(_swipePointerMove);

            if (_swipeHost != null && _swipePointerUp != null)
                _swipeHost.UnregisterCallback(_swipePointerUp);

            if (_swipeHost != null && _swipePointerCancel != null)
                _swipeHost.UnregisterCallback(_swipePointerCancel);

            _swipePointerDown = null;
            _swipePointerMove = null;
            _swipePointerUp = null;
            _swipePointerCancel = null;
        }

        private readonly struct FocusTabBinding
        {
            public FocusTabBinding(
                Button button,
                GarageEditorFocus focus,
                Action clicked)
            {
                Button = button;
                Focus = focus;
                Clicked = clicked;
            }

            public Button Button { get; }
            public GarageEditorFocus Focus { get; }
            public Action Clicked { get; }
        }
    }
}
