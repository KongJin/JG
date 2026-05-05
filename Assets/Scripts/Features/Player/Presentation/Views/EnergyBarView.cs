using Features.Player.Application.Events;
using Features.Player.Application.Ports;
using Shared.EventBus;
using Shared.Kernel;
using UnityEngine;

namespace Features.Player.Presentation
{
    public sealed class EnergyBarView : MonoBehaviour
    {
        private IEventSubscriber _eventBus;
        private DomainEntityId _playerId;
        private IEnergyManagementPort _energyPort;

        public float CurrentEnergy { get; private set; }
        public float MaxEnergy { get; private set; }
        public string DisplayText { get; private set; } = string.Empty;

        public void Initialize(
            IEventSubscriber eventBus,
            DomainEntityId playerId,
            float maxEnergy,
            IEnergyManagementPort energyPort)
        {
            _eventBus = eventBus;
            _playerId = playerId;
            _energyPort = energyPort;
            MaxEnergy = maxEnergy;
            UpdateDisplay(maxEnergy, maxEnergy);

            _eventBus.Subscribe(this, new System.Action<PlayerEnergyChangedEvent>(OnEnergyChanged));
        }

        private void OnDestroy()
        {
            // csharp-guardrails: allow-null-defense
            _eventBus?.UnsubscribeAll(this);
        }

        private void Update()
        {
// csharp-guardrails: allow-null-defense
            _energyPort?.TickRegen(Time.deltaTime, Time.time);
        }

        private void OnEnergyChanged(PlayerEnergyChangedEvent e)
        {
            if (!e.PlayerId.Equals(_playerId))
                return;

            UpdateDisplay(e.CurrentEnergy, e.MaxEnergy);
        }

        private void UpdateDisplay(float currentEnergy, float maxEnergy)
        {
            CurrentEnergy = currentEnergy;
            MaxEnergy = maxEnergy;
            DisplayText = $"ENERGY {Mathf.FloorToInt(currentEnergy)} / {Mathf.FloorToInt(maxEnergy)}";
        }
    }
}
