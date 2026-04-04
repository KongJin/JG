using Features.Skill.Domain.Delivery;
using Features.Status.Domain;

namespace Features.Skill.Domain
{
    /// <summary>
    /// Inspector 명시 태그가 없을 때 Delivery·수치·상태로부터 보조 추론한다.
    /// 힐/순수 유틸 등은 SO에서 명시 태그를 권장한다.
    /// </summary>
    public static class SkillGameplayTagResolver
    {
        public static SkillGameplayTags Resolve(
            SkillGameplayTags serializedExplicit,
            DeliveryType delivery,
            float damage,
            StatusPayload status)
        {
            if (serializedExplicit != SkillGameplayTags.None)
                return serializedExplicit;
            return Infer(delivery, damage, status);
        }

        public static SkillGameplayTags Infer(DeliveryType delivery, float damage, StatusPayload status)
        {
            var tags = SkillGameplayTags.None;
            if (damage > 0f)
                tags |= SkillGameplayTags.Damage;

            if (status.HasEffect)
            {
                switch (status.Type)
                {
                    case StatusType.Slow:
                        tags |= SkillGameplayTags.CrowdControl | SkillGameplayTags.Debuff;
                        break;
                    case StatusType.Burn:
                        tags |= SkillGameplayTags.Damage | SkillGameplayTags.Debuff;
                        break;
                    case StatusType.Haste:
                        tags |= SkillGameplayTags.Buff;
                        break;
                }
            }

            return tags;
        }
    }
}
