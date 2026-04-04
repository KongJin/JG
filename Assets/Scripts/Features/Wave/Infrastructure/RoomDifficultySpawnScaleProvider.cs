using Features.Wave.Application;
using Features.Wave.Application.Ports;
using UnityEngine;

namespace Features.Wave.Infrastructure
{
    /// <summary>
    /// Room CustomProperties의 난이도 프리셋을 읽어 스폰 배율을 제공한다.
    /// WaveBootstrap에 선택 연결; 미연결 시 Bootstrap이 동일 로직으로 폴백한다.
    /// </summary>
    public sealed class RoomDifficultySpawnScaleProvider : MonoBehaviour, IDifficultySpawnScale
    {
        public float SpawnCountMultiplier =>
            DifficultySpawnScale.MultiplierForPreset(RoomDifficultyReader.ReadPresetId());
    }
}
