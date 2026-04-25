using Shared.Attributes;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Shared.Runtime.Sound
{
    [CreateAssetMenu(fileName = "SoundCatalog", menuName = "Shared/SoundCatalog")]
    public sealed class SoundCatalog : ScriptableObject
    {
        [Required, SerializeField] private SoundEntry[] entries;

        private Dictionary<string, SoundEntry> _lookup;

        public IReadOnlyList<SoundEntry> Entries => entries;

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

        public string[] GetDuplicateKeys()
        {
            if (entries == null)
                return System.Array.Empty<string>();

            return entries
                .Where(e => e != null && !string.IsNullOrEmpty(e.Key))
                .GroupBy(e => e.Key)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToArray();
        }
    }
}
