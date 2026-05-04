using System;
using System.Collections.Generic;
using System.Linq;
using Features.Skill.Application.Ports;
using Features.Skill.Domain;

namespace Features.Skill.Application
{
    public sealed class SkillUpgradeAdapter : ISkillUpgradePort
    {
        private readonly SkillUpgradeLevel _upgrades;
        private readonly Func<string, IReadOnlyCollection<GrowthAxis>> _getEnabledAxes;

        public SkillUpgradeAdapter(
            SkillUpgradeLevel upgrades,
            Func<string, IReadOnlyCollection<GrowthAxis>> getEnabledAxes)
        {
            _upgrades = upgrades;
            _getEnabledAxes = getEnabledAxes;
        }

        public float GetAxisMultiplier(string skillId, GrowthAxis axis)
        {
            var enabled = _getEnabledAxes(skillId);
            if (enabled == null || !enabled.Contains(axis))
                return 1f;

            return _upgrades.GetMultiplier(skillId, axis);
        }

        public float GetAllyDamageScale(string skillId)
        {
            var enabled = _getEnabledAxes(skillId);
            if (enabled == null || !enabled.Contains(GrowthAxis.Safety))
                return 1f;

            return _upgrades.GetAllyDamageScale(skillId);
        }

        public bool TryUpgrade(string skillId, GrowthAxis axis)
        {
            var enabled = _getEnabledAxes(skillId);
            if (enabled == null)
                return false;

            return _upgrades.Increment(skillId, axis, enabled);
        }

        public int GetLevel(string skillId, GrowthAxis axis)
        {
            return _upgrades.GetLevel(skillId, axis);
        }

        public bool CanUpgrade(string skillId, GrowthAxis axis)
        {
            var enabled = _getEnabledAxes(skillId);
            if (enabled == null || !enabled.Contains(axis))
                return false;

            return _upgrades.CanUpgrade(skillId, axis);
        }
    }
}
