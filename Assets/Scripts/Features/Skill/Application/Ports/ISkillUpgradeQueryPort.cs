using Features.Skill.Domain;

namespace Features.Skill.Application.Ports
{
    public interface ISkillUpgradeQueryPort
    {
        float GetAxisMultiplier(string skillId, GrowthAxis axis);
        float GetAllyDamageScale(string skillId);
        int GetLevel(string skillId, GrowthAxis axis);
    }
}
