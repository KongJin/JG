using Features.Player.Application.Ports;
using UnityEngine;

namespace Features.Player.Presentation
{
    public sealed class EnergyRegenTicker : MonoBehaviour
    {
        private IEnergyRegenPort _energyRegenPort;
        private bool _initialized;

        public void Initialize(IEnergyRegenPort energyRegenPort)
        {
            if (energyRegenPort == null)
            {
                Debug.LogError("[EnergyRegenTicker] Energy regen port is not provided.", this);
                return;
            }

            _energyRegenPort = energyRegenPort;
            _initialized = true;
        }

        private void Update()
        {
            if (!_initialized) return;
            _energyRegenPort.TickRegen(Time.deltaTime, Time.time);
        }
    }
}
