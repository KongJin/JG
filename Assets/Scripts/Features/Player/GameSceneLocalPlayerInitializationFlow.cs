using System;
using Features.Combat;
using Features.Combat.Application;
using Features.Combat.Presentation;
using Features.Player.Application.Ports;
using Features.Player.Infrastructure;
using Features.Player.Presentation;
using Features.Projectile;
using Features.Status;
using Features.Unit;
using Features.Unit.Application;
using Features.Unit.Application.Ports;
using Features.Unit.Presentation;
using Features.Wave;
using Features.Zone;
using Photon.Pun;
using Shared.EventBus;
using Shared.Kernel;
using Shared.Lifecycle;
using UnityEngine;
using UnitSpec = Features.Unit.Domain.Unit;

namespace Features.Player
{
    internal sealed class GameSceneLocalPlayerInitializationFlow
    {
        public bool Execute(GameSceneLocalPlayerInitializationContext context)
        {
            var localPlayerSetup = context.LocalPlayerSetup;
            if (localPlayerSetup == null)
                return false;

            context.CameraFollower.Initialize(
                localPlayerSetup.transform,
                context.Camera.transform.position - localPlayerSetup.transform.position);

            context.StatusSetup.Initialize(
                context.EventBus,
                localPlayerSetup.StatusNetworkAdapter,
                localPlayerSetup.StatusNetworkAdapter,
                PhotonNetwork.IsMasterClient);

            localPlayerSetup.InitializeLocal(
                context.EventBus,
                new DefaultPlayerSpecProvider(context.PlayerSpecConfig),
                context.StatusSetup.SpeedModifier,
                context.PlayerSceneRegistry,
                context.PlayerLookup);

            context.CombatSetup.Initialize(
                context.EventBus,
                localPlayerSetup.CombatNetworkPort,
                localPlayerSetup.PlayerId,
                new EntityAffiliationAdapter());

            if (context.WaveSetup != null && context.CoreObjective == null)
            {
                Debug.LogError(
                    "[GameSceneRoot] WaveSetup is set but CoreObjective is missing. Assign CoreObjectiveSetup on the objective GameObject.");
            }

            if (context.CoreObjective != null)
            {
                context.CoreObjective.RegisterCombatTarget(context.CombatSetup);
                context.CoreObjective.InitializePlacementArea();
            }

            if (context.DamageNumberSpawner != null)
                context.DamageNumberSpawner.Initialize(context.EventBus);

            context.EndReportingFlow.RegisterEndHandlers(
                context.Disposables,
                context.CoreObjective != null ? context.CoreObjective.CoreId : default,
                context.CoreObjective != null ? context.CoreObjective.CoreMaxHp : 0f);

            context.ConnectPlayer(localPlayerSetup);

            context.EnergyBarView.Initialize(
                context.EventBus,
                localPlayerSetup.PlayerId,
                localPlayerSetup.MaxEnergy,
                localPlayerSetup.EnergyAdapterInstance);

            context.AudioBootstrapFlow.InitializeOrReport(context.EventBus, localPlayerSetup.PlayerId.Value);
            context.ProjectileSetup.Initialize(context.EventBus, context.EventBus);
            context.ZoneSetup.Initialize(context.EventBus);
            context.MarkRemotePlayerWiringReady();

            var units = context.InitializeUnitAndGarage(localPlayerSetup);
            if (context.WaveSetup == null)
                return true;

            if (context.CoreObjective == null)
            {
                Debug.LogError("[GameSceneRoot] Cannot initialize Wave without CoreObjectiveSetup.");
                return false;
            }

            context.WaveSetup.Initialize(
                context.EventBus,
                context.CombatSetup,
                localPlayerSetup.PlayerId,
                context.CoreObjective);
            context.WaveSetup.RegisterPlayer(localPlayerSetup.transform);

            context.UnitSetup.InitializeBattleEntity(
                context.EventBus,
                new UnitEnergyAdapter(localPlayerSetup.EnergyAdapterInstance),
                context.CombatSetup,
                context.WaveSetup.UnitPositionQuery);

            ValidateAndInitializeSummonSlots(context, localPlayerSetup.PlayerId, units);
            return true;
        }

