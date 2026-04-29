using System.Collections.Generic;
using UnityEngine;

namespace Shared.Kernel
{
    public sealed class EntityIdHolder : MonoBehaviour
    {
        private static readonly Dictionary<DomainEntityId, EntityIdHolder> Registry
            = new Dictionary<DomainEntityId, EntityIdHolder>();

        private DomainEntityId _id;

        public DomainEntityId Id => _id;
        public bool IsInitialized { get; private set; }

        public static bool TryGet(DomainEntityId id, out EntityIdHolder holder)
        {
            return Registry.TryGetValue(id, out holder);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            Registry.Clear();
        }

        public void Set(DomainEntityId id)
        {
            _id = id;
            IsInitialized = true;
            Registry[id] = this;
        }

        private void OnDestroy()
        {
            if (IsInitialized)
                Registry.Remove(_id);
        }
    }
}
