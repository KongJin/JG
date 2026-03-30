using Features.Player.Application;
using UnityEngine;

namespace Features.Player.Presentation
{
    public sealed class ManaRegenTicker : MonoBehaviour
    {
        private ManaAdapter _manaAdapter;

        public void Initialize(ManaAdapter manaAdapter)
        {
            _manaAdapter = manaAdapter;
        }

        private void Update()
        {
            if (_manaAdapter == null) return;
            _manaAdapter.TickRegen(Time.deltaTime, Time.time);
        }
    }
}
