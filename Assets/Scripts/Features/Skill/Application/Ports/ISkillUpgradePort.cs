using Features.Skill.Domain;

namespace Features.Skill.Application.Ports
{
    public interface ISkillUpgradePort
    {
        float GetAxisMultiplier(string skillId, GrowthAxis axis);
        float GetAllyDamageScale(string skillId);
        int GetLevel(string skillId, GrowthAxis axis);
        bool TryUpgrade(string skillId, GrowthAxis axis);
        bool CanUpgrade(string skillId, GrowthAxis axis);
    }
}
