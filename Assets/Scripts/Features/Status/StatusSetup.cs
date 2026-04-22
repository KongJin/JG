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
        private StatusContainerRegistry _registry;
        private StatusUseCases _useCases;
        private StatusNetworkEventHandler _networkEventHandler;
        private DisposableScope _disposables;
        private IStatusTickPort _tickPort;

        public ISpeedModifierPort SpeedModifier { get; private set; }
        public IStatusQueryPort StatusQuery { get; private set; }

        public void Initialize(
            EventBus eventBus,
            IStatusNetworkCommandPort commandPort,
            IStatusNetworkCallbackPort callbackPort,
            bool isMaster)
        {
            _disposables?.Dispose();
            _disposables = new DisposableScope();

            _registry = new StatusContainerRegistry();

            _useCases = new StatusUseCases(_registry, eventBus, commandPort);

            var statusHandler = new StatusEventHandler(eventBus, _useCases);
            _disposables.Add(EventBusSubscription.ForOwner(eventBus, statusHandler));

            _networkEventHandler = new StatusNetworkEventHandler(_useCases, callbackPort);

            _tickPort = new StatusTickUseCase(_registry, eventBus, commandPort, isMaster);

            SpeedModifier = new SpeedModifierAdapter(_registry);
            var queryAdapter = new StatusQueryAdapter(_registry);
            StatusQuery = queryAdapter;
        }

        public void RegisterRemoteCallbackPort(IStatusNetworkCallbackPort callbackPort)
        {
            _networkEventHandler?.WireCallbackPort(callbackPort);
        }

        private void OnDestroy()
        {
            _disposables?.Dispose();
        }

        private void Update()
        {
            _tickPort?.Tick(Time.deltaTime);
        }
    }
}
