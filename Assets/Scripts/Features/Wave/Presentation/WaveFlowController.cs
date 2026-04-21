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
            _driver?.Tick(Time.deltaTime);
        }

        private void OnGameStart(GameStartEvent _)
        {
            _driver?.StartFirstWave();
        }

        private void OnDestroy()
        {
            _subscriber?.UnsubscribeAll(this);
        }
    }
}
