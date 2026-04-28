using UnityEngine;

namespace Shared.Runtime
{
    public static class ComponentAccess
    {
        public static T Get<T>(GameObject gameObject) where T : Component
        {
            return gameObject != null ? gameObject.GetComponent<T>() : null;
        }

        public static T Ensure<T>(GameObject gameObject) where T : Component
        {
            if (gameObject == null)
                return null;

            var component = gameObject.GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
        }

        public static T InstantiateComponent<T>(
            GameObject prefab,
            Vector3 position,
            Quaternion rotation)
            where T : Component
        {
            if (prefab == null)
                return null;

            var instance = Object.Instantiate(prefab, position, rotation);
            return Get<T>(instance);
        }

        public static T InstantiateComponent<T>(
            GameObject prefab,
            Transform parent,
            bool instantiateInWorldSpace = false)
            where T : Component
        {
            if (prefab == null)
                return null;

            var instance = Object.Instantiate(prefab, parent, instantiateInWorldSpace);
            return Get<T>(instance);
        }
    }
}
