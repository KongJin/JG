using Features.Skill.Infrastructure;
using Features.Skill.Presentation;
using UnityEngine;

namespace Features.Skill
{
    public sealed class SkillIconAdapter : ISkillIconPort
    {
        private readonly SkillCatalog _catalog;

        public SkillIconAdapter(SkillCatalog catalog)
        {
            _catalog = catalog;
        }

        public Sprite GetIcon(string skillId)
        {
            var pres = _catalog.GetPresentationData(skillId);
            return pres != null ? pres.Icon : null;
        }
    }
}
