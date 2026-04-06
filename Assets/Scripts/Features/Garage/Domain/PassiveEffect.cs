using System;

namespace Features.Garage.Domain
{
    /// <summary>
    /// 고유 특성의 효과 유형.
    /// 순수 C# — Unity/Photon 의존성 없음.
    /// </summary>
    public enum PassiveEffectType
    {
        None = 0,
        /// <summary> 주변 아군 피격 데미지 감소 </summary>
        DamageReductionAura = 1,
        /// <summary> 이동범위 내 가장 가까운 적 대상 공격속도 증가 </summary>
        PursuitAttackSpeed = 2,
        /// <summary> 주기적으로 현재 대상에게 고정 데미지 </summary>
        PeriodicFixedDamage = 3,
        /// <summary> 주기적으로 가장 낮은 HP 아군 HP 회복 </summary>
        EmergencyHeal = 4
    }

    /// <summary>
    /// 고유 특성 효과의 도메인 표현.
    /// 순수 C# 값 객체.
    /// </summary>
    public readonly struct PassiveEffect
    {
        public PassiveEffectType Type { get; }
        public PassiveStrength Strength { get; }
        public float Value { get; }
        public float CooldownSeconds { get; }

        public PassiveEffect(
            PassiveEffectType type,
            PassiveStrength strength,
            float value,
            float cooldownSeconds)
        {
            Type = type;
            Strength = strength;
            Value = value;
            CooldownSeconds = cooldownSeconds;
        }

        /// <summary>
        /// 비용 보정값 (소환 비용 계산용).
        /// </summary>
        public int CostBonus => (int)Strength;
    }

    /// <summary>
    /// 고유 특성의 강도. 비용 보정값과 동일.
    /// </summary>
    public enum PassiveStrength
    {
        Weak = 2,
        Medium = 5,
        Strong = 10
    }
}
