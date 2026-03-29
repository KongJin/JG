using Shared.Attributes;
using System.Collections.Generic;
using UnityEngine;

namespace Shared.Runtime.Sound
{
    [CreateAssetMenu(fileName = "SoundCatalog", menuName = "Shared/SoundCatalog")]
    public sealed class SoundCatalog : ScriptableObject
    {
        [Required, SerializeField] private SoundEntry[] entries;

        private Dictionary<string, SoundEntry> _lookup;

        public SoundEntry Get(string key)
        {
            if (_lookup == null)
                BuildLookup();

            _lookup.TryGetValue(key, out var entry);
            return entry;
        }

        private void BuildLookup()
        {
            _lookup = new Dictionary<string, SoundEntry>();
            if (entries == null)
                return;

            foreach (var e in entries)
            {
                if (e == null || string.IsNullOrEmpty(e.Key))
                    continue;

                _lookup[e.Key] = e;
            }
        }
    }
}
