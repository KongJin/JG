using Shared.Kernel;

namespace Features.Enemy.Application.Events
{
    public readonly struct EnemyHealthChangedEvent
    {
        public DomainEntityId EnemyId { get; }
        public float CurrentHp { get; }
        public float MaxHp { get; }
        public float Damage { get; }
        public bool IsDead { get; }

        public EnemyHealthChangedEvent(DomainEntityId enemyId, float currentHp, float maxHp, float damage, bool isDead)
        {
            EnemyId = enemyId;
            CurrentHp = currentHp;
            MaxHp = maxHp;
            Damage = damage;
            IsDead = isDead;
        }
    }
}
