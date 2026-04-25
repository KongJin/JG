using Shared.Attributes;
using System;
using UnityEngine;

namespace Shared.Runtime.Sound
{
    [Serializable]
    public sealed class SoundEntry
    {
        [SerializeField] private string key;
        [Required, SerializeField] private AudioClip clip;
        [SerializeField] [Range(0f, 1f)] private float volume = 1f;
        [SerializeField] [Range(0f, 1f)] private float spatialBlend = 1f;
        [SerializeField] private float cooldown = 0.05f;
        [SerializeField] private SoundChannel channel = SoundChannel.Sfx;
        [SerializeField] private bool loop;

        public SoundEntry(
            string key,
            AudioClip clip,
            float volume = 1f,
            float spatialBlend = 1f,
            float cooldown = 0.05f,
            SoundChannel channel = SoundChannel.Sfx,
            bool loop = false)
        {
            this.key = key;
            this.clip = clip;
            this.volume = Mathf.Clamp01(volume);
            this.spatialBlend = Mathf.Clamp01(spatialBlend);
            this.cooldown = Mathf.Max(0f, cooldown);
            this.channel = channel;
            this.loop = loop;
        }

        public string Key => key;
        public AudioClip Clip => clip;
        public float Volume => volume;
        public float SpatialBlend => spatialBlend;
        public float Cooldown => cooldown;
        public SoundChannel Channel => channel;
        public bool Loop => loop;
    }
}
