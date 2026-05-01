using Features.Garage.Presentation;
using Features.Player.Domain;
using NUnit.Framework;
using Shared.Kernel;
using System.Collections.Generic;
using UnityEngine;
using ComposedUnit = Features.Unit.Domain.Unit;

namespace Tests.Editor
{
    public sealed class GaragePagePresenterDirectTests
    {
        [Test]
        public void ReadyUnlocked_WhenCommittedRosterIsValidAndDraftIsClean()
        {
            var state = CreateInitializedState(3);
            var presenter = new GaragePagePresenter(null);
            var evaluation = CreateEvaluation(state, resultSuccess: true);

            var viewModel = presenter.BuildResultViewModel(state, evaluation);

            Assert.IsTrue(viewModel.IsReady);
            Assert.IsFalse(viewModel.IsDirty);
            Assert.IsFalse(viewModel.CanSave);
            Assert.AreEqual("현역 편성", viewModel.PrimaryActionLabel);
        }

        [Test]
        public void DraftChanges_BlockReady_AndSwitchPrimaryActionToSave()
        {
            var state = CreateInitializedState(3);
            state.SetEditingFrameId("frame-updated");

            var presenter = new GaragePagePresenter(null);
            var evaluation = CreateEvaluation(state, resultSuccess: true);

            var viewModel = presenter.BuildResultViewModel(state, evaluation);

            Assert.IsFalse(viewModel.IsReady);
            Assert.IsTrue(viewModel.IsDirty);
            Assert.IsTrue(viewModel.CanSave);
            Assert.AreEqual("출격 편성 저장", viewModel.PrimaryActionLabel);
            StringAssert.Contains("기체 편성 갱신 대기", viewModel.RosterStatusText);
        }

        [Test]
        public void CommitDraft_RestoresReadyState()
        {
            var state = CreateInitializedState(3);
            state.SetEditingFrameId("frame-updated");
            state.CommitDraft();

            var presenter = new GaragePagePresenter(null);
            var evaluation = CreateEvaluation(state, resultSuccess: true);

            var viewModel = presenter.BuildResultViewModel(state, evaluation);

            Assert.IsTrue(viewModel.IsReady);
            Assert.IsFalse(viewModel.IsDirty);
            Assert.IsFalse(viewModel.CanSave);
            StringAssert.Contains("출격 편성 동기화", viewModel.RosterStatusText);
        }

        [Test]
        public void SlotViewModel_UsesDeterministicCallsignAndIdentityCopy()
        {
            var state = CreateInitializedState(3);
            var presenter = new GaragePagePresenter(CreateCatalog());

            var viewModels = presenter.BuildSlotViewModels(state);

            Assert.AreEqual("A-01", viewModels[0].SlotLabel);
            Assert.AreEqual("A-01 가디언", viewModels[0].Title);
            Assert.AreEqual("전선 고정", viewModels[0].RoleLabel);
            Assert.AreEqual("전적 기록 대기중", viewModels[0].ServiceTagText);
            Assert.AreEqual("frame0|fire0|mob0", viewModels[0].LoadoutKey);
            StringAssert.Contains("단일탄 / 중장갑", viewModels[0].Summary);
        }

        [Test]
        public void SlotViewModel_UsesServiceTagByLoadoutKeyWhenProvided()
        {
            var state = CreateInitializedState(3);
            var presenter = new GaragePagePresenter(CreateCatalog());
            var serviceTags = new Dictionary<string, GarageUnitServiceTag>
            {
                ["frame0|fire0|mob0"] = GarageUnitServiceTag.CoreNearBlocks(31),
            };

            var viewModels = presenter.BuildSlotViewModels(state, serviceTags);

            Assert.AreEqual("코어 근접 차단 31회", viewModels[0].ServiceTagText);
        }

        [Test]
        public void OperationRecordServiceTagMapper_MapsPrimaryRosterUnitsToLoadoutTags()
        {
            var state = CreateInitializedState(3);
            var presenter = new GaragePagePresenter(CreateCatalog());
            var records = CreateRecentOperations("frame0|fire0|mob0");
            var serviceTags = GarageOperationRecordServiceTagMapper.BuildByLoadoutKey(records);

            var viewModels = presenter.BuildSlotViewModels(state, serviceTags);

            Assert.AreEqual("최근 주요 기여 기체", viewModels[0].ServiceTagText);
            Assert.AreEqual("전적 기록 대기중", viewModels[1].ServiceTagText);
        }

