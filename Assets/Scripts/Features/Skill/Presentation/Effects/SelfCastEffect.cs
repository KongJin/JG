using Shared.Attributes;
using Shared.Runtime.Pooling;
using UnityEngine;

namespace Features.Skill.Presentation
{
    public sealed class SelfCastEffect : MonoBehaviour, IPoolResetHandler, IPoolBindingHandler
    {
        [SerializeField] private float _duration = 1f;
// csharp-guardrails: allow-serialized-field-without-required
        [SerializeField] private Color _effectColor = new Color(0.3f, 1f, 0.4f, 0.5f);
// csharp-guardrails: allow-serialized-field-without-required
        [SerializeField] private Renderer _renderer;

        private float _elapsed;
        private PooledObject _pooledObject;
        private Vector3 _initialScale = Vector3.one;

        private void Awake()
        {
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

        public void OnBindToPool(PooledObject pooledObject)
        {
            _pooledObject = pooledObject;
        }

        private void ReleaseSelf()
        {
// csharp-guardrails: allow-null-defense
            if (_pooledObject != null)
            {
                _pooledObject.Release();
                return;
            }

            Destroy(gameObject);
        }
    }
}
