using System;
using Features.Wave.Application.Events;
using Features.Wave.Application.Ports;
using Features.Wave.Domain;
using Shared.EventBus;

namespace Features.Wave.Application
{
    public sealed class WaveNetworkEventHandler
    {
        private readonly IWaveNetworkPort _networkPort;
        private readonly WaveLoopUseCase _waveLoop;

        public WaveNetworkEventHandler(
            IEventSubscriber subscriber,
            IWaveNetworkPort networkPort,
            WaveLoopUseCase waveLoop)
        {
            _networkPort = networkPort;
            _waveLoop = waveLoop;

            subscriber.Subscribe(this, new Action<WaveCountdownStartedEvent>(OnCountdownStarted));
            subscriber.Subscribe(this, new Action<WaveStartedEvent>(OnWaveStarted));
            subscriber.Subscribe(this, new Action<WaveVictoryEvent>(OnVictory));
            subscriber.Subscribe(this, new Action<WaveDefeatEvent>(OnDefeat));

            networkPort.OnWaveStateSynced = OnWaveStateSynced;
        }

        private void OnCountdownStarted(WaveCountdownStartedEvent e)
        {
            var countdownEndMs = _networkPort.ServerTimestampMs + (int)(e.Duration * 1000f);
            _networkPort.SyncWaveState(e.WaveIndex, (int)WaveState.Countdown, countdownEndMs);
        }

        private void OnWaveStarted(WaveStartedEvent e)
        {
            _networkPort.SyncWaveState(e.WaveIndex, (int)WaveState.Active, 0);
        }

        private void OnVictory(WaveVictoryEvent e)
        {
            _networkPort.SyncWaveState(_waveLoop.CurrentWaveIndex, (int)WaveState.Victory, 0);
        }

        private void OnDefeat(WaveDefeatEvent e)
        {
            _networkPort.SyncWaveState(_waveLoop.CurrentWaveIndex, (int)WaveState.Defeat, 0);
        }

        private void OnWaveStateSynced(int waveIndex, int waveStateInt, int countdownEndMs)
        {
            var state = (WaveState)waveStateInt;
            var countdownRemaining = 0f;

            if (state == WaveState.Countdown && countdownEndMs > 0)
            {
                var remainingMs = countdownEndMs - _networkPort.ServerTimestampMs;
                countdownRemaining = remainingMs > 0 ? remainingMs / 1000f : 0f;
            }

            _waveLoop.ForceState(waveIndex, state, countdownRemaining);
        }
    }
}
