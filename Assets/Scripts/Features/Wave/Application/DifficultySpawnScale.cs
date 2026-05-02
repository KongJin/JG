using System;
using Shared.Gameplay;

namespace Features.Wave.Application
{
    /// <summary>
    /// 로비에서 동기화된 난이도 프리셋 ID를 스폰 개수 배율로 변환한다. Unity 타입 없음.
    /// </summary>
    public static class DifficultySpawnScale
    {
        public const int PresetNormal = DifficultyPreset.Normal;
        public const int PresetEasy = DifficultyPreset.Easy;
        public const int PresetHard = DifficultyPreset.Hard;

        public static float MultiplierForPreset(int presetId)
        {
            return presetId switch
            {
                PresetEasy => 0.75f,
                PresetHard => 1.35f,
                PresetNormal => 1f,
                _ => 1f,
            };
        }

        public static int ScaledSpawnCount(int baseCount, float multiplier)
        {
            if (baseCount < 1)
                return baseCount;
            var scaled = (int)Math.Round(baseCount * multiplier, MidpointRounding.AwayFromZero);
            return Math.Max(1, scaled);
        }
    }
}
