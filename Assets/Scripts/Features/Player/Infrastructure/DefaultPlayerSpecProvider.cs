using Features.Player.Domain;
using UnityEngine;

namespace Features.Player.Infrastructure
{
    /// <summary>
    /// 기본 플레이어 스펙 제공 구현체.
    /// Resources/PlayerSpecConfig SO를 로드하여 사용.
    /// Config가 없으면 기본값 폴백.
    /// </summary>
    public sealed class DefaultPlayerSpecProvider : Application.Ports.IPlayerSpecProvider
    {
        private static readonly PlayerSpec DefaultLocalSpec = new(
            maxHp: 100f,
            defense: 5f,
            maxEnergy: 100f,
            energyRegenPerSecond: 5f
        );

        private static readonly PlayerSpec DefaultRemoteSpec = new(
            maxHp: 100f,
            defense: 5f,
            maxEnergy: 100f,
            energyRegenPerSecond: 5f
        );

        private readonly PlayerSpec _localSpec;
        private readonly PlayerSpec _remoteSpec;
        public DefaultPlayerSpecProvider()
            : this(new ResourcesPlayerSpecConfigPort())
        {
        }

        public DefaultPlayerSpecProvider(IPlayerSpecConfigPort configPort)
        {
            var config = configPort.Load();

            if (config != null)
            {
                _localSpec = new PlayerSpec(
                    maxHp: config.LocalMaxHp,
                    defense: config.LocalDefense,
                    maxEnergy: config.LocalMaxEnergy,
                    energyRegenPerSecond: config.LocalEnergyRegenPerSecond
                );

                _remoteSpec = new PlayerSpec(
                    maxHp: config.RemoteMaxHp,
                    defense: config.RemoteDefense,
                    maxEnergy: config.RemoteMaxEnergy,
                    energyRegenPerSecond: config.RemoteEnergyRegenPerSecond
                );
            }
            else
            {
                _localSpec = DefaultLocalSpec;
                _remoteSpec = DefaultRemoteSpec;
            }
        }

        public PlayerSpec GetLocalPlayerSpec() => _localSpec;
        public PlayerSpec GetRemotePlayerSpec() => _remoteSpec;
    }

    public interface IPlayerSpecConfigPort
    {
        PlayerSpecConfig Load();
    }

    internal sealed class ResourcesPlayerSpecConfigPort : IPlayerSpecConfigPort
    {
        private const string ConfigResourcePath = "PlayerSpecConfig";

        public PlayerSpecConfig Load()
        {
            return Resources.Load<PlayerSpecConfig>(ConfigResourcePath);
        }
    }
}
