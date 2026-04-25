using Shared.Attributes;
using UnityEngine;

namespace Shared.Runtime.Sound
{
    [CreateAssetMenu(fileName = "SoundPlayerRuntimeConfig", menuName = "Shared/Sound/RuntimeConfig")]
    public sealed class SoundPlayerRuntimeConfig : ScriptableObject
    {
        [Required, SerializeField] private SoundCatalog _catalog;
        [SerializeField] private int _initialPoolSize = 8;

        public SoundCatalog Catalog => _catalog;
        public int InitialPoolSize => _initialPoolSize;
    }
}
