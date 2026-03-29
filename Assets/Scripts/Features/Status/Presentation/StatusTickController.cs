using Features.Status.Application;
using UnityEngine;

namespace Features.Status.Presentation
{
    public sealed class StatusTickController : MonoBehaviour
    {
        private StatusTickUseCase _tickUseCase;

        public void Initialize(StatusTickUseCase tickUseCase)
        {
            _tickUseCase = tickUseCase;
        }

        private void Update()
        {
            _tickUseCase?.Tick(Time.deltaTime);
        }
    }
}