        [Test]
        public void SlotViewModel_UsesAssemblyPrefabsForCompletePreviewWhenAvailable()
        {
            var framePreview = new GameObject("FramePreview");
            var frameAssembly = new GameObject("FrameAssembly");
            var firepowerPreview = new GameObject("FirepowerPreview");
            var firepowerAssembly = new GameObject("FirepowerAssembly");
            var mobilityPreview = new GameObject("MobilityPreview");
            var mobilityAssembly = new GameObject("MobilityAssembly");

            try
            {
                var state = CreateInitializedState(1);
                var catalog = new GaragePanelCatalog(
                    new[]
                    {
                        new GaragePanelCatalog.FrameOption
                        {
                            Id = "frame0",
                            DisplayName = "가디언",
                            PreviewPrefab = framePreview,
                            AssemblyPrefab = frameAssembly
                        },
                    },
                    new[]
                    {
                        new GaragePanelCatalog.FirepowerOption
                        {
                            Id = "fire0",
                            DisplayName = "단일탄",
                            PreviewPrefab = firepowerPreview,
                            AssemblyPrefab = firepowerAssembly
                        },
                    },
                    new[]
                    {
                        new GaragePanelCatalog.MobilityOption
                        {
                            Id = "mob0",
                            DisplayName = "중장갑",
                            PreviewPrefab = mobilityPreview,
                            AssemblyPrefab = mobilityAssembly
                        },
                    });
                var presenter = new GaragePagePresenter(catalog);

                var viewModels = presenter.BuildSlotViewModels(state);

                Assert.AreSame(frameAssembly, viewModels[0].FramePreviewPrefab);
                Assert.AreSame(firepowerAssembly, viewModels[0].FirepowerPreviewPrefab);
                Assert.AreSame(mobilityAssembly, viewModels[0].MobilityPreviewPrefab);
            }
            finally
            {
                Object.DestroyImmediate(framePreview);
                Object.DestroyImmediate(frameAssembly);
                Object.DestroyImmediate(firepowerPreview);
                Object.DestroyImmediate(firepowerAssembly);
                Object.DestroyImmediate(mobilityPreview);
                Object.DestroyImmediate(mobilityAssembly);
            }
        }

        [Test]
        public void ResultViewModel_ShowsLatestOperationSummaryWhenNoDraftStats()
        {
            var state = CreateInitializedState(0);
            var presenter = new GaragePagePresenter(null);
            var evaluation = CreateEvaluation(state, resultSuccess: true);
            var records = CreateRecentOperations();
            var operationSummary = GarageOperationRecordSummaryFormatter.BuildSummary(records);

            var viewModel = presenter.BuildResultViewModel(state, evaluation, operationSummary);

            StringAssert.Contains("작전 1/5: 버텨냄", viewModel.StatsText);
            StringAssert.Contains("공세3", viewModel.StatsText);
            StringAssert.Contains("2:05", viewModel.StatsText);
        }

        [Test]
        public void ResultViewModel_FallsBackWhenNoOperationRecordExists()
        {
            var state = CreateInitializedState(0);
            var presenter = new GaragePagePresenter(null);
            var evaluation = CreateEvaluation(state, resultSuccess: true);

            var viewModel = presenter.BuildResultViewModel(state, evaluation);

            Assert.AreEqual("최근 작전 기록 없음", viewModel.StatsText);
        }

        private static GaragePageState CreateInitializedState(int completeUnitCount)
        {
            var roster = new Features.Garage.Domain.GarageRoster();
            for (int i = 0; i < completeUnitCount; i++)
            {
                roster.SetSlot(i, new Features.Garage.Domain.GarageRoster.UnitLoadout(
                    $"frame{i}",
                    $"fire{i}",
                    $"mob{i}"));
            }

            var state = new GaragePageState();
            state.Initialize(roster);
            return state;
        }

        private static GarageDraftEvaluation CreateEvaluation(GaragePageState state, bool resultSuccess)
        {
            var composeFailure = Result<ComposedUnit>.Failure("compose-not-needed");
            var rosterValidation = resultSuccess ? Result.Success() : Result.Failure("validation failed");
            return GarageDraftEvaluation.Create(state, false, composeFailure, rosterValidation);
        }

        private static GaragePanelCatalog CreateCatalog()
        {
            return new GaragePanelCatalog(
                new[]
                {
                    new GaragePanelCatalog.FrameOption { Id = "frame0", DisplayName = "가디언" },
                },
                new[]
                {
                    new GaragePanelCatalog.FirepowerOption { Id = "fire0", DisplayName = "단일탄", Range = 4f },
                },
                new[]
                {
                    new GaragePanelCatalog.MobilityOption { Id = "mob0", DisplayName = "중장갑", AnchorRange = 3f },
                });
        }

        private static RecentOperationRecords CreateRecentOperations(string primaryRosterUnit = null)
        {
            var records = new RecentOperationRecords();
            var record = new OperationRecord
            {
                operationId = "operation-1",
                endedAtUnixMs = 1000,
                result = OperationRecordResult.Held,
                reachedWave = 3,
                survivalSeconds = 125f,
                hasCoreHealthPercent = true,
                coreHealthPercent = 0.42f,
                summonCount = 4,
                unitKillCount = 2
            };

            if (!string.IsNullOrWhiteSpace(primaryRosterUnit))
                record.primaryRosterUnits.Add(primaryRosterUnit);

            records.AddOrReplace(record);
            return records;
        }
    }
}
