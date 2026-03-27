using UnityEngine;
using Shared.Runtime.Pooling;

namespace Features.Zone.Presentation
{
    public sealed class ZoneView : MonoBehaviour, IPoolResetHandler
    {
        private float _duration;
        private float _elapsed;
        private Renderer _renderer;
        private PooledObject _pooledObject;
        private Vector3 _scale = Vector3.one;
        private Color _baseColor = Color.white;

        public void Initialize(float radius, float duration)
        {
            _duration = duration;
            _elapsed = 0f;
            _scale = new Vector3(radius * 2f, 0.1f, radius * 2f);
            transform.localScale = _scale;

            _renderer = GetComponentInChildren<Renderer>();
            _pooledObject ??= GetComponent<PooledObject>();
        }

        public void SetColor(Color color)
        {
            _baseColor = color;
            if (_renderer != null)
                _renderer.material.color = _baseColor;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;

            if (_renderer != null)
            {
                var alpha = _duration > 0f ? Mathf.Lerp(1f, 0f, _elapsed / _duration) : 0f;
                var color = _baseColor;
                color.a = alpha;
                _renderer.material.color = color;
            }

            if (_elapsed >= _duration)
                ReleaseSelf();
        }

        public void OnRentFromPool()
        {
            _elapsed = 0f;
            transform.localScale = _scale;

            if (_renderer == null)
                _renderer = GetComponentInChildren<Renderer>();

            if (_renderer != null)
                _renderer.material.color = _baseColor;
        }

        public void OnReturnToPool()
        {
            _elapsed = 0f;
        }

        private void ReleaseSelf()
        {
            _pooledObject ??= GetComponent<PooledObject>();
            if (_pooledObject != null)
            {
                _pooledObject.Release();
                return;
            }

            Destroy(gameObject);
        }
    }
}
