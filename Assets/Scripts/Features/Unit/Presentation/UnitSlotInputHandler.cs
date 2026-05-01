using Features.Unit.Domain;
using Shared.Math;
using UnityEngine;
using UnityEngine.EventSystems;
using UnitSpec = Features.Unit.Domain.Unit;

namespace Features.Unit.Presentation
{
    public sealed class UnitSlotInputHandler : MonoBehaviour
    {
        [SerializeField] private Camera _worldCamera;
        [SerializeField] private float _screenToPlaneY = 0f;
        [SerializeField] private PlacementArea _placementArea;
        [SerializeField] private PlacementErrorView _errorView;
        [SerializeField] private PlacementAreaView _placementAreaView;

        private UnitSpec _unitSpec;
        private System.Action<UnitSpec, Float3> _onSummonRequested;

        public bool IsDragging { get; private set; }
        public bool IsInPlacementZone { get; private set; }

        public void Initialize(
            UnitSpec unitSpec,
            System.Action<UnitSpec, Float3> onSummonRequested,
            Camera worldCamera,
            PlacementArea placementArea,
            PlacementErrorView errorView,
            PlacementAreaView placementAreaView = null)
        {
            _unitSpec = unitSpec;
            _onSummonRequested = onSummonRequested;
            _worldCamera = worldCamera;
            _placementArea = placementArea;
            _errorView = errorView;
            _placementAreaView = placementAreaView;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData != null)
                BeginDrag(eventData.position);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData != null)
                Drag(eventData.position);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (eventData != null)
                EndDrag(eventData.position);
        }

        public void BeginDrag(Vector2 screenPosition)
        {
            if (_unitSpec == null)
                return;

            IsDragging = true;
            UpdatePlacementPreview(screenPosition);
        }

        public void Drag(Vector2 screenPosition)
        {
            if (!IsDragging)
                return;

            var worldPos = ScreenToWorldPosition(screenPosition);
            IsInPlacementZone = _placementArea?.Contains(worldPos) ?? false;
            UpdatePlacementPreview(screenPosition);
            _placementAreaView?.SetHighlight(IsInPlacementZone);
        }

        public void EndDrag(Vector2 screenPosition)
        {
            if (!IsDragging)
                return;

            IsDragging = false;
            var worldPos = ScreenToWorldPosition(screenPosition);
            var isInZone = _placementArea?.Contains(worldPos) ?? false;
            if (isInZone && _unitSpec != null)
            {
                var finalPos = _placementArea != null ? _placementArea.ClampToBounds(worldPos) : worldPos;
                _onSummonRequested?.Invoke(_unitSpec, new Float3(finalPos.x, finalPos.y, finalPos.z));
                _placementAreaView?.HideUnitPreview();
                return;
            }

            _errorView?.ShowError("배치 영역 밖");
            _placementAreaView?.HideUnitPreview();
            _placementAreaView?.ShowInvalidPlacementFeedback();
        }

        private void UpdatePlacementPreview(Vector2 screenPosition)
        {
            if (_unitSpec == null)
                return;

            var worldPos = ScreenToWorldPosition(screenPosition);
            var previewPos = _placementArea != null ? _placementArea.ClampToBounds(worldPos) : worldPos;
            _placementAreaView?.ShowUnitPreview(previewPos, _unitSpec.FinalAnchorRange, _unitSpec.FinalRange);
        }

        private Vector3 ScreenToWorldPosition(Vector2 screenPosition)
        {
            return PlacementScreenProjector.ToWorld(_worldCamera, _screenToPlaneY, screenPosition);
        }
    }
}
