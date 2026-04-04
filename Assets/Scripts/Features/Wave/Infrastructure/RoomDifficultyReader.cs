using Features.Wave.Application;
using Photon.Pun;

namespace Features.Wave.Infrastructure
{
    public static class RoomDifficultyReader
    {
        public static int ReadPresetId()
        {
            var room = PhotonNetwork.CurrentRoom;
            if (room == null)
                return DifficultySpawnScale.PresetNormal;

            if (!room.CustomProperties.TryGetValue(WaveRoomPropertyKeys.DifficultyPreset, out var raw) ||
                raw == null)
                return DifficultySpawnScale.PresetNormal;

            return raw switch
            {
                int i => i,
                byte b => b,
                short s => s,
                long l => (int)l,
                _ => DifficultySpawnScale.PresetNormal,
            };
        }
    }
}
