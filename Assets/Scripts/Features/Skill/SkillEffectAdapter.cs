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
            var pres = _catalog.GetPresentationData(skillId);
            return pres != null ? pres.CastEffectPrefab : null;
        }
    }
}
