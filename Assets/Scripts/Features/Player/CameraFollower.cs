using UnityEngine;

namespace Features.Player
{
    public sealed class CameraFollower : MonoBehaviour
    {
        private Transform _target;
        private Vector3 _offset;
        private Quaternion _baseRotation;

        public void Initialize(Transform target, Vector3 offset)
        {
            _target = target;
            _offset = offset;
            _baseRotation = transform.rotation;
        }

        private void LateUpdate()
        {
            if (_target == null)
                return;

            transform.position = _target.position + _offset;
            transform.rotation = _baseRotation;
        }
    }
}
