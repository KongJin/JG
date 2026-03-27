using UnityEngine;
using Shared.Runtime.Pooling;

namespace Features.Skill.Presentation
{
    public sealed class SelfCastEffect : MonoBehaviour, IPoolResetHandler
    {
        [SerializeField] private float _duration = 1f;
        [SerializeField] private Color _effectColor = new Color(0.3f, 1f, 0.4f, 0.5f);

        private float _elapsed;
        private Renderer _renderer;
        private PooledObject _pooledObject;
        private Vector3 _initialScale = Vector3.one;

        private void Awake()
        {
            _renderer = GetComponentInChildren<Renderer>();
            _pooledObject = GetComponent<PooledObject>();
            _initialScale = transform.localScale;
        }

        public void Play()
        {
            _elapsed = 0f;

            if (_renderer != null)
                _renderer.material.color = _effectColor;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;

            var scale = Mathf.Lerp(1f, 1.5f, _elapsed / _duration);
            transform.localScale = _initialScale * scale;

            if (_elapsed >= _duration)
                ReleaseSelf();
        }

        public void OnRentFromPool()
        {
            _elapsed = 0f;
            transform.localScale = _initialScale;

            if (_renderer != null)
                _renderer.material.color = _effectColor;
        }

        public void OnReturnToPool()
        {
            _elapsed = 0f;
            transform.localScale = _initialScale;
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
