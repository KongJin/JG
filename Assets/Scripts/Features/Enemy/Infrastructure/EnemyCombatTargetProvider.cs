using Features.Combat.Application.Ports;

namespace Features.Enemy.Infrastructure
{
    public sealed class EnemyCombatTargetProvider : ICombatTargetProvider
    {
        private readonly Domain.Enemy _enemy;

        public EnemyCombatTargetProvider(Domain.Enemy enemy)
        {
            _enemy = enemy;
        }

        public float GetDefense() => _enemy.Spec.Defense;

        public float GetCurrentHealth() => _enemy.CurrentHp;

        public CombatTargetDamageResult ApplyDamage(float damage)
        {
            if (_enemy.IsDead)
                return new CombatTargetDamageResult(_enemy.CurrentHp, true, false);

            var remaining = _enemy.TakeDamage(damage);
            return new CombatTargetDamageResult(remaining, _enemy.IsDead, false);
        }
    }
}
