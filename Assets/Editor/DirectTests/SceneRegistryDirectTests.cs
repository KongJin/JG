using System.Collections.Generic;
using Features.Enemy;
using Features.Player;
using NUnit.Framework;
using UnityEngine;

namespace Tests.Editor
{
    public sealed class SceneRegistryDirectTests
    {
        [Test]
        public void PlayerSceneRegistry_OnlyReplaysArrivalsWithoutSubscriber()
        {
            var registryGo = new GameObject("PlayerSceneRegistryTest");
            var firstPlayerGo = new GameObject("FirstPlayer");
            var secondPlayerGo = new GameObject("SecondPlayer");

            try
            {
                var registry = registryGo.AddComponent<PlayerSceneRegistry>();
                var firstPlayer = firstPlayerGo.AddComponent<PlayerSetup>();
                var secondPlayer = secondPlayerGo.AddComponent<PlayerSetup>();
                var received = new List<PlayerSetup>();

                registry.NotifyArrived(firstPlayer);
                registry.DrainPendingArrivals(received.Add);

                void OnArrived(PlayerSetup setup) => received.Add(setup);
                registry.PlayerArrived += OnArrived;
                registry.NotifyArrived(secondPlayer);
                registry.DrainPendingArrivals(received.Add);
                registry.PlayerArrived -= OnArrived;

                CollectionAssert.AreEqual(
                    new[] { firstPlayer, secondPlayer },
                    received);
            }
            finally
            {
                Object.DestroyImmediate(secondPlayerGo);
                Object.DestroyImmediate(firstPlayerGo);
                Object.DestroyImmediate(registryGo);
            }
        }

        [Test]
        public void EnemySceneRegistry_OnlyReplaysArrivalsWithoutSubscriber()
        {
            var registryGo = new GameObject("EnemySceneRegistryTest");
            var firstEnemyGo = new GameObject("FirstEnemy");
            var secondEnemyGo = new GameObject("SecondEnemy");

            try
            {
                var registry = registryGo.AddComponent<EnemySceneRegistry>();
                var firstEnemy = firstEnemyGo.AddComponent<EnemySetup>();
                var secondEnemy = secondEnemyGo.AddComponent<EnemySetup>();
                var received = new List<EnemySetup>();

                registry.NotifyArrived(firstEnemy);
                registry.DrainPendingArrivals(received.Add);

                void OnArrived(EnemySetup setup) => received.Add(setup);
                registry.EnemyArrived += OnArrived;
                registry.NotifyArrived(secondEnemy);
                registry.DrainPendingArrivals(received.Add);
                registry.EnemyArrived -= OnArrived;

                CollectionAssert.AreEqual(
                    new[] { firstEnemy, secondEnemy },
                    received);
            }
            finally
            {
                Object.DestroyImmediate(secondEnemyGo);
                Object.DestroyImmediate(firstEnemyGo);
                Object.DestroyImmediate(registryGo);
            }
        }
    }
}
