using Features.Account.Application.Ports;
using Features.Garage;
using Features.Lobby.Application;
using Features.Lobby.Application.Events;
using Features.Lobby.Infrastructure.Persistence;
using Features.Lobby.Infrastructure.Photon;
using Features.Lobby.Presentation;
using Features.Unit;
using Shared.EventBus;
using Shared.Runtime.Sound;
using Shared.Time;
using UnityEngine;
using DomainLobby = Features.Lobby.Domain.Lobby;

internal sealed class LobbySceneInitializationFlow
{
    public LobbySceneInitializationResult Initialize(
        EventBus eventBus,
        LobbyView view,
        LobbyPhotonAdapter photonAdapter,
        SoundPlayer soundPlayer,
        UnitSetup unitSetup,
        GarageSetup garageSetup,
        IAccountDataPort accountDataPort,
        System.Action applyLoadedAccountSettings)
    {
        var repository = new LobbyRepository();
        var clock = new ClockAdapter();
        var syncHandler = new LobbyNetworkEventHandler(repository, eventBus, photonAdapter);
        var useCases = new LobbyUseCases(repository, photonAdapter, clock);

        soundPlayer.Initialize(eventBus, SoundPlayer.LobbyOwnerId);
        applyLoadedAccountSettings?.Invoke();

        view.Initialize(eventBus, eventBus, useCases);
        eventBus.Publish(new LobbyUpdatedEvent(repository.LoadLobby() ?? new DomainLobby()));

        if (unitSetup != null)
            unitSetup.Initialize(eventBus);

        if (garageSetup != null)
        {
            if (unitSetup == null)
            {
                Debug.LogWarning(
                    "[LobbySetup] GarageSetup is assigned but UnitSetup is missing. Garage initialization is skipped.");
            }
            else
            {
                garageSetup.Initialize(
                    eventBus,
                    unitSetup.CompositionPort,
                    unitSetup.Catalog,
                    accountDataPort);
            }
        }

        return new LobbySceneInitializationResult(syncHandler);
    }
}

internal readonly struct LobbySceneInitializationResult
{
    public LobbyNetworkEventHandler SyncHandler { get; }

    public LobbySceneInitializationResult(LobbyNetworkEventHandler syncHandler)
    {
        SyncHandler = syncHandler;
    }
}
