using UnityEngine;
using Shared.Attributes;
using Shared.Runtime.Pooling;

namespace Features.Zone.Presentation
{
    public sealed class ZoneView : MonoBehaviour, IPoolResetHandler, IPoolBindingHandler
    {
        [Required, SerializeField] private Renderer _renderer;

        private float _duration;
        private float _elapsed;
        private PooledObject _pooledObject;
        private Vector3 _scale = Vector3.one;
        private Color _baseColor = Color.white;

        public void Initialize(float radius, float duration)
        {
            _duration = duration;
            _elapsed = 0f;
            _scale = new Vector3(radius * 2f, 0.1f, radius * 2f);
        }

        public void SetColor(Color color)
        {
            _baseColor = color;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;

            if (_renderer != null)
            {
                var alpha = _duration > 0f ? Mathf.Lerp(1f, 0f, _elapsed / _duration) : 0f;
                var color = _baseColor;
                color.a = alpha;
            }

            if (_elapsed >= _duration)
                ReleaseSelf();
        }

        public void OnRentFromPool()
        {
            _elapsed = 0f;
        }

        public void OnReturnToPool()
        {
            _elapsed = 0f;
        }

        public void OnBindToPool(PooledObject pooledObject)
        {
            _pooledObject = pooledObject;
        }

        private void ReleaseSelf()
        {
            if (_pooledObject != null)
            {
                _pooledObject.Release();
                return;
            }

            Destroy(gameObject);
        }
    }
}
