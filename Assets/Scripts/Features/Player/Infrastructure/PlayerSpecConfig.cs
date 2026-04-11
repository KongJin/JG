using UnityEngine;

namespace Features.Player.Infrastructure
{
    /// <summary>
    /// 플레이어 기본 스펙 정의 (ScriptableObject).
    /// Inspector에서 밸런스 조정 가능.
    /// </summary>
    [CreateAssetMenu(menuName = "JG/Player/PlayerSpecConfig", fileName = "PlayerSpecConfig")]
    public sealed class PlayerSpecConfig : ScriptableObject
    {
        [Header("Local Player")]
        [Min(1f)] public float LocalMaxHp = 100f;
        [Min(0f)] public float LocalDefense = 5f;
        [Min(1f)] public float LocalMaxEnergy = 100f;
        [Min(0f)] public float LocalEnergyRegenPerSecond = 5f;

        [Header("Remote Player")]
        [Min(1f)] public float RemoteMaxHp = 100f;
        [Min(0f)] public float RemoteDefense = 5f;
        [Min(1f)] public float RemoteMaxEnergy = 100f;
        [Min(0f)] public float RemoteEnergyRegenPerSecond = 5f;
    }
}
