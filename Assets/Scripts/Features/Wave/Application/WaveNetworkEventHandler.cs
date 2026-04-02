using System;
using Features.Wave.Application.Events;
using Features.Wave.Application.Ports;
using Features.Wave.Domain;
using Shared.EventBus;

namespace Features.Wave.Application
{
    public sealed class WaveNetworkEventHandler
    {
        private readonly IWaveNetworkCommandPort _commandPort;
        private readonly WaveLoopUseCase _waveLoop;

        public WaveNetworkEventHandler(
            IEventSubscriber subscriber,
            IWaveNetworkCommandPort commandPort,
            IWaveNetworkCallbackPort callbackPort,
            WaveLoopUseCase waveLoop)
        {
            _commandPort = commandPort;
            _waveLoop = waveLoop;

            subscriber.Subscribe(this, new Action<WaveCountdownStartedEvent>(OnCountdownStarted));
            subscriber.Subscribe(this, new Action<WaveStartedEvent>(OnWaveStarted));
            subscriber.Subscribe(this, new Action<WaveVictoryEvent>(OnVictory));
            subscriber.Subscribe(this, new Action<WaveDefeatEvent>(OnDefeat));

            callbackPort.OnWaveStateSynced = OnWaveStateSynced;
        }

        private void OnCountdownStarted(WaveCountdownStartedEvent e)
        {
            var countdownEndMs = _commandPort.ServerTimestampMs + (int)(e.Duration * 1000f);
            _commandPort.SyncWaveState(e.WaveIndex, (int)WaveState.Countdown, countdownEndMs);
        }

        private void OnWaveStarted(WaveStartedEvent e)
        {
            _commandPort.SyncWaveState(e.WaveIndex, (int)WaveState.Active, 0);
        }

        private void OnVictory(WaveVictoryEvent e)
        {
            _commandPort.SyncWaveState(_waveLoop.CurrentWaveIndex, (int)WaveState.Victory, 0);
        }

        private void OnDefeat(WaveDefeatEvent e)
        {
            _commandPort.SyncWaveState(_waveLoop.CurrentWaveIndex, (int)WaveState.Defeat, 0);
        }

        private void OnWaveStateSynced(int waveIndex, int waveStateInt, int countdownEndMs)
        {
            var state = (WaveState)waveStateInt;
            var countdownRemaining = 0f;

            if (state == WaveState.Countdown && countdownEndMs > 0)
            {
                var remainingMs = countdownEndMs - _commandPort.ServerTimestampMs;
                countdownRemaining = remainingMs > 0 ? remainingMs / 1000f : 0f;
            }

            _waveLoop.ForceState(waveIndex, state, countdownRemaining);
        }
    }
}
