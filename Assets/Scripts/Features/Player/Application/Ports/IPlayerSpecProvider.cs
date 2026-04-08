using Features.Player.Domain;

namespace Features.Player.Application.Ports
{
    /// <summary>
    /// 플레이어 스펙 제공 포트.
    /// 하드코딩된 값을 외부 설정/파일/서버에서 로드할 수 있도록 추상화.
    /// </summary>
    public interface IPlayerSpecProvider
    {
        PlayerSpec GetLocalPlayerSpec();
        PlayerSpec GetRemotePlayerSpec();
    }
}
