using UnityEngine;

namespace Shared.Runtime.Pooling
{
    public sealed class PooledObject : MonoBehaviour
    {
        private GameObjectPool _owner;

        public bool HasOwner => _owner != null;

        internal void Bind(GameObjectPool owner)
        {
            _owner = owner;
        }

        public void Release()
        {
            if (_owner != null)
            {
                _owner.Return(gameObject);
                return;
            }

            Destroy(gameObject);
        }
    }
}
