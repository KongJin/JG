using Shared.Attributes;
using Shared.Runtime.Pooling;
using UnityEngine;

namespace Shared.Runtime.Sound
{
    public sealed class PooledAudioSource : MonoBehaviour
    {
        [Required, SerializeField] private AudioSource audioSource;
        [Required, SerializeField] private LifetimeRelease lifetimeRelease;

        public AudioSource AudioSource => audioSource;
        public LifetimeRelease LifetimeRelease => lifetimeRelease;
    }
}
