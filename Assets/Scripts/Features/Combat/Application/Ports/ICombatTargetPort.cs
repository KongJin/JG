using Shared.Kernel;

namespace Features.Combat.Application.Ports
{
    /// <summary>대상에게 데미지 적용 후 남은 체력과 사망 여부를 반환하는 결과 데이터.</summary>
    public readonly struct CombatTargetDamageResult
    {
        public CombatTargetDamageResult(float remainingHealth, bool isDead, bool isDowned = false)
        {
            RemainingHealth = remainingHealth;
            IsDead = isDead;
            IsDowned = isDowned;
        }

        public float RemainingHealth { get; }
        public bool IsDead { get; }
        public bool IsDowned { get; }
    }

    public interface ICombatTargetPort
    {
        bool Exists(DomainEntityId targetId);
        float GetDefense(DomainEntityId targetId);
        CombatTargetDamageResult ApplyDamage(DomainEntityId targetId, float damage);
    }
}
