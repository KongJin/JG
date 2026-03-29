using Shared.Attributes;
using Features.Zone.Application;
using Shared.EventBus;
using Shared.Lifecycle;
using Shared.Time;
using UnityEngine;

namespace Features.Zone
{
    public sealed class ZoneSetup : MonoBehaviour
    {
        [Required, SerializeField]
        private ZoneEffectAdapter _zoneEffectAdapter;

        private ZoneEventHandler _zoneEventHandler;
        private DisposableScope _disposables = new DisposableScope();

        public void Initialize(EventBus eventBus)
        {
            _disposables.Dispose();
            _disposables = new DisposableScope();

            _zoneEffectAdapter.Initialize(eventBus);

            var spawnZoneUseCase = new SpawnZoneUseCase(
                _zoneEffectAdapter,
                new ClockAdapter(),
                eventBus
            );
            _zoneEventHandler = new ZoneEventHandler(spawnZoneUseCase, eventBus);
            _disposables.Add(EventBusSubscription.ForOwner(eventBus, _zoneEventHandler));
        }

        private void OnDestroy()
        {
            _disposables.Dispose();
        }
    }
}
