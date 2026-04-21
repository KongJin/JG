using Features.Status.Application;
using Features.Status.Application.Ports;
using UnityEngine;

namespace Features.Status.Presentation
{
    public sealed class StatusTickController : MonoBehaviour
    {
        private IStatusTickPort _tickPort;

        public void Initialize(IStatusTickPort tickPort)
        {
            _tickPort = tickPort;
        }

        private void Update()
        {
            _tickPort?.Tick(Time.deltaTime);
        }
    }
}
