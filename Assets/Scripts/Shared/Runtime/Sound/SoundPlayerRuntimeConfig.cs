using Shared.Attributes;
using UnityEngine;

namespace Shared.Runtime.Sound
{
    [CreateAssetMenu(fileName = "SoundPlayerRuntimeConfig", menuName = "Shared/Sound/RuntimeConfig")]
    public sealed class SoundPlayerRuntimeConfig : ScriptableObject
    {
        [Required, SerializeField] private SoundCatalog _catalog;

        public SoundCatalog Catalog => _catalog;
    }
}
