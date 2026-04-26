using Features.Enemy.Application;
using Features.Enemy.Application.Ports;
using Features.Enemy.Domain;
using Features.Unit.Domain;
using Features.Wave.Infrastructure;
using NUnit.Framework;
using Shared.Kernel;
using Shared.Math;
using UnityEngine;
using UnitSpec = Features.Unit.Domain.Unit;

namespace Tests.Editor
{
    public sealed class GameSceneRuntimeSystemsDirectTests
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

        private static UnitSpec CreateUnit(float anchorRange)
        {
            return new UnitSpec(
                new DomainEntityId("unit-1"),
                "frame",
                "Unit",
                "fire",
                "mobility",
                "",
                0,
                100f,
                10f,
                1f,
                5f,
                3f,
                anchorRange,
                3);
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
