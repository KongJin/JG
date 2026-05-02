namespace Shared.Runtime.Sound
{
    public sealed class SoundPlayerRuntimeHostFactory
    {
        public bool TryGetHostOnly(out SoundPlayer soundPlayer, out string errorMessage)
        {
            if (SoundPlayer.TryGetActive(out soundPlayer))
            {
                if (soundPlayer.HasRuntimeDependencies)
                {
                    errorMessage = null;
                    return true;
                }

                soundPlayer = null;
                errorMessage = "[SoundPlayer] Active scene host is missing serialized audio dependencies.";
                return false;
            }

            errorMessage = "[SoundPlayer] No active scene-owned SoundPlayer host is available.";
            return false;
        }
    }
}
