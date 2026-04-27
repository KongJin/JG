using Features.Account.Application.Ports;
using Features.Garage.Application;
using Features.Garage.Application.Ports;
using Features.Garage.Infrastructure;
using Features.Garage.Presentation;
using Features.Player.Application;
using Features.Player.Application.Ports;
using Features.Player.Infrastructure;
using Features.Unit.Application;
using Features.Unit.Infrastructure;
using Shared.Attributes;
using Shared.EventBus;
using Shared.Lifecycle;
using UnityEngine;
using UnityEngine.Serialization;

namespace Features.Garage
{
    /// <summary>
    /// Garage Feature의 Scene-level wiring + Composition Root.
    /// EventBus 주입, Infrastructure 어댑터 생성, UseCase 조립.
    /// 비즈니스 로직 없음 — 조립만 담당.
    /// </summary>
    public sealed class GarageSetup : MonoBehaviour
    {
        [Required, SerializeField]
        private GarageNetworkAdapter _networkAdapter;

        [FormerlySerializedAs("_panelView")]
        [SerializeField]
        private GaragePageController _pageController;

        [SerializeField]
        private NovaPartVisualCatalog _novaPartVisualCatalog;

        [SerializeField]
        private NovaPartAlignmentCatalog _novaPartAlignmentCatalog;

        private IAccountDataPort _accountDataPort;
        private RosterValidationProvider _rosterValidationProvider;
        private DisposableScope _disposables;
        private IEventPublisher _eventPublisher;
        private GaragePanelCatalog _panelCatalog;
        private readonly GaragePanelCatalogFactory _panelCatalogFactory = new();

        // Application UseCases
        public InitializeGarageUseCase InitializeGarage { get; private set; }
        public ComposeUnitUseCase ComposeUnit { get; private set; }
        public ValidateRosterUseCase ValidateRoster { get; private set; }
        public SaveRosterUseCase SaveRoster { get; private set; }
        public IGarageNetworkPort NetworkPort => _networkAdapter;
        public IAccountDataPort AccountDataPort => _accountDataPort;

        /// <summary>
        /// Garage Feature 초기화.
        /// 씬 Setup(예: LobbySetup)에서 EventBus와 Unit 조합 포트를 주입하고 호출한다.
        /// </summary>
        public void Initialize(
            EventBus eventBus,
            IUnitCompositionPort compositionPort,
            ModuleCatalog unitCatalog,
            IAccountDataPort accountDataPort = null)
        {
            _accountDataPort = accountDataPort;
            _eventPublisher = eventBus;
            _rosterValidationProvider = new RosterValidationProvider(unitCatalog);
            _panelCatalog = _panelCatalogFactory.Build(
                unitCatalog,
                _novaPartVisualCatalog,
                _novaPartAlignmentCatalog);

            // Composition root — UseCase 조립
            _disposables?.Dispose();
            _disposables = new DisposableScope();
            var localPersistence = new GarageJsonPersistence();
            var operationRecordStore = new OperationRecordJsonStore();
            var recentOperations = operationRecordStore.Load();

            ComposeUnit = new ComposeUnitUseCase(compositionPort);
            InitializeGarage = new InitializeGarageUseCase(
                localPersistence,
                _networkAdapter,
                accountDataPort as InitializeGarageUseCase.ICloudGarageLoadPort,
                eventBus);
            ValidateRoster = new ValidateRosterUseCase(_rosterValidationProvider);

            if (accountDataPort != null)
            {
                SaveRoster = new SaveRosterUseCase(
                    accountDataPort as SaveRosterUseCase.ICloudGaragePort,
                    localPersistence,
                    _networkAdapter,
                    eventBus);
            }
            else
            {
                // 폴백: AccountSetup 미연결 시 로컬 저장만 (GameScene 등)
                SaveRoster = new SaveRosterUseCase(
                    null,
                    localPersistence,
                    _networkAdapter,
                    eventBus);
            }

            if (_pageController != null)
            {
                _pageController.Initialize(
                    InitializeGarage,
                    ComposeUnit,
                    ValidateRoster,
                    SaveRoster,
                    _eventPublisher,
                    _panelCatalog,
                    recentOperations);
            }

            _ = SyncOperationRecordsAsync(
                operationRecordStore,
                accountDataPort as IOperationRecordCloudPort);
        }

