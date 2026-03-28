using System;
using UnityEngine;

namespace Shared.Runtime.Sound
{
    [Serializable]
    public sealed class SoundEntry
    {
        [SerializeField] private string key;
        [SerializeField] private AudioClip clip;
        [SerializeField] [Range(0f, 1f)] private float volume = 1f;
        [SerializeField] [Range(0f, 1f)] private float spatialBlend = 1f;
        [SerializeField] private float cooldown = 0.05f;

        public string Key => key;
        public AudioClip Clip => clip;
        public float Volume => volume;
        public float SpatialBlend => spatialBlend;
        public float Cooldown => cooldown;
    }
}
