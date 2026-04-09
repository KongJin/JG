using Features.Unit.Domain;
using Shared.Attributes;
using Shared.EventBus;
using Shared.Math;
using UnityEngine;
using UnityEngine.EventSystems;

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

        private Unit _unitSpec;
        private IEventBus _eventBus;
        private System.Action<Unit, Float3> _onSummonRequested;
        private System.Action<Unit> _onClickRequested;
        private Canvas _canvas;

        private RectTransform _dragGhost;
        private CanvasGroup _canvasGroup;
        private bool _isDragging;

        public void Initialize(
            Unit unitSpec,
            IEventBus eventBus,
            System.Action<Unit, Float3> onSummonRequested,
            System.Action<Unit> onClickRequested,
            Canvas canvas,
            Camera worldCamera)
        {
            _unitSpec = unitSpec;
            _eventBus = eventBus;
            _onSummonRequested = onSummonRequested;
            _onClickRequested = onClickRequested;
            _canvas = canvas;
            _worldCamera = worldCamera;
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
            CreateDragGhost();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging || _dragGhost == null) return;
            _dragGhost.position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_isDragging) return;
            _isDragging = false;
            DestroyDragGhost();

            if (IsInDeployZone(eventData.position) && _unitSpec != null)
            {
                var worldPos = ScreenToWorldPosition(eventData.position);
                _onSummonRequested?.Invoke(_unitSpec, new Float3(worldPos.x, worldPos.y, worldPos.z));
            }
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

        private bool IsInDeployZone(Vector2 screenPosition)
        {
            // 화면 하반부를 배치 영역으로 간주
            return screenPosition.y < Screen.height * 0.5f;
        }

        private Vector3 ScreenToWorldPosition(Vector2 screenPosition)
        {
            if (_worldCamera == null) return Vector3.zero;

            var ray = _worldCamera.ScreenPointToRay(new Vector3(screenPosition.x, screenPosition.y, 0f));
            var t = (_screenToPlaneY - ray.origin.y) / ray.direction.y;
            return ray.GetPoint(t);
        }
    }
}
