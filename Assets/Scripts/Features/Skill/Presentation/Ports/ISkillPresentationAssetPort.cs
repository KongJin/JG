using UnityEngine;

namespace Features.Skill.Presentation
{
    public interface ISkillPresentationAssetPort
    {
        Sprite GetIcon(string skillId);
        GameObject GetEffectPrefab(string skillId);
    }
}
