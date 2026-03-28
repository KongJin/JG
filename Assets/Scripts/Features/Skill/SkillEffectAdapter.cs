using Features.Skill.Infrastructure;
using Features.Skill.Presentation;
using UnityEngine;

namespace Features.Skill
{
    public sealed class SkillEffectAdapter : ISkillEffectPort
    {
        private readonly SkillCatalog _catalog;

        public SkillEffectAdapter(SkillCatalog catalog)
        {
            _catalog = catalog;
        }

        public GameObject GetEffectPrefab(string skillId)
        {
            var data = _catalog.GetData(skillId);
            return data != null ? data.CastEffectPrefab : null;
        }
    }
}
