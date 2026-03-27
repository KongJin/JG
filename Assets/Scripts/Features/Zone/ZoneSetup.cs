using Features.Skill.Application.Events;
using Features.Zone.Application;
using Shared.EventBus;
using Shared.Lifecycle;
using Shared.Time;
using UnityEngine;

namespace Features.Zone
{
    public sealed class ZoneSetup : MonoBehaviour
    {
        [SerializeField]
        private ZoneEffectAdapter _zoneEffectAdapter;

        private IEventSubscriber _eventBus;
        private SpawnZoneUseCase _spawnZoneUseCase;
        private DisposableScope _disposables = new DisposableScope();

        public void Initialize(EventBus eventBus)
        {
            if (_zoneEffectAdapter == null)
            {
                Debug.LogError("[ZoneSetup] ZoneEffectAdapter is not assigned in Inspector.", this);
                return;
            }

            _eventBus = eventBus;
            _disposables.Dispose();
            _disposables = new DisposableScope();
            _spawnZoneUseCase = new SpawnZoneUseCase(
                _zoneEffectAdapter,
                new ClockAdapter(),
                eventBus
            );
            _disposables.Add(EventBusSubscription.ForOwner(_eventBus, this));
            _eventBus.Subscribe(this, new System.Action<ZoneRequestedEvent>(OnZoneRequested));
        }

        private void OnDestroy()
        {
            _disposables.Dispose();
        }

        private void OnZoneRequested(ZoneRequestedEvent e)
        {
            if (_spawnZoneUseCase == null)
            {
                Debug.LogError("[ZoneSetup] SpawnZoneUseCase is not initialized.", this);
                return;
            }

            var result = _spawnZoneUseCase.Execute(
                e.CasterId, e.Position, e.Direction, e.Spec.Range, e.Spec.Cooldown);

            if (result.IsFailure)
                Debug.LogError($"[ZoneSetup] Spawn failed: {result.Error}", this);
        }
    }
}
