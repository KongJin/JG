using Features.Combat.Application.Events;
using Features.Combat.Domain;
using Features.Enemy.Application;
using Features.Enemy.Application.Events;
using Features.Enemy.Application.Ports;
using Features.Enemy.Domain;
using Features.Player.Application;
using Features.Player.Application.Events;
using Features.Unit.Application.Events;
using Features.Unit.Domain;
using Features.Wave.Application;
using Features.Wave.Application.Events;
using Features.Wave.Infrastructure;
using NUnit.Framework;
using Shared.EventBus;
using Shared.Gameplay;
using Shared.Kernel;
using Shared.Math;
using UnityEngine;
using UnitSpec = Features.Unit.Domain.Unit;

namespace Tests.Editor
{
    public sealed class BattleSceneRuntimeSystemsDirectTests
    {
        [Test]
        public void BattleEntity_MoveTo_UsesSpawnAnchorAsRadiusCenter()
        {
            var entity = new BattleEntity(
                new DomainEntityId("battle-unit-1"),
                CreateUnit(anchorRange: 5f),
                new DomainEntityId("player-1"),
                new Float3(0f, 0f, 0f));

            entity.MoveTo(new Float3(4f, 0f, 0f));
            entity.MoveTo(new Float3(8f, 0f, 0f));

            Assert.AreEqual(4f, entity.Position.X);
            Assert.IsFalse(entity.IsWithinAnchorRadius(new Float3(8f, 0f, 0f)));
        }

        [Test]
        public void EnemyMoveTargetResolver_PrioritizesCoreBeforeUnitOrPlayer()
        {
            var players = new FakePlayerPositionQuery(1f, 0f, 1f);
            var core = new FakeCoreObjectiveQuery(hasCore: true, 10f, 0f, 10f);
            var spec = new EnemySpec(
                maxHp: 10f,
                defense: 0f,
                moveSpeed: 1f,
                contactDamage: 1f,
                contactCooldown: 1f,
                targetMode: EnemyTargetMode.ChaseNearestPlayer);

            var resolved = EnemyMoveTargetResolver.TryGetMoveDestination(
                spec,
                0f,
                0f,
                0f,
                players,
                core,
                out var isCoreTarget,
                out var dx,
                out _,
                out var dz);

            Assert.IsTrue(resolved);
            Assert.IsTrue(isCoreTarget);
            Assert.AreEqual(10f, dx);
            Assert.AreEqual(10f, dz);
        }

        [Test]
        public void EnemyMoveTargetResolver_UsesHostileQuery_WhenCoreIsUnavailable()
        {
            var players = new FakePlayerPositionQuery(5f, 0f, 5f);
            var core = new FakeCoreObjectiveQuery(hasCore: false, 10f, 0f, 10f);
            var spec = new EnemySpec(
                maxHp: 10f,
                defense: 0f,
                moveSpeed: 1f,
                contactDamage: 1f,
                contactCooldown: 1f,
                targetMode: EnemyTargetMode.ChaseCore);

            var resolved = EnemyMoveTargetResolver.TryGetMoveDestination(
                spec,
                0f,
                0f,
                0f,
                players,
                core,
                out var isCoreTarget,
                out var dx,
                out _,
                out var dz);

            Assert.IsTrue(resolved);
            Assert.IsFalse(isCoreTarget);
            Assert.AreEqual(5f, dx);
            Assert.AreEqual(5f, dz);
        }

        [Test]
        public void HostilePositionQuery_UsesUnitBeforePlayerWhenCoreIsUnavailable()
        {
            var playerGo = new GameObject("Player");
            var unitGo = new GameObject("Unit");
            var playerQueryGo = new GameObject("PlayerQuery");
            var unitQueryGo = new GameObject("UnitQuery");

            try
            {
                playerGo.transform.position = new Vector3(1f, 0f, 1f);
                unitGo.transform.position = new Vector3(5f, 0f, 5f);

                var playerQuery = playerQueryGo.AddComponent<PlayerPositionQueryAdapter>();
                var unitQuery = unitQueryGo.AddComponent<UnitPositionQueryAdapter>();
                playerQuery.RegisterPlayer(playerGo.transform);
                unitQuery.RegisterUnit(unitGo.transform);

                var hostileQuery = new HostilePositionQuery(playerQuery, unitQuery);
                var target = hostileQuery.GetNearestPlayerPosition(0f, 0f, 0f);

                Assert.AreEqual(5f, target.x);
                Assert.AreEqual(5f, target.z);
            }
            finally
            {
                Object.DestroyImmediate(unitQueryGo);
                Object.DestroyImmediate(playerQueryGo);
                Object.DestroyImmediate(unitGo);
                Object.DestroyImmediate(playerGo);
            }
        }

