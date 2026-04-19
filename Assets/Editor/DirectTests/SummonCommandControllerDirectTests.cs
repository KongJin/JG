using Features.Unit.Application;
using Features.Unit.Application.Ports;
using Features.Unit.Presentation;
using NUnit.Framework;
using Shared.EventBus;
using Shared.Kernel;
using Shared.Math;
using UnityEngine;
using UnitSpec = Features.Unit.Domain.Unit;

namespace Tests.Editor
{
    public sealed class SummonCommandControllerDirectTests
    {
        private GameObject _root;
        private SummonCommandController _controller;
        private EventBus _eventBus;
        private FakeSummonExecutionPort _summonPort;
        private SummonUnitUseCase _summonUseCase;
        private PlacementArea _placementArea;
        private DomainEntityId _ownerId;
        private FakeUnitEnergyPort _energyPort;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("SummonCommandControllerTest");
            _controller = _root.AddComponent<SummonCommandController>();
            _eventBus = new EventBus();
            _energyPort = new FakeUnitEnergyPort(100f);
            _summonPort = new FakeSummonExecutionPort();
            _summonUseCase = new SummonUnitUseCase(_energyPort, _summonPort, _eventBus);
            _placementArea = new PlacementArea(width: 8f, depth: 5f, forwardOffset: 0f);
            _placementArea.SetCorePosition(new Vector3(0f, 0f, 4f));
            _ownerId = new DomainEntityId("player-1");

            var cameraObject = new GameObject("Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 10f, -10f);
            camera.transform.rotation = Quaternion.Euler(45f, 0f, 0f);
            cameraObject.transform.SetParent(_root.transform, false);

            _controller.Initialize(
                _eventBus,
                _summonUseCase,
                _ownerId,
                _placementArea,
                null,
                null,
                camera);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
        }

        [Test]
        public void SelectingSlot_EntersActiveSelectionState_WithoutSummoning()
        {
            var unitSpec = CreateUnitSpec(cost: 25);

            var selected = _controller.TrySelectUnit(unitSpec, slotIndex: 1, canAfford: true);

            Assert.IsTrue(selected);
            Assert.IsTrue(_controller.SelectionState.IsActive);
            Assert.AreEqual(1, _controller.SelectionState.SelectedSlotIndex);
            Assert.AreEqual(unitSpec, _controller.SelectionState.SelectedUnit);
            Assert.AreEqual(0, _summonPort.SpawnCount);
        }

        [Test]
        public void ValidPlacement_ConsumesEnergy_SpawnsUnit_AndClearsSelection()
        {
            var unitSpec = CreateUnitSpec(cost: 30);
            _controller.TrySelectUnit(unitSpec, slotIndex: 0, canAfford: true);

            var success = _controller.TryConfirmPlacementWorld(new Vector3(0f, 0f, 4f));

            Assert.IsTrue(success);
            Assert.AreEqual(1, _summonPort.SpawnCount);
            Assert.AreEqual(70f, _energyPort.CurrentEnergy);
            Assert.IsFalse(_controller.SelectionState.IsActive);
        }

        [Test]
        public void InvalidPlacement_KeepsSelectionActive_AndShowsDockFeedback()
        {
            var unitSpec = CreateUnitSpec(cost: 30);
            _controller.TrySelectUnit(unitSpec, slotIndex: 2, canAfford: true);

            var success = _controller.TryConfirmPlacementWorld(new Vector3(99f, 0f, 99f));

            Assert.IsFalse(success);
            Assert.IsTrue(_controller.SelectionState.IsActive);
            Assert.AreEqual("배치 영역 밖", _controller.CurrentFeedbackMessage);
            Assert.AreEqual(0, _summonPort.SpawnCount);
        }

        [Test]
        public void InsufficientEnergy_BlocksSelection_AndShowsNeedEnergy()
        {
            var unitSpec = CreateUnitSpec(cost: 120);

            var selected = _controller.TrySelectUnit(unitSpec, slotIndex: 0, canAfford: false);

            Assert.IsFalse(selected);
            Assert.IsFalse(_controller.SelectionState.IsActive);
            Assert.AreEqual("Need Energy", _controller.CurrentFeedbackMessage);
        }

        private static UnitSpec CreateUnitSpec(int cost)
        {
            return new UnitSpec(
                DomainEntityId.New(),
                "frame-bastion",
                "frame_bastion",
                "fire-basic",
                "mobility-basic",
                "trait-none",
                0,
                100f,
                10f,
                1f,
                4f,
                3f,
                2.5f,
                cost);
        }

        private sealed class FakeUnitEnergyPort : IUnitEnergyPort
        {
            public FakeUnitEnergyPort(float currentEnergy)
            {
                CurrentEnergy = currentEnergy;
            }

            public float CurrentEnergy { get; private set; }

            public bool TrySpendEnergy(DomainEntityId ownerId, float cost)
            {
                if (CurrentEnergy < cost)
                    return false;

                CurrentEnergy -= cost;
                return true;
            }

            public float GetCurrentEnergy(DomainEntityId ownerId)
            {
                return CurrentEnergy;
            }
        }

        private sealed class FakeSummonExecutionPort : ISummonExecutionPort
        {
            public int SpawnCount { get; private set; }

            public DomainEntityId SpawnBattleEntity(UnitSpec unitSpec, Float3 spawnPosition, DomainEntityId ownerId)
            {
                SpawnCount++;
                return DomainEntityId.New();
            }
        }
    }
}
