using Features.Player.Application;
using UnityEngine;

namespace Features.Player.Presentation
{
    public sealed class EnergyRegenTicker : MonoBehaviour
    {
        private EnergyAdapter _energyAdapter;
        private bool _initialized;

        public void Initialize(EnergyAdapter energyAdapter)
        {
            if (energyAdapter == null)
            {
                Debug.LogError("[EnergyRegenTicker] EnergyAdapter is not provided.", this);
                return;
            }

            _energyAdapter = energyAdapter;
            _initialized = true;
        }

        private void Update()
        {
            if (!_initialized) return;
            _energyAdapter.TickRegen(Time.deltaTime, Time.time);
        }
    }
}
