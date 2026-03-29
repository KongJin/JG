using Shared.Kernel;

namespace Features.Enemy.Domain
{
    public sealed class Enemy : Entity
    {
        public Enemy(DomainEntityId id, EnemySpec spec) : base(id)
        {
            Spec = spec;
            MaxHp = spec.MaxHp;
            CurrentHp = spec.MaxHp;
        }

        public EnemySpec Spec { get; }
        public float MaxHp { get; }
        public float CurrentHp { get; private set; }
        public bool IsDead => CurrentHp <= 0f;

        public float TakeDamage(float damage)
        {
            if (IsDead)
                return CurrentHp;

            if (damage < 0f)
                damage = 0f;

            CurrentHp -= damage;
            if (CurrentHp < 0f)
                CurrentHp = 0f;

            return CurrentHp;
        }

        public void Die()
        {
            CurrentHp = 0f;
        }
    }
}
