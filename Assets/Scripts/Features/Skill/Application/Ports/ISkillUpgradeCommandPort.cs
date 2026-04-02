using Features.Skill.Domain;

namespace Features.Skill.Application.Ports
{
    public interface ISkillUpgradeCommandPort
    {
        bool TryUpgrade(string skillId, GrowthAxis axis);
        bool CanUpgrade(string skillId, GrowthAxis axis);
    }
}
