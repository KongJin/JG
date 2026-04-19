using UnityEngine;

namespace Shared.Runtime.Sound
{
    public sealed class SoundPlayerRuntimeHostFactory
    {
        private const string RuntimeConfigResourcePath = "Shared/Sound/SoundPlayerRuntimeConfig";

        public bool TryGetOrCreate(out SoundPlayer soundPlayer, out string errorMessage)
        {
            soundPlayer = UnityEngine.Object.FindFirstObjectByType<SoundPlayer>();
            if (soundPlayer != null)
            {
                if (TryApplyConfig(soundPlayer, out errorMessage))
                    return true;

                soundPlayer = null;
                return false;
            }

            var config = Resources.Load<SoundPlayerRuntimeConfig>(RuntimeConfigResourcePath);
            if (config == null)
            {
                errorMessage =
                    $"[SoundPlayer] Missing runtime config at Resources/{RuntimeConfigResourcePath}.asset.";
                return false;
            }

            var host = new GameObject("SoundPlayer");
            soundPlayer = host.AddComponent<SoundPlayer>();
            soundPlayer.ApplyRuntimeConfig(config);

            if (!soundPlayer.HasRuntimeDependencies)
            {
                errorMessage = "[SoundPlayer] Runtime host was created, but audio dependencies are still missing.";
                UnityEngine.Object.Destroy(host);
                soundPlayer = null;
                return false;
            }

            errorMessage = null;
            return true;
        }

        private static bool TryApplyConfig(SoundPlayer soundPlayer, out string errorMessage)
        {
            if (soundPlayer.HasRuntimeDependencies)
            {
                errorMessage = null;
                return true;
            }

            var config = Resources.Load<SoundPlayerRuntimeConfig>(RuntimeConfigResourcePath);
            if (config == null)
            {
                errorMessage =
                    $"[SoundPlayer] Existing host is missing dependencies and runtime config was not found at Resources/{RuntimeConfigResourcePath}.asset.";
                return false;
            }

            soundPlayer.ApplyRuntimeConfig(config);
            if (!soundPlayer.HasRuntimeDependencies)
            {
                errorMessage = "[SoundPlayer] Existing host is still missing dependencies after applying runtime config.";
                return false;
            }

            errorMessage = null;
            return true;
        }
    }
}
