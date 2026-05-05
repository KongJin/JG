using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Features.Skill.Infrastructure
{
    public sealed class SkillCatalog
    {
        private readonly Dictionary<string, SkillData> _dataById = new Dictionary<string, SkillData>();

        public SkillCatalog(SkillCatalogData catalogData)
        {
            foreach (var data in catalogData.Skills)
            {
// csharp-guardrails: allow-null-defense
                if (data == null) continue;
                if (_dataById.ContainsKey(data.SkillId))
                {
                    Debug.LogWarning($"[SkillCatalog] Duplicate skill ID: {data.SkillId}");
                    continue;
                }
                _dataById[data.SkillId] = data;
            }
        }

        public Domain.Skill Get(string skillId)
        {
            if (_dataById.TryGetValue(skillId, out var data))
                return data.ToDomain();

            Debug.LogError($"[SkillCatalog] Skill not found: {skillId}");
            return null;
        }

        public SkillData GetData(string skillId)
        {
            _dataById.TryGetValue(skillId, out var data);
            return data;
        }

        public SkillPresentationData GetPresentationData(string skillId)
        {
            if (_dataById.TryGetValue(skillId, out var data))
                return data.Presentation;
            return null;
        }

        public SkillData[] UniqueSkills => _dataById.Values.ToArray();
    }
}
