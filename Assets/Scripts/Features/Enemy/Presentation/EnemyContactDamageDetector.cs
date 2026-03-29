using Features.Combat;
using Features.Combat.Domain;
using Shared.Kernel;
using UnityEngine;

namespace Features.Enemy.Presentation
{
    public sealed class EnemyContactDamageDetector : MonoBehaviour
    {
        private CombatBootstrap _combatBootstrap;
        private DomainEntityId _enemyId;
        private float _damage;
        private float _cooldown;
        private float _lastDamageTime;
        private bool _initialized;

        public void Initialize(CombatBootstrap combatBootstrap, DomainEntityId enemyId, float damage, float cooldown)
        {
            _combatBootstrap = combatBootstrap;
            _enemyId = enemyId;
            _damage = damage;
            _cooldown = cooldown;
            _initialized = true;
        }

        private void OnTriggerStay(Collider other)
        {
            if (!_initialized) return;
            if (Time.time - _lastDamageTime < _cooldown) return;

            var holder = other.GetComponentInParent<EntityIdHolder>();
            if (holder == null || !holder.IsInitialized) return;

            var targetId = holder.Id;
            if (targetId.Value != null && targetId.Value.StartsWith("enemy")) return;

            _combatBootstrap.ApplyDamage(targetId, _damage, DamageType.Physical, _enemyId);
            _lastDamageTime = Time.time;
        }
    }
}
