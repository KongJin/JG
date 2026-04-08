using Features.Combat.Application.Ports;
using Features.Unit.Domain;

namespace Features.Unit.Infrastructure
{
    public sealed class BattleEntityCombatTargetProvider : ICombatTargetProvider
    {
        private readonly BattleEntity _entity;

        public BattleEntityCombatTargetProvider(BattleEntity entity)
        {
            _entity = entity;
        }

        public float GetDefense() => 0f; // Defense not yet modeled in UnitSpec

        public float GetCurrentHealth() => _entity.CurrentHp;

        public CombatTargetDamageResult ApplyDamage(float damage)
        {
            if (_entity.IsDead)
                return new CombatTargetDamageResult(_entity.CurrentHp, true, false);

            var remaining = _entity.TakeDamage(damage);
            return new CombatTargetDamageResult(remaining, _entity.IsDead, false);
        }
    }
}
