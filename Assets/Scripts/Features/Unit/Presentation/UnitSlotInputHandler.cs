using Features.Unit.Domain;
using Shared.Attributes;
using Shared.EventBus;
using Shared.Math;
using UnityEngine;
using UnityEngine.EventSystems;
using UnitSpec = Features.Unit.Domain.Unit;

namespace Features.Unit.Presentation
{
    /// <summary>
    /// 유닛 슬롯 입력 처리 (클릭 + 드래그 앤 드롭).
    /// UnitSlotView와 함께 같은 GO에 붙이거나, 별도 컴포넌트로 초기화.
    /// </summary>
    public sealed class UnitSlotInputHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        [Header("Configuration")]
        [Required, SerializeField] private Camera _worldCamera;
        [SerializeField] private float _screenToPlaneY = 0f;

        [Header("Drag")]
        [SerializeField] private Vector2 _dragGhostSize = new Vector2(80f, 80f);

        [Header("Placement")]
        [Tooltip("배치 영역 판정용 PlacementArea 참조.")]
        [SerializeField] private PlacementArea _placementArea;
        [Tooltip("배치 실패 시 에러 표시 UI View.")]
        [SerializeField] private PlacementErrorView _errorView;

        private UnitSpec _unitSpec;
        private System.Action<UnitSpec, Float3> _onSummonRequested;
        private System.Action<UnitSpec> _onClickRequested;
        private Canvas _canvas;

        private RectTransform _dragGhost;
        private CanvasGroup _canvasGroup;
        private bool _isDragging;
        private bool _isInPlacementZone;

        public void Initialize(
            UnitSpec unitSpec,
            IEventSubscriber eventBus,
            System.Action<UnitSpec, Float3> onSummonRequested,
            System.Action<UnitSpec> onClickRequested,
            Canvas canvas,
            Camera worldCamera,
            PlacementArea placementArea,
            PlacementErrorView errorView)
        {
            _unitSpec = unitSpec;
            _onSummonRequested = onSummonRequested;
            _onClickRequested = onClickRequested;
            _canvas = canvas;
            _worldCamera = worldCamera;
            _placementArea = placementArea;
            _errorView = errorView;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_isDragging) return;
            _onClickRequested?.Invoke(_unitSpec);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_unitSpec == null) return;
            _isDragging = true;
            _isInPlacementZone = false;
            CreateDragGhost();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging || _dragGhost == null) return;
            _dragGhost.position = eventData.position;

            // 배치 영역 유효성 체크
            var worldPos = ScreenToWorldPosition(eventData.position);
            _isInPlacementZone = _placementArea?.Contains(worldPos) ?? false;

            // 고스트 색상 피드백
            UpdateDragGhostColor(_isInPlacementZone);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_isDragging) return;
            _isDragging = false;

            var worldPos = ScreenToWorldPosition(eventData.position);
            var isInZone = _placementArea?.Contains(worldPos) ?? false;

            DestroyDragGhost();

            if (isInZone && _unitSpec != null)
            {
                var finalPos = _placementArea != null ? _placementArea.ClampToBounds(worldPos) : worldPos;
                _onSummonRequested?.Invoke(_unitSpec, new Float3(finalPos.x, finalPos.y, finalPos.z));
            }
            else
            {
                // 배치 영역 밖 → 에러 피드백
                OnPlacementFailed();
            }
        }

        /// <summary>
        /// 배치 실패 시 피드백.
        /// </summary>
        private void OnPlacementFailed()
        {
            _errorView?.Show("배치 영역 밖입니다!");
        }

        private void CreateDragGhost()
        {
            if (_canvas == null) return;

            var ghostGo = new GameObject("DragGhost");
            _dragGhost = ghostGo.AddComponent<RectTransform>();
            _canvasGroup = ghostGo.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = 0.7f;

            var ghostImage = ghostGo.AddComponent<UnityEngine.UI.Image>();
            if (TryGetComponent<UnityEngine.UI.Image>(out var slotImage))
            {
                ghostImage.sprite = slotImage.sprite;
            }

            _dragGhost.sizeDelta = _dragGhostSize;
            _dragGhost.SetParent(_canvas.transform, false);
            _dragGhost.SetAsLastSibling();
        }

        private void DestroyDragGhost()
        {
            if (_dragGhost != null)
            {
                Destroy(_dragGhost.gameObject);
                _dragGhost = null;
                _canvasGroup = null;
            }
        }

        private void UpdateDragGhostColor(bool isValid)
        {
            if (_dragGhost == null) return;

            if (_dragGhost.TryGetComponent<UnityEngine.UI.Image>(out var image))
            {
                if (isValid)
                {
                    image.color = new Color(0f, 1f, 0f, 0.8f); // 녹색
                }
                else
                {
                    image.color = new Color(1f, 0f, 0f, 0.8f); // 빨간색
                }
            }
        }

        private Vector3 ScreenToWorldPosition(Vector2 screenPosition)
        {
            if (_worldCamera == null) return Vector3.zero;

            // Plane-Raycast 교차 계산으로 정확한 지면 위치 변환
            var ray = _worldCamera.ScreenPointToRay(new Vector3(screenPosition.x, screenPosition.y, 0f));
            var plane = new Plane(Vector3.up, new Vector3(0, _screenToPlaneY, 0));

            if (plane.Raycast(ray, out float enter))
            {
                return ray.GetPoint(enter);
            }

            return Vector3.zero;
        }
    }
}
