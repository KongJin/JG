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
            _commandPort.SyncWaveState(e.WaveIndex, (int)WaveState.Countdown);
        }

        private void OnWaveStarted(WaveStartedEvent e)
        {
            _commandPort.SyncWaveState(e.WaveIndex, (int)WaveState.Active);
        }

        private void OnVictory(WaveVictoryEvent e)
        {
            _commandPort.SyncWaveState(_waveLoop.CurrentWaveIndex, (int)WaveState.Victory);
        }

        private void OnDefeat(WaveDefeatEvent e)
        {
            _commandPort.SyncWaveState(_waveLoop.CurrentWaveIndex, (int)WaveState.Defeat);
        }

        private void OnWaveStateSynced(int waveIndex, int waveStateInt)
        {
            _waveLoop.ForceState(waveIndex, (WaveState)waveStateInt);
        }
    }
}
