using Features.Combat;
using Features.Enemy.Application.Ports;
using Features.Wave.Application;
using Features.Wave.Domain;
using Features.Wave.Infrastructure;
using Shared.Attributes;
using Shared.Kernel;
using UnityEngine;

namespace Features.Wave
{
    public sealed class CoreObjectiveBootstrap : MonoBehaviour, ICoreObjectiveQuery
    {
        [Required, SerializeField] private Transform _coreAnchor;
        [Required, SerializeField] private EntityIdHolder _entityIdHolder;
        [SerializeField] private float _maxHp = 1500f;
        [SerializeField] private float _defense;

        private bool _registered;

        public DomainEntityId CoreId => ObjectiveCoreIds.Default;
        public float CoreMaxHp => _maxHp;

        public bool TryGetCoreWorldPosition(out float x, out float y, out float z)
        {
            if (_coreAnchor == null)
            {
                x = y = z = 0f;
                return false;
            }

            var p = _coreAnchor.position;
            x = p.x;
            y = p.y;
            z = p.z;
            return true;
        }

        public void RegisterCombatTarget(CombatBootstrap combatBootstrap)
        {
            if (_registered)
                return;

            if (combatBootstrap == null)
            {
                Debug.LogError("[CoreObjectiveBootstrap] CombatBootstrap is missing.", this);
                return;
            }

            var id = ObjectiveCoreIds.Default;
            _entityIdHolder.Set(id);
            var domain = new SpawnObjectiveCoreUseCase().Execute(id, _maxHp, _defense);
            combatBootstrap.RegisterTarget(id, new ObjectiveCoreCombatTargetProvider(domain));
            _registered = true;
        }
    }
}
