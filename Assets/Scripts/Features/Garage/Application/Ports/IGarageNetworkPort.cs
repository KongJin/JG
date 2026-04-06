using Features.Garage.Domain;

namespace Features.Garage.Application.Ports
{
    /// <summary>
    /// 편성 데이터 네트워크 동기화 포트.
    /// Consumer(Garage)가 정의하고 Provider(Photon Infrastructure)가 구현.
    /// </summary>
    public interface IGarageNetworkPort
    {
        /// <summary>
        /// 내 편성 데이터를 네트워크에 동기화.
        /// CustomProperties["garageRoster"]에 JSON 직렬화.
        /// </summary>
        void SyncRoster(GarageRoster roster);

        /// <summary>
        /// 편성 완료 여부 동기화.
        /// CustomProperties["garageReady"] 설정.
        /// </summary>
        void SyncReady(bool isReady);

        /// <summary>
        /// 특정 플레이어의 편성 데이터 조회.
        /// late-join 시 CustomProperties에서 복구.
        /// </summary>
        GarageRoster GetPlayerRoster(object playerId);

        /// <summary>
        /// 특정 플레이어의 편성 완료 여부 조회.
        /// </summary>
        bool IsPlayerReady(object playerId);

        /// <summary>
        /// 룸 내 모든 플레이어 편성 데이터 조회.
        /// </summary>
        System.Collections.Generic.Dictionary<object, GarageRoster> GetAllPlayersRosters();
    }
}