        [Test]
        public void HostilePositionQuery_FallsBackToPlayerWhenNoUnitIsRegistered()
        {
            var playerGo = new GameObject("Player");
            var playerQueryGo = new GameObject("PlayerQuery");
            var unitQueryGo = new GameObject("UnitQuery");

            try
            {
                playerGo.transform.position = new Vector3(1f, 0f, 1f);

                var playerQuery = playerQueryGo.AddComponent<PlayerPositionQueryAdapter>();
                var unitQuery = unitQueryGo.AddComponent<UnitPositionQueryAdapter>();
                playerQuery.RegisterPlayer(playerGo.transform);

                var hostileQuery = new HostilePositionQuery(playerQuery, unitQuery);
                var target = hostileQuery.GetNearestPlayerPosition(0f, 0f, 0f);

                Assert.AreEqual(1f, target.x);
                Assert.AreEqual(1f, target.z);
            }
            finally
            {
                Object.DestroyImmediate(unitQueryGo);
                Object.DestroyImmediate(playerQueryGo);
                Object.DestroyImmediate(playerGo);
            }
        }

        [Test]
        public void HostilePositionQuery_UsesUnitWithinAggroRadiusBeforePlayer()
        {
            var playerGo = new GameObject("Player");
            var unitGo = new GameObject("Unit");
            var playerQueryGo = new GameObject("PlayerQuery");
            var unitQueryGo = new GameObject("UnitQuery");

            try
            {
                playerGo.transform.position = new Vector3(1f, 0f, 1f);
                unitGo.transform.position = new Vector3(3f, 0f, 0f);

                var playerQuery = playerQueryGo.AddComponent<PlayerPositionQueryAdapter>();
                var unitQuery = unitQueryGo.AddComponent<UnitPositionQueryAdapter>();
                playerQuery.RegisterPlayer(playerGo.transform);
                unitQuery.RegisterUnit(unitGo.transform);

                var hostileQuery = new HostilePositionQuery(playerQuery, unitQuery);
                var found = hostileQuery.TryGetNearestPlayerWithinHorizontalRadius(
                    0f,
                    0f,
                    0f,
                    4f,
                    out var x,
                    out _,
                    out var z);

                Assert.IsTrue(found);
                Assert.AreEqual(3f, x);
                Assert.AreEqual(0f, z);
            }
            finally
            {
                Object.DestroyImmediate(unitQueryGo);
                Object.DestroyImmediate(playerQueryGo);
                Object.DestroyImmediate(unitGo);
                Object.DestroyImmediate(playerGo);
            }
        }

        [Test]
        public void HostilePositionQuery_FallsBackToPlayerWithinAggroRadiusWhenNoUnitMatches()
        {
            var playerGo = new GameObject("Player");
            var farUnitGo = new GameObject("FarUnit");
            var playerQueryGo = new GameObject("PlayerQuery");
            var unitQueryGo = new GameObject("UnitQuery");

            try
            {
                playerGo.transform.position = new Vector3(1f, 0f, 1f);
                farUnitGo.transform.position = new Vector3(9f, 0f, 0f);

                var playerQuery = playerQueryGo.AddComponent<PlayerPositionQueryAdapter>();
                var unitQuery = unitQueryGo.AddComponent<UnitPositionQueryAdapter>();
                playerQuery.RegisterPlayer(playerGo.transform);
                unitQuery.RegisterUnit(farUnitGo.transform);

                var hostileQuery = new HostilePositionQuery(playerQuery, unitQuery);
                var found = hostileQuery.TryGetNearestPlayerWithinHorizontalRadius(
                    0f,
                    0f,
                    0f,
                    4f,
                    out var x,
                    out _,
                    out var z);

                Assert.IsTrue(found);
                Assert.AreEqual(1f, x);
                Assert.AreEqual(1f, z);
            }
            finally
            {
                Object.DestroyImmediate(unitQueryGo);
                Object.DestroyImmediate(playerQueryGo);
                Object.DestroyImmediate(farUnitGo);
                Object.DestroyImmediate(playerGo);
            }
        }

