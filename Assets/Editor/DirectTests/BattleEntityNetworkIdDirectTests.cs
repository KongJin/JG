using System.Reflection;
using Features.Unit.Infrastructure;
using NUnit.Framework;
using Photon.Pun;
using Shared.Kernel;
using UnityEngine;
using UnitSpec = Features.Unit.Domain.Unit;

namespace Tests.Editor
{
    public sealed class BattleEntityNetworkIdDirectTests
    {
        private static readonly FieldInfo PhotonViewIdField =
            typeof(PhotonView).GetField("viewIdField", BindingFlags.Instance | BindingFlags.NonPublic);

        [Test]
        public void TryBuild_UsesPhotonViewId_WhenAllocated()
        {
            var go = new GameObject("BattleEntityNetworkIdTest");
            var view = go.AddComponent<PhotonView>();
            PhotonViewIdField.SetValue(view, 1234);
            var unit = CreateUnit("unit-1");

            var result = BattleEntityNetworkId.TryBuild(unit, view, out var battleEntityId);

            Assert.IsTrue(result);
            Assert.AreEqual("battle-unit-1-view-1234", battleEntityId.Value);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void TryBuild_RejectsMissingPhotonViewId()
        {
            var go = new GameObject("BattleEntityNetworkIdMissingViewIdTest");
            var view = go.AddComponent<PhotonView>();
            var unit = CreateUnit("unit-1");

            var result = BattleEntityNetworkId.TryBuild(unit, view, out var battleEntityId);

            Assert.IsFalse(result);
            Assert.IsTrue(string.IsNullOrWhiteSpace(battleEntityId.Value));

            Object.DestroyImmediate(go);
        }

        [Test]
        public void TryBuild_RejectsMissingPhotonView()
        {
            var unit = CreateUnit("unit-1");

            var result = BattleEntityNetworkId.TryBuild(unit, null, out var battleEntityId);

            Assert.IsFalse(result);
            Assert.IsTrue(string.IsNullOrWhiteSpace(battleEntityId.Value));
        }

        private static UnitSpec CreateUnit(string id)
        {
            return new UnitSpec(
                new DomainEntityId(id),
                "frame",
                "Unit",
                "fire",
                "mobility",
                "",
                0,
                100f,
                2f,
                10f,
                1f,
                5f,
                4f,
                3f,
                3f,
                1,
                0,
                0,
                1);
        }
    }
}
