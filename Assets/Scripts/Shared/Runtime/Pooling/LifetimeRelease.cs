using UnityEngine;

namespace Shared.Runtime.Pooling
{
    public sealed class LifetimeRelease : MonoBehaviour, IPoolResetHandler, IPoolBindingHandler
    {
        private PooledObject _pooledObject;
        private bool _isArmed;
        private float _remaining;

        public void OnBindToPool(PooledObject pooledObject)
        {
            _pooledObject = pooledObject;
        }

        public void Arm(float seconds)
        {
            _remaining = seconds;
            _isArmed = seconds > 0f;
        }

        private void Update()
        {
            if (!_isArmed)
                return;

            _remaining -= UnityEngine.Time.deltaTime;
            if (_remaining > 0f)
                return;

            _isArmed = false;
            if (_pooledObject != null)
                _pooledObject.Release();
            else
                Destroy(gameObject);
        }

        public void OnRentFromPool()
        {
            _remaining = 0f;
            _isArmed = false;
        }

        public void OnReturnToPool()
        {
            _remaining = 0f;
            _isArmed = false;
        }
    }
}
