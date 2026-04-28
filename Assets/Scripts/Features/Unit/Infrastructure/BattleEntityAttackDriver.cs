using Features.Combat;
using Features.Combat.Domain;
using Features.Unit.Domain;
using Photon.Pun;
using Shared.Kernel;
using Shared.Math;
using Shared.Runtime;
using UnityEngine;

namespace Features.Unit.Infrastructure
{
    public sealed class BattleEntityAttackDriver : MonoBehaviour
    {
        [SerializeField] private float _fallbackAttackIntervalSeconds = 1f;
        [SerializeField] private float _fallbackAttackRangePadding = 0.25f;

        private CombatSetup _combatSetup;
        private BattleEntity _battleEntity;
        private float _nextAttackTime;

        public void Initialize(CombatSetup combatSetup, BattleEntity battleEntity)
        {
            _combatSetup = combatSetup;
            _battleEntity = battleEntity;
            _nextAttackTime = 0f;
        }

        public void Clear()
        {
            _combatSetup = null;
            _battleEntity = null;
        }

        private void Update()
        {
            if (_battleEntity == null || _combatSetup == null)
                return;

            if (!_battleEntity.IsAlive || !PhotonNetwork.IsMasterClient)
                return;

            if (Time.time < _nextAttackTime)
                return;

            if (!TryGetNearestEnemyInRange(out var targetId))
                return;

            _combatSetup.ApplyDamage(
                targetId,
                _battleEntity.UnitSpec.FinalAttackDamage,
                DamageType.Physical,
                _battleEntity.Id);

            var attackSpeed = _battleEntity.UnitSpec.FinalAttackSpeed;
            var interval = attackSpeed > 0f ? 1f / attackSpeed : _fallbackAttackIntervalSeconds;
            _nextAttackTime = Time.time + Mathf.Max(0.1f, interval);
        }

        private bool TryGetNearestEnemyInRange(out DomainEntityId targetId)
        {
            targetId = default;

            var attackRange = Mathf.Max(0.5f, _battleEntity.UnitSpec.FinalRange + _fallbackAttackRangePadding);
            var anchorRange = _battleEntity.UnitSpec.FinalAnchorRange;
            var queryRange = anchorRange > 0f ? Mathf.Min(attackRange, anchorRange) : attackRange;
            var hits = Physics.OverlapSphere(
                transform.position,
                queryRange,
                ~0,
                QueryTriggerInteraction.Collide);

            var bestDistanceSq = float.MaxValue;
            var found = false;

            foreach (var hit in hits)
            {
                if (hit == null)
                    continue;

                if (!ComponentAccess.TryGetEntityIdHolder(hit, out var holder))
                    continue;

                var candidateId = holder.Id;
                if (string.IsNullOrWhiteSpace(candidateId.Value) || !candidateId.Value.StartsWith("enemy-"))
                    continue;

                var candidatePosition = hit.transform.position;
                if (!_battleEntity.IsWithinAnchorRadius(new Float3(
                        candidatePosition.x,
                        candidatePosition.y,
                        candidatePosition.z)))
                {
                    continue;
                }

                var distanceSq = (hit.transform.position - transform.position).sqrMagnitude;
                if (distanceSq >= bestDistanceSq)
                    continue;

                bestDistanceSq = distanceSq;
                targetId = candidateId;
                found = true;
            }

            return found;
        }
    }
}
