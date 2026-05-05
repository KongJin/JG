using UnityEngine;

namespace Features.Player.Presentation
{
    public sealed class CameraFollower : MonoBehaviour
    {
        private Transform _target;
        private Vector3 _offset;

        public void Initialize(Transform target, Vector3 offset)
        {
            _target = target;
            _offset = offset;
        }

        private void LateUpdate()
        {
// csharp-guardrails: allow-null-defense
            if (_target == null) return;
        }
    }
}
