using System;

namespace Features.Skill.Domain
{
    /// <summary>
    /// EffectRole 계열 분류 플래그. Phase 2 태그 시너지·필터용. 상세는 SkillGameplayTagResolver와 game_design 문서를 따른다.
    /// </summary>
    [Flags]
    public enum SkillGameplayTags : uint
    {
        None = 0,
        Damage = 1u << 0,
        Heal = 1u << 1,
        Shield = 1u << 2,
        CrowdControl = 1u << 3,
        Move = 1u << 4,
        Buff = 1u << 5,
        Debuff = 1u << 6,
        Summon = 1u << 7,
        Vision = 1u << 8,
        Utility = 1u << 9
    }
}
