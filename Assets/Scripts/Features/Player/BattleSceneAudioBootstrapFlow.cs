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
            if (!_hostFactory.TryGetHostOnly(out var soundPlayer, out errorMessage))
                return false;

            soundPlayer.Initialize(eventBus, playerId);
            soundPlayer.PlayBgm("bgm_battle", 0.35f);
            errorMessage = null;
            return true;
        }
    }

    internal sealed class BattleSceneAudioBootstrapFlow
    {
        private readonly IAudioRuntimePort _audioRuntimePort;

        public BattleSceneAudioBootstrapFlow()
            : this(new SoundPlayerAudioRuntimePort())
        {
        }

        public BattleSceneAudioBootstrapFlow(IAudioRuntimePort audioRuntimePort)
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