        private async System.Threading.Tasks.Task SyncOperationRecordsAsync(
            OperationRecordJsonStore operationRecordStore,
            IOperationRecordCloudPort cloudPort)
        {
            if (operationRecordStore == null || cloudPort == null)
                return;

            var syncOperationRecords = new SyncOperationRecordsUseCase(
                operationRecordStore,
                cloudPort,
                Debug.LogWarning);
            var result = await syncOperationRecords.Execute();
            if (result.IsFailure)
                return;

            if (_pageController == null || _panelCatalog == null)
                return;

            _pageController.Initialize(
                InitializeGarage,
                ComposeUnit,
                ValidateRoster,
                SaveRoster,
                _eventPublisher,
                _panelCatalog,
                result.Value);
        }

        /// <summary>
        /// 씬 전환 시 정리.
        /// </summary>
        public void Cleanup()
        {
            _disposables?.Dispose();
            _disposables = null;
        }

        private void OnDestroy()
        {
            _disposables?.Dispose();
        }
    }

    internal sealed class GaragePanelCatalogFactory
    {
        public GaragePanelCatalog Build(
            ModuleCatalog unitCatalog,
            NovaPartVisualCatalog novaPartVisualCatalog = null,
            NovaPartAlignmentCatalog novaPartAlignmentCatalog = null)
        {
            var novaMetadata = BuildNovaMetadataByPartId(novaPartVisualCatalog);
            var novaAlignment = BuildNovaAlignmentByPartId(novaPartAlignmentCatalog);
            var frames = new System.Collections.Generic.List<GaragePanelCatalog.FrameOption>();
            for (int i = 0; i < unitCatalog.UnitFrames.Count; i++)
            {
                var frame = unitCatalog.UnitFrames[i];
                novaMetadata.TryGetValue(frame.FrameId, out var metadata);
                novaAlignment.TryGetValue(frame.FrameId, out var alignment);
                frames.Add(new GaragePanelCatalog.FrameOption
                {
                    Id = frame.FrameId,
                    DisplayName = frame.DisplayName,
                    BaseHp = frame.BaseHp,
                    BaseAttackSpeed = frame.BaseAttackSpeed,
                    PreviewPrefab = frame.PreviewPrefab,
                    SourcePath = metadata?.SourceRelativePath,
                    Tier = metadata?.Tier ?? 0,
                    NeedsNameReview = metadata?.NeedsNameReview ?? false,
                    Alignment = CreateAlignment(alignment)
                });
            }

            var firepower = new System.Collections.Generic.List<GaragePanelCatalog.FirepowerOption>();
            for (int i = 0; i < unitCatalog.FirepowerModules.Count; i++)
            {
                var module = unitCatalog.FirepowerModules[i];
                novaMetadata.TryGetValue(module.ModuleId, out var metadata);
                novaAlignment.TryGetValue(module.ModuleId, out var alignment);
                firepower.Add(new GaragePanelCatalog.FirepowerOption
                {
                    Id = module.ModuleId,
                    DisplayName = module.DisplayName,
                    AttackDamage = module.AttackDamage,
                    AttackSpeed = module.AttackSpeed,
                    Range = module.Range,
                    PreviewPrefab = module.PreviewPrefab,
                    SourcePath = metadata?.SourceRelativePath,
                    Tier = metadata?.Tier ?? 0,
                    NeedsNameReview = metadata?.NeedsNameReview ?? false,
                    Alignment = CreateAlignment(alignment)
                });
            }

            var mobility = new System.Collections.Generic.List<GaragePanelCatalog.MobilityOption>();
            for (int i = 0; i < unitCatalog.MobilityModules.Count; i++)
            {
                var module = unitCatalog.MobilityModules[i];
                novaMetadata.TryGetValue(module.ModuleId, out var metadata);
                novaAlignment.TryGetValue(module.ModuleId, out var alignment);
                mobility.Add(new GaragePanelCatalog.MobilityOption
                {
                    Id = module.ModuleId,
                    DisplayName = module.DisplayName,
                    HpBonus = module.HpBonus,
                    MoveRange = module.MoveRange,
                    AnchorRange = module.AnchorRange,
                    PreviewPrefab = module.PreviewPrefab,
                    SourcePath = metadata?.SourceRelativePath,
                    Tier = metadata?.Tier ?? 0,
                    NeedsNameReview = metadata?.NeedsNameReview ?? false,
                    Alignment = CreateAlignment(alignment)
                });
            }

            return new GaragePanelCatalog(frames, firepower, mobility);
        }

