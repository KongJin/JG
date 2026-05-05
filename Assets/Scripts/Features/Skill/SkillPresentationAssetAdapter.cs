using Features.Skill.Infrastructure;
using Features.Skill.Presentation;
using UnityEngine;

namespace Features.Skill
{
    public sealed class SkillPresentationAssetAdapter : ISkillPresentationAssetPort
    {
        private readonly SkillCatalog _catalog;

        public SkillPresentationAssetAdapter(SkillCatalog catalog)
        {
            _catalog = catalog;
        }

        public Sprite GetIcon(string skillId)
        {
            var pres = _catalog.GetPresentationData(skillId);
// csharp-guardrails: allow-null-defense
            return pres != null ? pres.Icon : null;
        }

        public GameObject GetEffectPrefab(string skillId)
        {
            var pres = _catalog.GetPresentationData(skillId);
// csharp-guardrails: allow-null-defense
            return pres != null ? pres.CastEffectPrefab : null;
        }
    }
}
