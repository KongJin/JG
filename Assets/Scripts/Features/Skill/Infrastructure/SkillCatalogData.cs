using Shared.Attributes;
using UnityEngine;

namespace Features.Skill.Infrastructure
{
    /// <summary>ScriptableObject that holds the collection of all available SkillData assets.</summary>
    [CreateAssetMenu(fileName = "SkillCatalog", menuName = "Skill/SkillCatalogData")]
    public sealed class SkillCatalogData : ScriptableObject
    {
        [Required, SerializeField] private SkillData[] skills;

        public SkillData[] Skills => skills;
    }
}
