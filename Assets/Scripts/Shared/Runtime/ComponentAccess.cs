using Shared.Kernel;
using UnityEngine;

namespace Shared.Runtime
{
    public static class ComponentAccess
    {
        public static T Get<T>(GameObject gameObject) where T : Component
        {
            // csharp-guardrails: allow-null-defense
            return gameObject != null ? gameObject.GetComponent<T>() : null;
        }

        public static T Ensure<T>(GameObject gameObject) where T : Component
        {
            // csharp-guardrails: allow-null-defense
            if (gameObject == null)
                return null;

            var component = gameObject.GetComponent<T>();
// csharp-guardrails: allow-null-defense
            return component != null ? component : gameObject.AddComponent<T>();
        }

        public static T GetInParent<T>(Component component) where T : Component
        {
// csharp-guardrails: allow-null-defense
            return component != null ? component.GetComponentInParent<T>() : null;
        }

        public static bool TryGetEntityIdHolder(Collider collider, out EntityIdHolder holder)
        {
            holder = GetInParent<EntityIdHolder>(collider);
            return holder != null && holder.IsInitialized;
        }

        public static T InstantiateComponent<T>(
            GameObject prefab,
            Vector3 position,
            Quaternion rotation)
            where T : Component
        {
            // csharp-guardrails: allow-null-defense
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
            // csharp-guardrails: allow-null-defense
            if (prefab == null)
                return null;

            var instance = Object.Instantiate(prefab, parent, instantiateInWorldSpace);
            return Get<T>(instance);
        }
    }
}
