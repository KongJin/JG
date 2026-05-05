using UnityEngine;

namespace Shared.Runtime.Pooling
{
    public interface IPoolBindingHandler
    {
        void OnBindToPool(PooledObject pooledObject);
    }

    public sealed class PooledObject : MonoBehaviour
    {
        private GameObjectPool _owner;

// csharp-guardrails: allow-null-defense
        public bool HasOwner => _owner != null;

        internal void Bind(GameObjectPool owner)
        {
            _owner = owner;
        }

        public void Release()
        {
// csharp-guardrails: allow-null-defense
            if (_owner != null)
            {
                _owner.Return(gameObject);
                return;
            }

            Destroy(gameObject);
        }
    }

}
