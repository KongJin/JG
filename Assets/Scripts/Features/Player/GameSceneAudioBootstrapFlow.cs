using Shared.EventBus;
using Shared.Runtime.Sound;
using UnityEngine;

namespace Features.Player
{
    internal interface IAudioRuntimePort
    {
        bool TryInitialize(IEventSubscriber eventBus, string playerId, out string errorMessage);
    }

    internal sealed class SoundPlayerAudioRuntimePort : IAudioRuntimePort
    {
        private readonly SoundPlayerRuntimeHostFactory _hostFactory = new();

        public bool TryInitialize(IEventSubscriber eventBus, string playerId, out string errorMessage)
        {
            if (!_hostFactory.TryGetOrCreate(out var soundPlayer, out errorMessage))
                return false;

            soundPlayer.Initialize(eventBus, playerId);
            errorMessage = null;
            return true;
        }
    }

    internal sealed class GameSceneAudioBootstrapFlow
    {
        private readonly IAudioRuntimePort _audioRuntimePort;

        public GameSceneAudioBootstrapFlow()
            : this(new SoundPlayerAudioRuntimePort())
        {
        }

        public GameSceneAudioBootstrapFlow(IAudioRuntimePort audioRuntimePort)
        {
            _audioRuntimePort = audioRuntimePort;
        }

        public void InitializeOrReport(IEventSubscriber eventBus, string playerId)
        {
            if (!_audioRuntimePort.TryInitialize(eventBus, playerId, out var errorMessage))
            {
                Debug.LogError(errorMessage);
                return;
            }
        }
    }
}
