using Features.Garage;
using Features.Garage.Application;
using Features.Garage.Domain;
using Features.Unit;
using Features.Unit.Application;
using Photon.Pun;
using Shared.EventBus;
using Shared.Kernel;
using Shared.Time;
using UnityEngine;

namespace Features.Player
{
    internal sealed class GameSceneGarageBootstrapFlow
    {
        public GameSceneGarageBootstrapResult Initialize(
            EventBus eventBus,
            UnitSetup unitSetup,
            GarageSetup garageSetup,
            DomainEntityId playerId)
        {
            unitSetup.Initialize(eventBus);
            garageSetup.Initialize(eventBus, unitSetup.CompositionPort, unitSetup.Catalog);

            var restoreGarageRosterUseCase = new RestoreGarageRosterUseCase(garageSetup.NetworkPort);
            var computePlayerUnitSpecsUseCase = new ComputePlayerUnitSpecsUseCase(
                garageSetup.ComposeUnit,
                new ClockAdapter(),
                eventBus);

            GarageRoster.UnitLoadout[] loadouts;
            if (!PhotonNetwork.InRoom)
            {
                Debug.LogWarning("[GameSceneRoot] Cannot restore GarageRoster: not in room.");
                loadouts = System.Array.Empty<GarageRoster.UnitLoadout>();
            }
            else
            {
                loadouts = restoreGarageRosterUseCase.Execute();
            }

            var units = computePlayerUnitSpecsUseCase.Execute(loadouts, playerId);
            return new GameSceneGarageBootstrapResult(units);
        }
    }

    internal readonly struct GameSceneGarageBootstrapResult
    {
        public Unit.Domain.Unit[] PlayerUnits { get; }

        public GameSceneGarageBootstrapResult(Unit.Domain.Unit[] playerUnits)
        {
            PlayerUnits = playerUnits;
        }
    }
}
