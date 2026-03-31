using Features.Player.Application;
using UnityEngine;

namespace Features.Player.Presentation
{
    public sealed class ManaRegenTicker : MonoBehaviour
    {
        private ManaAdapter _manaAdapter;
        private bool _initialized;

        public void Initialize(ManaAdapter manaAdapter)
        {
            if (manaAdapter == null)
            {
                Debug.LogError("[ManaRegenTicker] ManaAdapter is not provided.", this);
                return;
            }

            _manaAdapter = manaAdapter;
            _initialized = true;
        }

        private void Update()
        {
            if (!_initialized) return;
            _manaAdapter.TickRegen(Time.deltaTime, Time.time);
        }
    }
}
