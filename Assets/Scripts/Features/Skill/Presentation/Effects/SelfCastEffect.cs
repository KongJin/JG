using Shared.Attributes;
using Shared.Runtime.Pooling;
using UnityEngine;

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
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;

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
