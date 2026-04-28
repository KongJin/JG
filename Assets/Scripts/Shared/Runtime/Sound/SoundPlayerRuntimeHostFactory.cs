using UnityEngine;

namespace Shared.Runtime.Sound
{
    public sealed class SoundPlayerRuntimeHostFactory
    {
        private const string RuntimeConfigResourcePath = "Shared/Sound/SoundPlayerRuntimeConfig";
        private const string RuntimePrefabResourcePath = "Shared/Sound/SoundPlayer";

        public bool TryGetOrCreate(out SoundPlayer soundPlayer, out string errorMessage)
        {
            if (SoundPlayer.TryGetActive(out soundPlayer))
            {
                if (TryApplyConfig(soundPlayer, out errorMessage))
                    return true;

                soundPlayer = null;
                return false;
            }

            var prefab = Resources.Load<SoundPlayer>(RuntimePrefabResourcePath);
            if (prefab == null)
            {
                errorMessage =
                    $"[SoundPlayer] Missing runtime prefab at Resources/{RuntimePrefabResourcePath}.prefab.";
                return false;
            }

            soundPlayer = UnityEngine.Object.Instantiate(prefab);
            soundPlayer.name = prefab.name;

            if (!soundPlayer.HasRuntimeDependencies)
            {
                errorMessage = "[SoundPlayer] Runtime prefab was created, but audio dependencies are missing.";
                UnityEngine.Object.Destroy(soundPlayer.gameObject);
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
