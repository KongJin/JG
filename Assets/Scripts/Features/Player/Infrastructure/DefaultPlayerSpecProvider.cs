using Features.Player.Domain;

namespace Features.Player.Infrastructure
{
    /// <summary>
    /// 기본 플레이어 스펙 제공 구현체.
    /// TODO: 나중에 설정 파일, 서버, 또는 Garage에서 로드하도록 변경.
    /// </summary>
    public sealed class DefaultPlayerSpecProvider : Application.Ports.IPlayerSpecProvider
    {
        // TODO: 하드코딩된 값을 외부 소스에서 로드하도록 변경
        private readonly PlayerSpec _localSpec = new(
            maxHp: 100f,
            defense: 5f,
            maxEnergy: 100f,
            energyRegenPerSecond: 5f
        );

        private readonly PlayerSpec _remoteSpec = new(
            maxHp: 100f,
            defense: 5f,
            maxEnergy: 100f,
            energyRegenPerSecond: 5f
        );

        public PlayerSpec GetLocalPlayerSpec() => _localSpec;
        public PlayerSpec GetRemotePlayerSpec() => _remoteSpec;
    }
}
