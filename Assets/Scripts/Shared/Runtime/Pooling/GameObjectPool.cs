using System;
using System.Collections.Generic;
using Shared.Runtime;
using UnityEngine;

namespace Shared.Runtime.Pooling
{
    public sealed class GameObjectPool
    {
        private readonly GameObject _prefab;
        private readonly Transform _root;
        private readonly Stack<GameObject> _available = new Stack<GameObject>();
        private readonly HashSet<GameObject> _availableSet = new HashSet<GameObject>();
        private readonly Dictionary<GameObject, PoolInstanceBinding> _bindings =
            new Dictionary<GameObject, PoolInstanceBinding>();

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
            Notify(Bind(instance), onRent: true);
            return instance;
        }

        public T RentComponent<T>(Vector3 position, Quaternion rotation, Transform parentOverride = null)
            where T : Component
        {
            var instance = Rent(position, rotation, parentOverride);
            return ComponentAccess.Get<T>(instance);
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
            first = ComponentAccess.Get<TFirst>(instance);
            second = ComponentAccess.Get<TSecond>(instance);
            return first != null && second != null;
        }

        public void Return(GameObject instance)
        {
            if (instance == null || _availableSet.Contains(instance))
                return;

            var binding = Bind(instance);
            Notify(binding, onRent: false);
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

        private PoolInstanceBinding Bind(GameObject instance)
        {
            if (_bindings.TryGetValue(instance, out var binding))
                return binding;

            var pooledObject = ComponentAccess.Ensure<PooledObject>(instance);
            pooledObject.Bind(this);

            var behaviours = instance.GetComponentsInChildren<MonoBehaviour>(true);
            var resetHandlers = new List<IPoolResetHandler>(behaviours.Length);
            for (var i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IPoolBindingHandler bindingHandler)
                    bindingHandler.OnBindToPool(pooledObject);

                if (behaviours[i] is IPoolResetHandler resetHandler)
                    resetHandlers.Add(resetHandler);
            }

            binding = new PoolInstanceBinding(resetHandlers.ToArray());
            _bindings[instance] = binding;
            return binding;
        }

        private static void Notify(PoolInstanceBinding binding, bool onRent)
        {
            var handlers = binding.ResetHandlers;
            for (var i = 0; i < handlers.Length; i++)
            {
                if (onRent)
                    handlers[i].OnRentFromPool();
                else
                    handlers[i].OnReturnToPool();
            }
        }

        private sealed class PoolInstanceBinding
        {
            public PoolInstanceBinding(IPoolResetHandler[] resetHandlers)
            {
                ResetHandlers = resetHandlers;
            }

            public IPoolResetHandler[] ResetHandlers { get; }
        }
    }
}
