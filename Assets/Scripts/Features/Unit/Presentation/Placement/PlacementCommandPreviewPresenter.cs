using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnitSpec = Features.Unit.Domain.Unit;

namespace Features.Unit.Presentation
{
    internal sealed class PlacementCommandPreviewPresenter
    {
        private readonly Camera _worldCamera;
        private readonly float _screenToPlaneY;
        private readonly PlacementArea _placementArea;
        private readonly PlacementAreaView _placementAreaView;

        public PlacementCommandPreviewPresenter(
            Camera worldCamera,
            float screenToPlaneY,
            PlacementArea placementArea,
            PlacementAreaView placementAreaView)
        {
            _worldCamera = worldCamera;
            _screenToPlaneY = screenToPlaneY;
            _placementArea = placementArea;
            _placementAreaView = placementAreaView;
        }

        public void Show(UnitSpec unit, Vector3 worldPosition)
        {
            if (unit == null)
                return;

// csharp-guardrails: allow-null-defense
            var previewPosition = _placementArea != null
                ? _placementArea.ClampToBounds(worldPosition)
                : worldPosition;

// csharp-guardrails: allow-null-defense
            _placementAreaView?.ShowUnitPreview(
                previewPosition,
                unit.FinalAnchorRange,
                unit.FinalRange);
        }

        public void UpdateFromPointer(UnitSpec unit)
        {
            if (unit == null || !TryResolvePointerScreenPosition(out var screenPosition, out var pointerId))
                return;

            if (IsPointerOverUi(pointerId))
                return;

            var worldPosition = ScreenToWorldPosition(screenPosition);
// csharp-guardrails: allow-null-defense
            var isValid = _placementArea == null || _placementArea.Contains(worldPosition);
            Show(unit, worldPosition);
// csharp-guardrails: allow-null-defense
            _placementAreaView?.SetHighlight(isValid);
        }

        public bool TryConsumeTouchPress(out Vector3 worldPosition, out bool shouldConfirm)
        {
            worldPosition = Vector3.zero;
            shouldConfirm = false;

            var touchscreen = Touchscreen.current;
// csharp-guardrails: allow-null-defense
            if (touchscreen == null)
                return false;

            var touch = touchscreen.primaryTouch;
            if (!touch.press.wasPressedThisFrame)
                return false;

            var touchId = touch.touchId.ReadValue();
            if (IsPointerOverUi(touchId))
                return true;

            worldPosition = ScreenToWorldPosition(touch.position.ReadValue());
            shouldConfirm = true;
            return true;
        }

        public bool TryConsumePlacementPress(out Vector3 worldPosition, out bool shouldConfirm)
        {
            if (TryConsumeTouchPress(out worldPosition, out shouldConfirm))
                return true;

            return TryConsumeMousePress(out worldPosition, out shouldConfirm);
        }

        public bool TryConsumeMousePress(out Vector3 worldPosition, out bool shouldConfirm)
        {
            worldPosition = Vector3.zero;
            shouldConfirm = false;

            var mouse = Mouse.current;
// csharp-guardrails: allow-null-defense
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
                return false;

            if (IsPointerOverUi())
                return true;

            worldPosition = ScreenToWorldPosition(mouse.position.ReadValue());
            shouldConfirm = true;
            return true;
        }

        private Vector3 ScreenToWorldPosition(Vector2 screenPosition)
        {
            return PlacementScreenProjector.ToWorld(_worldCamera, _screenToPlaneY, screenPosition);
        }

        private static bool TryResolvePointerScreenPosition(out Vector2 screenPosition, out int pointerId)
        {
            var touchscreen = Touchscreen.current;
// csharp-guardrails: allow-null-defense
            if (touchscreen != null)
            {
                var touch = touchscreen.primaryTouch;
                if (touch.press.isPressed)
                {
                    screenPosition = touch.position.ReadValue();
                    pointerId = touch.touchId.ReadValue();
                    return true;
                }
            }

            var mouse = Mouse.current;
// csharp-guardrails: allow-null-defense
            if (mouse != null)
            {
                screenPosition = mouse.position.ReadValue();
                pointerId = -1;
                return true;
            }

            screenPosition = default;
            pointerId = -1;
            return false;
        }

        private static bool IsPointerOverUi(int pointerId = -1)
        {
// csharp-guardrails: allow-null-defense
            if (EventSystem.current == null)
                return false;

            return pointerId >= 0
                ? EventSystem.current.IsPointerOverGameObject(pointerId)
                : EventSystem.current.IsPointerOverGameObject();
        }
    }
}
