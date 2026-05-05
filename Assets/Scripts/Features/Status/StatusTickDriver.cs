using Features.Status.Application.Ports;
using UnityEngine;

namespace Features.Status
{
    public sealed class StatusTickDriver : MonoBehaviour
    {
        private IStatusTickPort _tickPort;

        public void Initialize(IStatusTickPort tickPort)
        {
            _tickPort = tickPort;
        }

        public void Clear()
        {
            _tickPort = null;
        }

        private void Update()
        {
// csharp-guardrails: allow-null-defense
            _tickPort?.Tick(Time.deltaTime);
        }
    }
}