        [Test]
        public void WaveGameEndBridge_UsesElapsedTimeAndReachedWave()
        {
            var eventBus = new EventBus();
            var bridge = new WaveGameEndBridge(
                eventBus,
                eventBus,
                () => 12.5f,
                () => 2);
            GameEndEvent received = default;
            var receivedCount = 0;

            eventBus.Subscribe(this, new System.Action<GameEndEvent>(e =>
            {
                received = e;
                receivedCount++;
            }));

            eventBus.Publish(new WaveDefeatEvent());

            Assert.AreEqual(1, receivedCount);
            Assert.IsFalse(received.IsVictory);
            Assert.AreEqual(2, received.ReachedWave);
            Assert.AreEqual(12.5f, received.PlayTimeSeconds);
            bridge.Dispose();
        }

        [Test]
        public void GameEndAnalytics_ReportsCountedSummonsAndKills()
        {
            var eventBus = new EventBus();
            var analytics = new GameEndAnalytics(eventBus, eventBus);
            GameEndReportRequestedEvent report = default;
            var reportCount = 0;

            eventBus.Subscribe(this, new System.Action<GameEndReportRequestedEvent>(e =>
            {
                report = e;
                reportCount++;
            }));

            eventBus.Publish(new UnitSummonCompletedEvent(
                new DomainEntityId("player-1"),
                new DomainEntityId("battle-unit-1"),
                CreateUnit("frame", "fire", "mobility", 3f)));
            eventBus.Publish(new EnemyDiedEvent(
                new DomainEntityId("enemy-1"),
                new DomainEntityId("battle-unit-1")));
            eventBus.Publish(new GameEndEvent(
                isVictory: false,
                message: "Defeat!",
                reachedWave: 1,
                playTimeSeconds: 9f));

            Assert.AreEqual(1, reportCount);
            Assert.AreEqual(1, report.ReachedWave);
            Assert.AreEqual(9f, report.PlayTimeSeconds);
            Assert.AreEqual(1, report.SummonCount);
            Assert.AreEqual(1, report.UnitKillCount);
            Assert.IsNotNull(report.ContributionCards);
            analytics.Dispose();
        }

        [Test]
        public void GameEndAnalytics_BuildsContributionCardsFromCombatEvents()
        {
            var eventBus = new EventBus();
            var coreId = new DomainEntityId("objective-core");
            var analytics = new GameEndAnalytics(eventBus, eventBus, coreId, 100f);
            GameEndReportRequestedEvent report = default;
            var reportCount = 0;
            var playerId = new DomainEntityId("player-1");
            var unitId = new DomainEntityId("battle-unit-1");
            var enemyId = new DomainEntityId("enemy-1");

            eventBus.Subscribe(this, new System.Action<GameEndReportRequestedEvent>(e =>
            {
                report = e;
                reportCount++;
            }));

            eventBus.Publish(new UnitSummonCompletedEvent(
                playerId,
                unitId,
                CreateUnit("guardian", "single", "heavy", 3f)));
            eventBus.Publish(new EnemySpawnedEvent(enemyId));
            eventBus.Publish(new DamageAppliedEvent(enemyId, 25f, DamageType.Physical, 0f, isDead: true, unitId));
            eventBus.Publish(new DamageAppliedEvent(unitId, 10f, DamageType.Physical, 90f, isDead: false, enemyId));
            eventBus.Publish(new DamageAppliedEvent(coreId, 20f, DamageType.Physical, 80f, isDead: false, enemyId));
            eventBus.Publish(new GameEndEvent(
                isVictory: true,
                message: "Victory!",
                reachedWave: 2,
                playTimeSeconds: 30f));

            Assert.AreEqual(1, reportCount);
            Assert.AreEqual(80f, report.CoreRemainingHealth);
            Assert.AreEqual(100f, report.CoreMaxHealth);
            Assert.LessOrEqual(report.ContributionCards.Length, 3);
            Assert.IsTrue(ContainsContribution(report, ResultContributionKind.KeepCoreAlive));
            Assert.IsTrue(ContainsContribution(report, ResultContributionKind.ClearPressure));
            Assert.IsTrue(ContainsContribution(report, ResultContributionKind.HoldPosition));
            Assert.IsTrue(System.Array.Exists(
                report.ContributionCards,
                card => card.LoadoutKey == "guardian|single|heavy"));
            analytics.Dispose();
        }