        private static System.Collections.Generic.Dictionary<string, NovaPartVisualCatalog.Entry> BuildNovaMetadataByPartId(
            NovaPartVisualCatalog novaPartVisualCatalog)
        {
            var byPartId = new System.Collections.Generic.Dictionary<string, NovaPartVisualCatalog.Entry>();
            if (novaPartVisualCatalog == null)
                return byPartId;

            for (int i = 0; i < novaPartVisualCatalog.Entries.Count; i++)
            {
                var entry = novaPartVisualCatalog.Entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.PartId))
                    continue;

                byPartId[entry.PartId] = entry;
            }

            return byPartId;
        }

        private static System.Collections.Generic.Dictionary<string, NovaPartAlignmentCatalog.Entry> BuildNovaAlignmentByPartId(
            NovaPartAlignmentCatalog novaPartAlignmentCatalog)
        {
            var byPartId = new System.Collections.Generic.Dictionary<string, NovaPartAlignmentCatalog.Entry>();
            if (novaPartAlignmentCatalog == null)
                return byPartId;

            for (int i = 0; i < novaPartAlignmentCatalog.Entries.Count; i++)
            {
                var entry = novaPartAlignmentCatalog.Entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.PartId))
                    continue;

                byPartId[entry.PartId] = entry;
            }

            return byPartId;
        }

        private static GaragePanelCatalog.PartAlignment CreateAlignment(NovaPartAlignmentCatalog.Entry entry)
        {
            if (entry == null)
                return null;

            return new GaragePanelCatalog.PartAlignment
            {
                PivotOffset = entry.PivotOffset,
                SocketOffset = entry.SocketOffset,
                SocketEuler = entry.SocketEuler,
                HasXfiMetadata = entry.HasXfiMetadata,
                XfiPath = entry.XfiPath,
                XfiHeader = entry.XfiHeader,
                XfiHeaderKind = entry.XfiHeaderKind,
                XfiAttachSlot = entry.XfiAttachSlot,
                XfiAttachVariant = entry.XfiAttachVariant,
                XfiTransformCount = entry.XfiTransformCount,
                XfiTransformTranslations = entry.XfiTransformTranslations,
                XfiDirectionRangeCount = entry.XfiDirectionRangeCount,
                XfiDirectionRanges = entry.XfiDirectionRanges,
                HasXfiAttachSocket = entry.HasXfiAttachSocket,
                XfiAttachSocketOffset = entry.XfiAttachSocketOffset,
                HasFrameTopSocket = entry.HasFrameTopSocket,
                FrameTopSocketOffset = entry.FrameTopSocketOffset,
                XfiSocketQuality = entry.XfiSocketQuality,
                XfiSocketName = entry.XfiSocketName,
                QualityFlag = entry.QualityFlag,
                ReviewReason = entry.ReviewReason
            };
        }
    }
}
