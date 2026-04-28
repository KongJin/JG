using UnityEngine;

namespace Features.Player.Runtime
{
    public sealed class PlayerHealthHudFollower : MonoBehaviour
    {
        private Transform _target;
        private Camera _worldCamera;
        private Vector3 _headOffset;

        public Vector3 ScreenPosition { get; private set; }
        public bool IsVisible { get; private set; }

        public void Initialize(Transform target, Camera worldCamera, Vector3 headOffset)
        {
            _target = target;
            _worldCamera = worldCamera;
            _headOffset = headOffset;
        }

        private void LateUpdate()
        {
            if (_target == null || _worldCamera == null)
                return;

            ScreenPosition = _worldCamera.WorldToScreenPoint(_target.position + _headOffset);
            IsVisible = ScreenPosition.z > 0f;
        }
    }
}
