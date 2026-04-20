using UnityEngine;

namespace Features.Player.Runtime
{
    public sealed class PlayerHealthHudFollower : MonoBehaviour
    {
        private static readonly Vector2 HiddenAnchoredPosition = new Vector2(100000f, 100000f);

        private Transform _target;
        private Camera _worldCamera;
        private Canvas _hudCanvas;
        private RectTransform _rectTransform;
        private RectTransform _hudCanvasRectTransform;
        private Vector3 _headOffset;

        public void Initialize(Transform target, Camera worldCamera, Canvas hudCanvas, Vector3 headOffset)
        {
            _target = target;
            _worldCamera = worldCamera;
            _hudCanvas = hudCanvas;
            _headOffset = headOffset;
            _rectTransform = transform as RectTransform;
            _hudCanvasRectTransform = hudCanvas != null ? hudCanvas.transform as RectTransform : null;
        }

        private void LateUpdate()
        {
            if (_target == null || _worldCamera == null || _hudCanvas == null || _rectTransform == null || _hudCanvasRectTransform == null)
                return;

            var screenPoint = _worldCamera.WorldToScreenPoint(_target.position + _headOffset);
            if (screenPoint.z <= 0f)
            {
                _rectTransform.anchoredPosition = HiddenAnchoredPosition;
                return;
            }

            var uiCamera = _hudCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : _hudCanvas.worldCamera;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _hudCanvasRectTransform,
                screenPoint,
                uiCamera,
                out var anchoredPosition);

            _rectTransform.anchoredPosition = anchoredPosition;
        }
    }
}