        private static void ValidateAndInitializeSummonSlots(
            GameSceneLocalPlayerInitializationContext context,
            DomainEntityId playerId,
            UnitSpec[] specs)
        {
            if (specs == null || specs.Length == 0)
            {
                Debug.LogWarning("[GameSceneRoot] No unit specs for player. Summon slots not initialized.");
                return;
            }

            var initialEnergy = context.LocalPlayerSetup.EnergyAdapterInstance.GetCurrentEnergy(playerId);
            var energyResult = InitialEnergyValidator.Validate(initialEnergy, specs);
            if (!energyResult.IsValid)
            {
                Debug.LogWarning(
                    $"[GameSceneRoot] Player {playerId.Value} starts with insufficient energy " +
                    $"({energyResult.InitialEnergy:F1} < {energyResult.MinSummonCost:F1}). " +
                    "Consider increasing initial energy.");
            }

            if (context.UnitSlotsContainer == null)
            {
                Debug.LogWarning("[GameSceneRoot] UnitSlotsContainer not assigned. Summon UI skipped.");
                return;
            }

            var placementArea = context.CoreObjective?.PlacementArea;
            if (placementArea == null)
                Debug.LogWarning("[GameSceneRoot] PlacementArea not available. Using default spawn position.");

            var energyPort = new UnitEnergyAdapter(context.LocalPlayerSetup.EnergyAdapterInstance);
            context.UnitSlotsContainer.Initialize(
                context.EventBus,
                context.UnitSetup.BattleEntitySetup.SummonUnit,
                energyPort,
                specs,
                playerId,
                placementArea,
                context.CoreObjective?.PlacementAreaView);
        }
    }

    internal readonly struct GameSceneLocalPlayerInitializationContext
    {
        public GameSceneLocalPlayerInitializationContext(
            EventBus eventBus,
            DisposableScope disposables,
            PlayerSetup localPlayerSetup,
            Camera camera,
            CameraFollower cameraFollower,
            PlayerSpecConfig playerSpecConfig,
            PlayerSceneRegistry playerSceneRegistry,
            IPlayerLookupPort playerLookup,
            StatusSetup statusSetup,
            CombatSetup combatSetup,
            ZoneSetup zoneSetup,
            ProjectileSetup projectileSetup,
            CoreObjectiveSetup coreObjective,
            WaveSetup waveSetup,
            UnitSetup unitSetup,
            UnitSlotsContainer unitSlotsContainer,
            EnergyBarView energyBarView,
            DamageNumberSpawner damageNumberSpawner,
            GameSceneAudioBootstrapFlow audioBootstrapFlow,
            GameSceneEndReportingFlow endReportingFlow,
            Action<PlayerSetup> connectPlayer,
            Action markRemotePlayerWiringReady,
            Func<PlayerSetup, UnitSpec[]> initializeUnitAndGarage)
        {
            EventBus = eventBus;
            Disposables = disposables;
            LocalPlayerSetup = localPlayerSetup;
            Camera = camera;
            CameraFollower = cameraFollower;
            PlayerSpecConfig = playerSpecConfig;
            PlayerSceneRegistry = playerSceneRegistry;
            PlayerLookup = playerLookup;
            StatusSetup = statusSetup;
            CombatSetup = combatSetup;
            ZoneSetup = zoneSetup;
            ProjectileSetup = projectileSetup;
            CoreObjective = coreObjective;
            WaveSetup = waveSetup;
            UnitSetup = unitSetup;
            UnitSlotsContainer = unitSlotsContainer;
            EnergyBarView = energyBarView;
            DamageNumberSpawner = damageNumberSpawner;
            AudioBootstrapFlow = audioBootstrapFlow;
            EndReportingFlow = endReportingFlow;
            ConnectPlayer = connectPlayer;
            MarkRemotePlayerWiringReady = markRemotePlayerWiringReady;
            InitializeUnitAndGarage = initializeUnitAndGarage;
        }

        public EventBus EventBus { get; }
        public DisposableScope Disposables { get; }
        public PlayerSetup LocalPlayerSetup { get; }
        public Camera Camera { get; }
        public CameraFollower CameraFollower { get; }
        public PlayerSpecConfig PlayerSpecConfig { get; }
        public PlayerSceneRegistry PlayerSceneRegistry { get; }
        public IPlayerLookupPort PlayerLookup { get; }
        public StatusSetup StatusSetup { get; }
        public CombatSetup CombatSetup { get; }
        public ZoneSetup ZoneSetup { get; }
        public ProjectileSetup ProjectileSetup { get; }
        public CoreObjectiveSetup CoreObjective { get; }
        public WaveSetup WaveSetup { get; }
        public UnitSetup UnitSetup { get; }
        public UnitSlotsContainer UnitSlotsContainer { get; }
        public EnergyBarView EnergyBarView { get; }
        public DamageNumberSpawner DamageNumberSpawner { get; }
        public GameSceneAudioBootstrapFlow AudioBootstrapFlow { get; }
        public GameSceneEndReportingFlow EndReportingFlow { get; }
        public Action<PlayerSetup> ConnectPlayer { get; }
        public Action MarkRemotePlayerWiringReady { get; }
        public Func<PlayerSetup, UnitSpec[]> InitializeUnitAndGarage { get; }
    }
}
