using System;
using System.Collections.Generic;
using Features.Skill.Domain;
using UnityEngine;

namespace Features.Skill.Infrastructure
{
    [Serializable]
    public sealed class GrowthAxisConfig
    {
        [SerializeField] private bool countEnabled;
        [SerializeField] private bool rangeEnabled;
        [SerializeField] private bool durationEnabled;
        [SerializeField] private bool safetyEnabled;

        public bool IsEnabled(GrowthAxis axis)
        {
            switch (axis)
            {
                case GrowthAxis.Count: return countEnabled;
                case GrowthAxis.Range: return rangeEnabled;
                case GrowthAxis.Duration: return durationEnabled;
                case GrowthAxis.Safety: return safetyEnabled;
                default: throw new ArgumentOutOfRangeException(nameof(axis), axis, null);
            }
        }

        public List<GrowthAxis> GetEnabledAxes()
        {
            var axes = new List<GrowthAxis>(4);
            if (countEnabled) axes.Add(GrowthAxis.Count);
            if (rangeEnabled) axes.Add(GrowthAxis.Range);
            if (durationEnabled) axes.Add(GrowthAxis.Duration);
            if (safetyEnabled) axes.Add(GrowthAxis.Safety);
            return axes;
        }
    }
}