        [Test]
        public void GameEndAnalytics_UsesSharedLoadoutKeyForMissingParts()
        {
            var eventBus = new EventBus();
            var analytics = new GameEndAnalytics(eventBus, eventBus);
            GameEndReportRequestedEvent report = default;

            eventBus.Subscribe(this, new System.Action<GameEndReportRequestedEvent>(e => report = e));

            eventBus.Publish(new UnitSummonCompletedEvent(
                new DomainEntityId("player-1"),
                new DomainEntityId("battle-unit-1"),
                CreateUnit(null, "single", " ", 3f)));
            eventBus.Publish(new EnemyDiedEvent(
                new DomainEntityId("enemy-1"),
                new DomainEntityId("battle-unit-1")));
            eventBus.Publish(new GameEndEvent(
                isVictory: true,
                message: "Victory!",
                reachedWave: 1,
                playTimeSeconds: 9f));

            Assert.IsTrue(System.Array.Exists(
                report.ContributionCards,
                card => card.LoadoutKey == "-|single|-"));
            analytics.Dispose();
        }

        [Test]
        public void GameEndAnalytics_DoesNotClaimCorePreservedWhenCoreCollapsed()
        {
            var eventBus = new EventBus();
            var coreId = new DomainEntityId("objective-core");
            var analytics = new GameEndAnalytics(eventBus, eventBus, coreId, 100f);
            GameEndReportRequestedEvent report = default;

            eventBus.Subscribe(this, new System.Action<GameEndReportRequestedEvent>(e => report = e));

            eventBus.Publish(new DamageAppliedEvent(
                coreId,
                100f,
                DamageType.Physical,
                0f,
                isDead: true,
                new DomainEntityId("enemy-1")));
            eventBus.Publish(new GameEndEvent(
                isVictory: false,
                message: "Defeat!",
                reachedWave: 1,
                playTimeSeconds: 30f));

            Assert.AreEqual(0f, report.CoreRemainingHealth);
            Assert.IsFalse(ContainsContribution(report, ResultContributionKind.KeepCoreAlive));
            Assert.IsTrue(report.ContributionCards.Length > 0);
            analytics.Dispose();
        }

        private static UnitSpec CreateUnit(float anchorRange)
        {
            return CreateUnit("frame", "fire", "mobility", anchorRange);
        }

        private static UnitSpec CreateUnit(
            string frameId,
            string firepowerId,
            string mobilityId,
            float anchorRange)
        {
            return new UnitSpec(
                new DomainEntityId("unit-1"),
                frameId,
                "Unit",
                firepowerId,
                mobilityId,
                "",
                0,
                100f,
                2f,
                10f,
                1f,
                5f,
                4f,
                3f,
                anchorRange,
                1,
                1,
                1,
                3);
        }

        private static bool ContainsContribution(GameEndReportRequestedEvent report, ResultContributionKind kind)
        {
            for (var i = 0; i < report.ContributionCards.Length; i++)
            {
                if (report.ContributionCards[i].Kind == kind)
                    return true;
            }

            return false;
        }

        private sealed class FakePlayerPositionQuery : IPlayerPositionQuery
        {
            private readonly float _x;
            private readonly float _y;
            private readonly float _z;

            public FakePlayerPositionQuery(float x, float y, float z)
            {
                _x = x;
                _y = y;
                _z = z;
            }

            public (float x, float y, float z) GetNearestPlayerPosition(float fromX, float fromY, float fromZ)
            {
                return (_x, _y, _z);
            }

            public bool TryGetNearestPlayerWithinHorizontalRadius(
                float fromX,
                float fromY,
                float fromZ,
                float radius,
                out float tx,
                out float ty,
                out float tz)
            {
                tx = _x;
                ty = _y;
                tz = _z;
                return true;
            }
        }

        private sealed class FakeCoreObjectiveQuery : ICoreObjectiveQuery
        {
            private readonly bool _hasCore;
            private readonly float _x;
            private readonly float _y;
            private readonly float _z;

            public FakeCoreObjectiveQuery(bool hasCore, float x, float y, float z)
            {
                _hasCore = hasCore;
                _x = x;
                _y = y;
                _z = z;
            }

            public DomainEntityId CoreId => new DomainEntityId("objective-core");
            public float CoreMaxHp => 100f;

            public bool TryGetCoreWorldPosition(out float x, out float y, out float z)
            {
                x = _x;
                y = _y;
                z = _z;
                return _hasCore;
            }
        }
    }
}
