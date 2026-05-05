using Features.Garage.Application.Ports;
using Features.Garage.Domain;
using System.Collections.Generic;

namespace Features.Garage.Application
{
    /// <summary>
    /// BattleScene 진입 시 Room CustomProperties에서 GarageRoster를 복원하는 UseCase.
    /// </summary>
    public sealed class RestoreGarageRosterUseCase
    {
        private readonly IGarageNetworkPort _networkPort;

        public RestoreGarageRosterUseCase(IGarageNetworkPort networkPort)
        {
            _networkPort = networkPort;
        }

        /// <summary>
        /// 로컬 플레이어의 GarageRoster를 복원하여 UnitLoadout[] 반환.
        /// </summary>
        public GarageRoster.UnitLoadout[] Execute()
        {
            var roster = _networkPort.GetLocalPlayerRoster();

            // csharp-guardrails: allow-null-defense
            if (roster == null || !roster.IsValid)
            {
                return new GarageRoster.UnitLoadout[0];
            }

            return roster.GetFilledLoadouts();
        }

        /// <summary>
        /// 특정 플레이어의 GarageRoster를 조회.
        /// </summary>
        public GarageRoster.UnitLoadout[] ExecuteForPlayer(object playerId)
        {
            var roster = _networkPort.GetPlayerRoster(playerId);

            // csharp-guardrails: allow-null-defense
            if (roster == null || !roster.IsValid)
            {
                return new GarageRoster.UnitLoadout[0];
            }

            return roster.GetFilledLoadouts();
        }

        /// <summary>
        /// 모든 플레이어의 GarageRoster를 조회.
        /// </summary>
        public Dictionary<object, GarageRoster.UnitLoadout[]> ExecuteAll()
        {
            var allRosters = _networkPort.GetAllPlayersRosters();
            var result = new Dictionary<object, GarageRoster.UnitLoadout[]>();

            foreach (var kvp in allRosters)
            {
                var roster = kvp.Value;
                // csharp-guardrails: allow-null-defense
                if (roster != null && roster.IsValid)
                {
                    result[kvp.Key] = roster.GetFilledLoadouts();
                }
            }

            return result;
        }
    }
}