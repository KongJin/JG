using Shared.Attributes;
using Shared.Runtime.Pooling;
using UnityEngine;

namespace Features.Skill.Presentation
{
    public sealed class TargetedCastEffect : MonoBehaviour, IPoolResetHandler
    {
        [SerializeField] private float _duration = 0.5f;
        [SerializeField] private Color _flashColor = new Color(1f, 0.2f, 0.2f);
        [SerializeField] private Renderer _renderer;

        private Color _defaultColor;
        private Color _originalColor;
        private float _elapsed;
        private bool _isFlashing;
        private PooledObject _pooledObject;

        private void Awake()
        {
            _pooledObject = GetComponent<PooledObject>();

            if (_renderer != null)
                _defaultColor = _renderer.material.color;
        }

        public void Play()
        {
            _pooledObject ??= GetComponent<PooledObject>();
            if (_renderer == null)
            {
                ReleaseSelf();
                return;
            }

            _originalColor = _defaultColor;
            _elapsed = 0f;
            _isFlashing = true;
        }

        private void Update()
        {
            if (!_isFlashing) return;

            _elapsed += Time.deltaTime;
            if (_elapsed >= _duration)
            {
                _isFlashing = false;
                ReleaseSelf();
            }
        }

        public void OnRentFromPool()
        {
            _elapsed = 0f;
            _isFlashing = false;
            _pooledObject ??= GetComponent<PooledObject>();
        }

        public void OnReturnToPool()
        {
            _elapsed = 0f;
            _isFlashing = false;
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
