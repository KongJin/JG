using Features.Unit.Domain;
using Shared.Attributes;
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
    public sealed class UnitSlotInputHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("Configuration")]
        [Required, SerializeField] private Camera _worldCamera;
        [SerializeField] private float _screenToPlaneY = 0f;

        [Header("Drag")]
        [Tooltip("드래그 고스트 Prefab (RectTransform + Image + CanvasGroup).")]
        [Required, SerializeField] private GameObject _dragGhostPrefab;

        [Header("Animation")]
        [Tooltip("드래그 고스트 Prefab의 CanvasGroup (Prefab 내부에서 연결됨).")]
        [Required, SerializeField] private CanvasGroup _ghostCanvasGroup;
        [SerializeField] private Vector2 _dragGhostSize = new Vector2(80f, 80f);

        [Header("Placement")]
        [Tooltip("배치 영역 판정용 PlacementArea 참조.")]
        [SerializeField] private PlacementArea _placementArea;
        [Tooltip("배치 실패 시 에러 표시 UI View.")]
        [SerializeField] private PlacementErrorView _errorView;

        private UnitSpec _unitSpec;
        private System.Action<UnitSpec, Float3> _onSummonRequested;
        private Canvas _canvas;

        private RectTransform _dragGhost;
        private CanvasGroup _canvasGroup;

        /// <summary>
        /// 드래그 고스트 Prefab에 포함된 CanvasGroup을 검증합니다.
        /// Awake 또는 Initialize에서 호출되어야 합니다.
        /// </summary>
        public void ValidateGhostPrefab()
        {
            if (_dragGhostPrefab == null) return;

            var ghostCanvasGroup = _dragGhostPrefab.GetComponent<CanvasGroup>();
            if (ghostCanvasGroup == null)
            {
                Debug.LogError($"[UnitSlotInputHandler] DragGhostPrefab '{_dragGhostPrefab.name}'에 CanvasGroup이 없습니다. Prefab에 CanvasGroup을 추가하세요.");
            }
            else
            {
                _ghostCanvasGroup = ghostCanvasGroup;
            }
        }
        private bool _isDragging;
        private bool _isInPlacementZone;

        public void Initialize(
            UnitSpec unitSpec,
            System.Action<UnitSpec, Float3> onSummonRequested,
            Canvas canvas,
            Camera worldCamera,
            PlacementArea placementArea,
            PlacementErrorView errorView)
        {
            _unitSpec = unitSpec;
            _onSummonRequested = onSummonRequested;
            _canvas = canvas;
            _worldCamera = worldCamera;
            _placementArea = placementArea;
            _errorView = errorView;
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
            _errorView?.ShowError("배치 영역 밖");
        }

        private void CreateDragGhost()
        {
            if (_canvas == null) return;
            if (_dragGhostPrefab == null) return;

            // Prefab 기반으로 인스턴스화
            var ghostGo = Instantiate(_dragGhostPrefab, _canvas.transform);
            ghostGo.name = "DragGhost";
            _dragGhost = ghostGo.GetComponent<RectTransform>();
            _canvasGroup = ghostGo.GetComponent<CanvasGroup>();

            // Prefab에 CanvasGroup이 필수적으로 포함되어야 함
            if (_canvasGroup == null)
            {
                Debug.LogError($"[UnitSlotInputHandler] DragGhostPrefab '{_dragGhostPrefab.name}'에 CanvasGroup이 없습니다. Prefab에 CanvasGroup을 추가하세요.");
                return;
            }

            _canvasGroup.alpha = 0.7f;

            // 슬롯 이미지 스프라이트 복제
            if (_dragGhost.TryGetComponent<UnityEngine.UI.Image>(out var ghostImage) &&
                TryGetComponent<UnityEngine.UI.Image>(out var slotImage))
            {
                ghostImage.sprite = slotImage.sprite;
            }

            if (_dragGhost != null)
            {
                _dragGhost.SetAsLastSibling();
            }
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
