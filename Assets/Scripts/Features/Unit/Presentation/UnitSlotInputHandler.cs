using Features.Unit.Domain;
using Shared.EventBus;
using Shared.Math;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Features.Unit.Presentation
{
    /// <summary>
    /// 유닛 슬롯 입력 처리 (클릭 + 드래그 앤 드롭).
    /// </summary>
    public sealed class UnitSlotInputHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        private UnitSlotView _slotView;
        private IEventBus _eventBus;
        private System.Action<Unit, Float3> _onSummonRequested;

        private Canvas _canvas;
        private RectTransform _dragGhost;
        private CanvasGroup _canvasGroup;
        private bool _isDragging;

        public void Initialize(
            UnitSlotView slotView,
            IEventBus eventBus,
            System.Action<Unit, Float3> onSummonRequested,
            Canvas canvas)
        {
            _slotView = slotView;
            _eventBus = eventBus;
            _onSummonRequested = onSummonRequested;
            _canvas = canvas;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_isDragging) return; // 드래그 중이면 클릭 무시

            _slotView.OnClicked();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _isDragging = true;
            CreateDragGhost();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging || _dragGhost == null) return;

            // 드래그 고스트 위치 업데이트
            var rectTransform = _dragGhost.rectTransform;
            rectTransform.position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_isDragging) return;

            _isDragging = false;
            DestroyDragGhost();

            // 배치 가능 영역 확인
            if (IsInDeployZone(eventData.position))
            {
                // 소환 요청 (Float3로 변환)
                var worldPos = ScreenToWorldPosition(eventData.position);
                _onSummonRequested?.Invoke(_slotView.UnitSpec, new Float3(worldPos.x, worldPos.y, worldPos.z));
            }
        }

        private void CreateDragGhost()
        {
            if (_canvas == null) return;

            // 드래그 고스트 생성
            var ghostGo = new GameObject("DragGhost");
            _dragGhost = ghostGo.AddComponent<RectTransform>();
            _canvasGroup = ghostGo.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = 0.7f;

            // 아이콘 복사 (선택사항)
            var ghostImage = ghostGo.AddComponent<UnityEngine.UI.Image>();
            if (_slotView.TryGetComponent<UnityEngine.UI.Image>(out var slotImage))
            {
                ghostImage.sprite = slotImage.sprite;
            }

            _dragGhost.sizeDelta = Vector2.one * 100f;
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
            // TODO: 배치 가능 영역 판정
            // 현재는 임시로 화면 하반부를 배치 영역으로 간주
            var screenHeight = Screen.height;
            return screenPosition.y < screenHeight * 0.5f;
        }

        private Vector3 ScreenToWorldPosition(Vector2 screenPosition)
        {
            // TODO: Camera.main 또는 직렬화 필드 사용
            // 현재는 임시 구현
            var camera = Camera.main;
            if (camera == null) return Vector3.zero;

            screenPosition.z = 10f;
            return camera.ScreenToWorldPoint(screenPosition);
        }
    }
}
