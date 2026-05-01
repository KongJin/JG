using ExitGames.Client.Photon;
using Features.Player.Domain;
using Features.Player.Infrastructure;
using NUnit.Framework;

namespace Tests.Editor
{
    public sealed class PlayerNetworkPropertyReaderDirectTests
    {
        [Test]
        public void TryReadHealthChange_UsesCurrentMaxHealthForPartialUpdate()
        {
            var changedProps = new Hashtable
            {
                { PlayerNetworkPropertyKeys.Health, 42f }
            };
            var currentProps = new Hashtable
            {
                { PlayerNetworkPropertyKeys.MaxHealth, 160f }
            };

            Assert.IsTrue(PlayerNetworkPropertyReader.TryReadHealthChange(
                changedProps,
                currentProps,
                out var snapshot));
            Assert.AreEqual(42f, snapshot.Current);
            Assert.AreEqual(160f, snapshot.Max);
        }

        [Test]
        public void TryReadHealthChange_UsesDefaultMaxHealthOnlyWhenNoMaxExists()
        {
            var changedProps = new Hashtable
            {
                { PlayerNetworkPropertyKeys.Health, 12f }
            };

            Assert.IsTrue(PlayerNetworkPropertyReader.TryReadHealthChange(
                changedProps,
                new Hashtable(),
                out var snapshot));
            Assert.AreEqual(12f, snapshot.Current);
            Assert.AreEqual(100f, snapshot.Max);
        }

        [Test]
        public void TryReadHydratedHealth_RequiresCompleteHealthSnapshot()
        {
            var props = new Hashtable
            {
                { PlayerNetworkPropertyKeys.Health, 42f }
            };

            Assert.IsFalse(PlayerNetworkPropertyReader.TryReadHydratedHealth(props, out _));
        }

        [Test]
        public void TryReadEnergyChange_UsesCurrentMaxEnergyForPartialUpdate()
        {
            var changedProps = new Hashtable
            {
                { PlayerNetworkPropertyKeys.Energy, 38f }
            };
            var currentProps = new Hashtable
            {
                { PlayerNetworkPropertyKeys.MaxEnergy, 140f }
            };

            Assert.IsTrue(PlayerNetworkPropertyReader.TryReadEnergyChange(
                changedProps,
                currentProps,
                out var snapshot));
            Assert.AreEqual(38f, snapshot.Current);
            Assert.AreEqual(140f, snapshot.Max);
        }

        [Test]
        public void TryReadHydratedEnergy_UsesDefaultMaxEnergyForLegacySnapshot()
        {
            var props = new Hashtable
            {
                { PlayerNetworkPropertyKeys.Energy, 65f }
            };

            Assert.IsTrue(PlayerNetworkPropertyReader.TryReadHydratedEnergy(props, out var snapshot));
            Assert.AreEqual(65f, snapshot.Current);
            Assert.AreEqual(100f, snapshot.Max);
        }

        [Test]
        public void CreateLifeStateProperties_RoundTripsLifeState()
        {
            var props = PlayerNetworkPropertyReader.CreateLifeStateProperties(LifeState.Dead);

            Assert.IsTrue(PlayerNetworkPropertyReader.TryReadLifeState(props, out var state));
            Assert.AreEqual(LifeState.Dead, state);
        }
    }
}
