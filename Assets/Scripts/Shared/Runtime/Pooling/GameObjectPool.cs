using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shared.Runtime.Pooling
{
    public sealed class GameObjectPool
    {
        private readonly GameObject _prefab;
        private readonly Transform _root;
        private readonly Stack<GameObject> _available = new Stack<GameObject>();
        private readonly HashSet<GameObject> _availableSet = new HashSet<GameObject>();

        public GameObjectPool(GameObject prefab, Transform root = null, int initialSize = 0)
        {
            _prefab = prefab ?? throw new ArgumentNullException(nameof(prefab));
            _root = root;

            for (var i = 0; i < initialSize; i++)
                Return(CreateInstance());
        }

        public GameObject Rent(Vector3 position, Quaternion rotation, Transform parentOverride = null)
        {
            var instance = _available.Count > 0 ? _available.Pop() : CreateInstance();
            _availableSet.Remove(instance);

            var targetParent = parentOverride != null ? parentOverride : _root;
            if (targetParent != null)
                instance.transform.SetParent(targetParent, false);

            instance.transform.SetPositionAndRotation(position, rotation);
            instance.SetActive(true);
            Notify(instance, onRent: true);
            return instance;
        }

        public T RentComponent<T>(Vector3 position, Quaternion rotation, Transform parentOverride = null)
            where T : Component
        {
            var instance = Rent(position, rotation, parentOverride);
            return instance.GetComponent<T>();
        }

        public bool RentComponents<TFirst, TSecond>(
            Vector3 position,
            Quaternion rotation,
            out TFirst first,
            out TSecond second,
            Transform parentOverride = null)
            where TFirst : Component
            where TSecond : Component
        {
            var instance = Rent(position, rotation, parentOverride);
            first = instance.GetComponent<TFirst>();
            second = instance.GetComponent<TSecond>();
            return first != null && second != null;
        }

        public void Return(GameObject instance)
        {
            if (instance == null || _availableSet.Contains(instance))
                return;

            Bind(instance);
            Notify(instance, onRent: false);
            instance.SetActive(false);

            if (_root != null)
                instance.transform.SetParent(_root, false);

            _available.Push(instance);
            _availableSet.Add(instance);
        }

        private GameObject CreateInstance()
        {
            var instance = _root == null
                ? UnityEngine.Object.Instantiate(_prefab)
                : UnityEngine.Object.Instantiate(_prefab, _root);

            instance.name = _prefab.name;
            Bind(instance);
            instance.SetActive(false);
            return instance;
        }

        private void Bind(GameObject instance)
        {
            var pooledObject = instance.GetComponent<PooledObject>();
            if (pooledObject == null)
                pooledObject = instance.AddComponent<PooledObject>();

            pooledObject.Bind(this);

            var behaviours = instance.GetComponentsInChildren<MonoBehaviour>(true);
            for (var i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IPoolBindingHandler bindingHandler)
                    bindingHandler.OnBindToPool(pooledObject);
            }
        }

        private static void Notify(GameObject instance, bool onRent)
        {
            var behaviours = instance.GetComponentsInChildren<MonoBehaviour>(true);
            for (var i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is not IPoolResetHandler handler)
                    continue;

                if (onRent)
                    handler.OnRentFromPool();
                else
                    handler.OnReturnToPool();
            }
        }
    }
}
