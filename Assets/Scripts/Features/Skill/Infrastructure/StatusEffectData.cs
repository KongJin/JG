using System;
using Features.Status.Domain;
using UnityEngine;

namespace Features.Skill.Infrastructure
{
    [Serializable]
    public sealed class StatusEffectData
    {
        [SerializeField] private bool enabled;
        [SerializeField] private StatusType type;
        [SerializeField] private float magnitude;
        [SerializeField] private float duration;
        [SerializeField] private float tickInterval;

        public StatusPayload ToPayload()
        {
            return enabled
                ? StatusPayload.Create(type, magnitude, duration, tickInterval)
                : StatusPayload.None;
        }
    }
}
