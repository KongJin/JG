using Features.Garage.Application;
using Features.Garage.Application.Ports;
using Features.Garage.Domain;
using Features.Garage.Infrastructure;
using Features.Garage.Presentation;
using Features.Player.Domain;
using Features.Unit.Application;
using Features.Unit.Domain;
using NUnit.Framework;
using Shared.Kernel;
using System.Collections.Generic;
using System.Threading.Tasks;
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
        public void SingleCompleteDraft_CanSaveWithoutUnlockingReady()
        {
            var catalog = CreateCatalog();
            var state = CreateInitializedState(0);
            state.SetEditingFrameId("frame0");
            state.SetEditingFirepowerId("fire0");
            state.SetEditingMobilityId("mob0");

            var evaluation = GarageDraftEvaluator.Evaluate(
                state,
                catalog,
                new ComposeUnitUseCase(new ValidUnitCompositionPort()),
                new ValidateRosterUseCase(new AlwaysValidRosterValidationProvider()));
            var presenter = new GaragePagePresenter(catalog);

            var viewModel = presenter.BuildResultViewModel(state, evaluation);

            Assert.IsTrue(evaluation.HasDraftChanges);
            Assert.IsTrue(evaluation.HasCompleteDraft);
            Assert.IsTrue(evaluation.CanSave);
            Assert.IsFalse(state.DraftRoster.IsValid);
            Assert.IsFalse(viewModel.IsReady);
            Assert.IsTrue(viewModel.CanSave);
        }

        [Test]
        public void SelectedCompleteDraft_CanSaveWhenAnotherSlotHasIncompleteDraft()
        {
            var catalog = CreateCatalog();
            var state = CreateInitializedState(0);
            state.SelectSlot(1);
            state.SetEditingFrameId("frame1");
            state.SelectSlot(0);
            state.SetEditingFrameId("frame0");
            state.SetEditingFirepowerId("fire0");
            state.SetEditingMobilityId("mob0");

            var evaluation = GarageDraftEvaluator.Evaluate(
                state,
                catalog,
                new ComposeUnitUseCase(new ValidUnitCompositionPort()),
                new ValidateRosterUseCase(new AlwaysValidRosterValidationProvider()));
            var presenter = new GaragePagePresenter(catalog);

            var viewModel = presenter.BuildResultViewModel(state, evaluation);

            Assert.IsTrue(state.GetSelectedDraftSlot().IsComplete);
            Assert.IsTrue(evaluation.CanSave);
            Assert.IsTrue(viewModel.CanSave);
        }

        [Test]
        public void MigratedLegacySavedRoster_AllowsSavingNewNovaSelectedSlot()
        {
            var catalog = CreateKnownNovaCatalog();
            var legacyRoster = CreateLegacyRosterFixture();
            var migratedRoster = new GarageRosterLegacyIdMigrator().Migrate(legacyRoster);
            var state = new GaragePageState();
            state.Initialize(migratedRoster);
            state.SelectSlot(0);
            state.SetEditingFrameId("nova_frame_body11_kn");
            state.SetEditingFirepowerId("nova_fire_arm33_hkoo");
            state.SetEditingMobilityId("nova_mob_g_legs58_pps");

            var evaluation = GarageDraftEvaluator.Evaluate(
                state,
                catalog,
                new ComposeUnitUseCase(new ValidUnitCompositionPort()),
                new ValidateRosterUseCase(new KnownNovaRosterValidationProvider()));
            var viewModel = new GaragePagePresenter(catalog).BuildResultViewModel(state, evaluation);

            Assert.IsTrue(evaluation.CanSave);
            Assert.IsTrue(viewModel.CanSave);
            Assert.AreEqual("출격 편성 저장", viewModel.PrimaryActionLabel);
        }

        [Test]
        public void UnknownSavedRosterId_RemainsBlockedAndShowsValidationReason()
        {
            var catalog = CreateKnownNovaCatalog();
            var roster = new GarageRoster();
            roster.SetSlot(1, new GarageRoster.UnitLoadout(
                "unknown_frame",
                "unknown_fire",
                "unknown_mob"));
            var state = new GaragePageState();
            state.Initialize(roster);
            state.SelectSlot(0);
            state.SetEditingFrameId("nova_frame_body11_kn");
            state.SetEditingFirepowerId("nova_fire_arm33_hkoo");
            state.SetEditingMobilityId("nova_mob_g_legs58_pps");

            var evaluation = GarageDraftEvaluator.Evaluate(
                state,
                catalog,
                new ComposeUnitUseCase(new ValidUnitCompositionPort()),
                new ValidateRosterUseCase(new KnownNovaRosterValidationProvider()));
            var viewModel = new GaragePagePresenter(catalog).BuildResultViewModel(state, evaluation);

            Assert.IsFalse(evaluation.CanSave);
            Assert.IsFalse(viewModel.CanSave);
            StringAssert.Contains("슬롯 2", viewModel.ValidationText);
            StringAssert.Contains("데이터를 찾을 수 없습니다", viewModel.ValidationText);
        }

        [Test]
        public void RosterMigrator_MapsKnownLegacyRosterIdsToNovaIds()
        {
            var roster = new GarageRoster();
            roster.SetSlot(0, new GarageRoster.UnitLoadout(
                "frame_bastion",
                "fire_scatter",
                "mob_burst"));

            var migrated = new GarageRosterLegacyIdMigrator().Migrate(roster);
            var slot = migrated.GetSlot(0);

            Assert.AreEqual("nova_frame_body25_bosro", slot.frameId);
            Assert.AreEqual("nova_fire_arm1_sz", slot.firepowerModuleId);
            Assert.AreEqual("nova_mob_legs10_prg", slot.mobilityModuleId);
            Assert.AreSame(migrated, new GarageRosterLegacyIdMigrator().Migrate(migrated));
        }

        [Test]
        public async Task InitializeGarage_MigratesLocalRosterBeforeSyncAndPersistence()
        {
            var persistence = new FakePersistence
            {
                LoadedRoster = new GarageRoster()
            };
            persistence.LoadedRoster.SetSlot(0, new GarageRoster.UnitLoadout(
                "frame_striker",
                "fire_pulse",
                "mob_vector"));
            var network = new FakeNetwork();
            var useCase = new InitializeGarageUseCase(
                persistence,
                network,
                cloudPort: null,
                new GarageRosterLegacyIdMigrator());

            var roster = await useCase.Execute();

            Assert.AreEqual("nova_frame_body1_sz", roster.GetSlot(0).frameId);
            Assert.AreEqual("nova_fire_arm1_sz", roster.GetSlot(0).firepowerModuleId);
            Assert.AreEqual("nova_mob_legs1_rdrn", roster.GetSlot(0).mobilityModuleId);
            Assert.AreEqual("nova_frame_body1_sz", persistence.SavedRoster.GetSlot(0).frameId);
            Assert.AreEqual("nova_frame_body1_sz", network.SyncedRoster.GetSlot(0).frameId);
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
                    new GaragePanelCatalog.MobilityOption { Id = "mob0", DisplayName = "중장갑", MoveRange = 3f },
                });
        }

        private static GaragePanelCatalog CreateKnownNovaCatalog()
        {
            return new GaragePanelCatalog(
                new[]
                {
                    new GaragePanelCatalog.FrameOption { Id = "nova_frame_body11_kn", DisplayName = "커널" },
                },
                new[]
                {
                    new GaragePanelCatalog.FirepowerOption { Id = "nova_fire_arm33_hkoo", DisplayName = "호크아이", Range = 6.25f },
                },
                new[]
                {
                    new GaragePanelCatalog.MobilityOption { Id = "nova_mob_g_legs58_pps", DisplayName = "포퍼스G", MoveRange = 5.1f },
                });
        }

        private static GarageRoster CreateLegacyRosterFixture()
        {
            var roster = new GarageRoster();
            roster.SetSlot(0, new GarageRoster.UnitLoadout("frame_bastion", "fire_scatter", "mob_burst"));
            roster.SetSlot(1, new GarageRoster.UnitLoadout("frame_striker", "fire_pulse", "mob_vector"));
            roster.SetSlot(2, new GarageRoster.UnitLoadout("frame_bastion", "fire_pulse", "mob_burst"));
            roster.SetSlot(3, new GarageRoster.UnitLoadout("frame_relay", "fire_scatter", "mob_burst"));
            return roster;
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

        private sealed class AlwaysValidRosterValidationProvider : ValidateRosterUseCase.IRosterValidationProvider
        {
            public bool TryValidateComposition(
                string frameId,
                string firepowerModuleId,
                string mobilityModuleId,
                out string errorMessage)
            {
                errorMessage = null;
                return true;
            }
        }

        private sealed class KnownNovaRosterValidationProvider : ValidateRosterUseCase.IRosterValidationProvider
        {
            private readonly HashSet<string> _frameIds = new()
            {
                "nova_frame_body25_bosro",
                "nova_frame_body1_sz",
                "nova_frame_body11_kn",
            };

            private readonly HashSet<string> _firepowerIds = new()
            {
                "nova_fire_arm10_broz",
                "nova_fire_arm1_sz",
                "nova_fire_arm33_hkoo",
            };

            private readonly HashSet<string> _mobilityIds = new()
            {
                "nova_mob_legs10_prg",
                "nova_mob_legs1_rdrn",
                "nova_mob_g_legs58_pps",
            };

            public bool TryValidateComposition(
                string frameId,
                string firepowerModuleId,
                string mobilityModuleId,
                out string errorMessage)
            {
                if (!_frameIds.Contains(frameId))
                {
                    errorMessage = "중단(프레임) 데이터를 찾을 수 없습니다.";
                    return false;
                }

                if (!_firepowerIds.Contains(firepowerModuleId) || !_mobilityIds.Contains(mobilityModuleId))
                {
                    errorMessage = "상단(무장) 또는 하단(기동) 데이터를 찾을 수 없습니다.";
                    return false;
                }

                errorMessage = null;
                return true;
            }
        }

        private sealed class FakePersistence : IGaragePersistencePort
        {
            public GarageRoster LoadedRoster { get; set; }
            public GarageRoster SavedRoster { get; private set; }

            public void Save(GarageRoster roster)
            {
                SavedRoster = roster?.Clone();
            }

            public GarageRoster Load()
            {
                return LoadedRoster;
            }

            public void Delete()
            {
                LoadedRoster = null;
            }
        }

        private sealed class FakeNetwork : IGarageNetworkPort
        {
            public GarageRoster SyncedRoster { get; private set; }

            public void SyncRoster(GarageRoster roster)
            {
                SyncedRoster = roster?.Clone();
            }

            public void SyncReady(bool isReady)
            {
            }

            public GarageRoster GetPlayerRoster(object playerId)
            {
                return null;
            }

            public bool IsPlayerReady(object playerId)
            {
                return false;
            }

            public Dictionary<object, GarageRoster> GetAllPlayersRosters()
            {
                return new Dictionary<object, GarageRoster>();
            }

            public GarageRoster GetLocalPlayerRoster()
            {
                return SyncedRoster;
            }
        }

        private sealed class ValidUnitCompositionPort : IUnitCompositionPort
        {
            public ModuleStats GetFrameBaseStats(string frameId)
            {
                return new ModuleStats(frameBaseHp: 600f, defense: 5f);
            }

            public ModuleStats GetFirepowerStats(string moduleId)
            {
                return new ModuleStats(attackDamage: 30f, attackSpeed: 1f, range: 4f);
            }

            public ModuleStats GetMobilityStats(string moduleId)
            {
                return new ModuleStats(moveSpeed: 3f, moveRange: 3f);
            }

            public CostCalculator.StatCostTuning GetCostTuning()
            {
                return CostCalculator.StatCostTuning.Default;
            }

            public string GetPassiveTraitId(string frameId)
            {
                return string.Empty;
            }

            public int GetPassiveTraitCostBonus(string frameId)
            {
                return 0;
            }
        }
    }
}
