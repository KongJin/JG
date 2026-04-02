using System;
using System.Collections.Generic;
using System.Linq;

namespace Features.Skill.Domain
{
    public sealed class SkillUpgradeLevel
    {
        public const int MaxLevel = 3;

        private static readonly Dictionary<GrowthAxis, float[]> MultiplierTable = new()
        {
            { GrowthAxis.Count, new[] { 1f, 2f, 3f, 4f } },
            { GrowthAxis.Range, new[] { 1f, 1.4f, 1.8f, 2.2f } },
            { GrowthAxis.Duration, new[] { 1f, 1.4f, 1.8f, 2.2f } },
            { GrowthAxis.Safety, new[] { 1f, 1.4f, 1.8f, 2.2f } },
        };

        private readonly Dictionary<string, Dictionary<GrowthAxis, int>> _levels = new();

        public int GetLevel(string skillId, GrowthAxis axis)
        {
            if (_levels.TryGetValue(skillId, out var axes) && axes.TryGetValue(axis, out var level))
                return level;
            return 0;
        }

        public bool Increment(string skillId, GrowthAxis axis, IReadOnlyCollection<GrowthAxis> allowedAxes)
        {
            if (allowedAxes == null || !allowedAxes.Contains(axis))
                return false;

            var current = GetLevel(skillId, axis);
            if (current >= MaxLevel) return false;

            if (!_levels.ContainsKey(skillId))
                _levels[skillId] = new Dictionary<GrowthAxis, int>();

            _levels[skillId][axis] = current + 1;
            return true;
        }

        public float GetMultiplier(string skillId, GrowthAxis axis)
        {
            var level = GetLevel(skillId, axis);
            return MultiplierTable[axis][level];
        }

        public float GetAllyDamageScale(string skillId)
        {
            var safetyMultiplier = GetMultiplier(skillId, GrowthAxis.Safety);
            return safetyMultiplier > 1f ? 1f / safetyMultiplier : 1f;
        }

        public bool CanUpgrade(string skillId, GrowthAxis axis)
        {
            return GetLevel(skillId, axis) < MaxLevel;
        }
    }
}
