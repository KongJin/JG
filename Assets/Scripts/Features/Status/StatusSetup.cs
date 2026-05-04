using Features.Player.Application.Ports;
using Features.Skill.Application.Ports;
using Features.Status.Application;
using Features.Status.Application.Ports;
using Shared.Attributes;
using Shared.EventBus;
using Shared.Lifecycle;
using UnityEngine;

namespace Features.Status
{
    public sealed class StatusSetup : MonoBehaviour
    {
        [Required]
        [SerializeField]
        private StatusTickDriver _tickDriver;

        private StatusContainerRegistry _registry;
        private StatusUseCases _useCases;
        private StatusNetworkEventHandler _networkEventHandler;
        private DisposableScope _disposables;

        public ISpeedModifierPort SpeedModifier { get; private set; }
        public IStatusQueryPort StatusQuery { get; private set; }

        public void Initialize(
            EventBus eventBus,
            IStatusNetworkPort networkPort,
            bool isMaster)
        {
            _disposables?.Dispose();
            _disposables = new DisposableScope();

            _registry = new StatusContainerRegistry();

            _useCases = new StatusUseCases(_registry, eventBus, networkPort);

            var statusHandler = new StatusEventHandler(eventBus, _useCases);
            _disposables.Add(EventBusSubscription.ForOwner(eventBus, statusHandler));

            _networkEventHandler = new StatusNetworkEventHandler(_useCases, networkPort);

            _tickDriver.Initialize(new StatusTickUseCase(_registry, eventBus, networkPort, isMaster));

            SpeedModifier = new SpeedModifierAdapter(_registry);
            var queryAdapter = new StatusQueryAdapter(_registry);
            StatusQuery = queryAdapter;
        }

        public void RegisterRemoteNetworkPort(IStatusNetworkPort networkPort)
        {
            _networkEventHandler?.WireNetworkPort(networkPort);
        }

        private void OnDestroy()
        {
            _disposables?.Dispose();
            _tickDriver?.Clear();
        }
    }
}
