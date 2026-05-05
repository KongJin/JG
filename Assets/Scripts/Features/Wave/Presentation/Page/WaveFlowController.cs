using System;
using Features.Wave.Application;
using Features.Wave.Application.Events;
using Shared.EventBus;
using UnityEngine;

namespace Features.Wave.Presentation
{
    public sealed class WaveFlowController : MonoBehaviour
    {
        private WaveFlowDriver _driver;
        private IEventSubscriber _subscriber;

        public void Initialize(
            WaveFlowDriver driver,
            IEventSubscriber subscriber)
        {
            _driver = driver;
            _subscriber = subscriber;

            _subscriber.Subscribe(this, new Action<GameStartEvent>(OnGameStart));
        }

        private void Update()
        {
// csharp-guardrails: allow-null-defense
            _driver?.Tick(Time.deltaTime);
        }

        private void OnGameStart(GameStartEvent _)
        {
// csharp-guardrails: allow-null-defense
            _driver?.StartFirstWave();
        }

        private void OnDestroy()
        {
            // csharp-guardrails: allow-null-defense
            _subscriber?.UnsubscribeAll(this);
        }
    }
}
