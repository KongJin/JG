using Shared.Runtime.Pooling;
using UnityEngine;

namespace Shared.Runtime.Sound
{
    public sealed class PooledAudioSource : MonoBehaviour
    {
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private LifetimeRelease lifetimeRelease;

        public AudioSource AudioSource => audioSource;
        public LifetimeRelease LifetimeRelease => lifetimeRelease;
    }
}
