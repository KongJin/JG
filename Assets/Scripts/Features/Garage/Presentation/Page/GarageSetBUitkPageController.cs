using System;
using System.Collections.Generic;
using System.Threading;
using Features.Garage.Application;
using Features.Garage.Domain;
using Features.Player.Domain;
using Features.Unit.Application;
using Shared.EventBus;
using Shared.Runtime;
using UnityEngine;

namespace Features.Garage.Presentation
{
    public sealed class GarageSetBUitkPageController : MonoBehaviour
    {
        [SerializeField] private GarageSetBUitkRuntimeAdapter _adapter;

        [SerializeField] private GarageEditorFocus _focusedPart = GarageEditorFocus.Mobility;
        [SerializeField] private string _partSearchText = string.Empty;
        [SerializeField] private bool _isSettingsOpen;

        private InitializeGarageUseCase _initializeGarage;
        private ComposeUnitUseCase _composeUnit;
        private ValidateRosterUseCase _validateRoster;
        private SaveRosterUseCase _saveRoster;
        private IEventPublisher _eventPublisher;
        private GaragePanelCatalog _catalog;
        private RecentOperationRecords _recentOperations;
        private GaragePageState _state;
        private GaragePagePresenter _presenter;
        private readonly PublishGarageDraftStateUseCase _draftStatePublisher = new();
        private readonly GarageSaveFlow _saveFlow = new();
        private bool _callbacksHooked;
        private GarageSetBUitkPageSnapshot _lastSnapshot;

        // 초기화 상태 가드 (CanRender 대체)
        private readonly GarageInitializationGuard _initGuard = new();

        // 비동기 작업 추적 (로딩 상태 표시 및 취소 지원)
        private AsyncOperationHandle _initializeOperation;
        private AsyncOperationHandle _saveOperation;

        /// <summary>
        /// 로딩 중인지 여부
        /// </summary>
        public bool IsLoading => _initializeOperation?.IsInProgress ?? false;

        /// <summary>
        /// 저장 중인지 여부
        /// </summary>
        public bool IsSaving => (_saveOperation?.IsInProgress ?? false) || _saveFlow.IsSaving;

        /// <summary>
        /// 현재 진행 중인 작업 이름
        /// </summary>
        public string CurrentOperationName
        {
            get
            {
                if (_initializeOperation?.IsInProgress == true)
                    return _initializeOperation.OperationName;
                if (_saveOperation?.IsInProgress == true)
                    return _saveOperation.OperationName;
                return string.Empty;
            }
        }

        public bool IsInitialized => _initGuard.IsReady;
        public GarageSetBUitkPageSnapshot CurrentSnapshot => _lastSnapshot;

        public event Action<GarageSetBUitkPageSnapshot> Rendered;
        internal event Action<GarageSaveFlowResultKind> SaveCompleted;

        public void Initialize(
            InitializeGarageUseCase initializeGarage,
            ComposeUnitUseCase composeUnit,
            ValidateRosterUseCase validateRoster,
            SaveRosterUseCase saveRoster,
            IEventPublisher eventPublisher,
            GaragePanelCatalog catalog,
            RecentOperationRecords recentOperations = null)
        {
            if (_adapter == null)
                _adapter = ComponentAccess.Get<GarageSetBUitkRuntimeAdapter>(gameObject);

            _initializeGarage = initializeGarage;
            _composeUnit = composeUnit;
            _validateRoster = validateRoster;
            _saveRoster = saveRoster;
            _eventPublisher = eventPublisher;
            _catalog = catalog;
            _recentOperations = recentOperations;
            _presenter = new GaragePagePresenter(_catalog);
            _state ??= new GaragePageState();
            _initGuard.Reset();
            if (!CanRender())
                throw new InvalidOperationException($"Garage page dependency is missing: {_initGuard.MissingDependency}");

            _focusedPart = GarageEditorFocus.Mobility;
            _partSearchText = string.Empty;

            HookCallbacks();
            _state.Initialize(new GarageRoster());
            Render();
            _ = InitializeRosterAsync();
        }

        /// <summary>
        /// 진행 중인 작업 취소
        /// </summary>
        public void CancelCurrentOperation()
        {
            _initializeOperation?.Cancel();
            _saveOperation?.Cancel();
        }

        public void SelectSlot(int slotIndex)
        {
            if (!CanRender())
                return;

            _state.SelectSlot(slotIndex);
            _focusedPart = GarageEditorFocus.Mobility;
            _partSearchText = string.Empty;
            Render();
        }

        public void SetFocusedPart(GarageEditorFocus focus)
        {
            if (_focusedPart != focus)
                _partSearchText = string.Empty;

            _focusedPart = focus;
            Render();
        }

        public void SetPartSearchText(string value)
        {
            string next = value ?? string.Empty;
            if (_partSearchText == next)
                return;

            _partSearchText = next;
            Render();
        }

        public void ToggleSettings()
        {
            _isSettingsOpen = !_isSettingsOpen;
            Render();
        }

        public void RequestSave()
        {
            _ = RunSaveAsync();
        }

        public bool TrySelectVisiblePart(
            GarageNovaPartPanelSlot slot,
            int visibleIndex,
            out GarageNovaPartSelection selection,
            out bool hasOptions)
        {
            selection = default;
            hasOptions = false;
            if (!CanRender())
                return false;

            _focusedPart = GarageNovaPartsPanelViewModelFactory.ToEditorFocus(slot);
            var viewModel = BuildPartListViewModel(slot);
            if (viewModel.Options == null || viewModel.Options.Count == 0)
            {
                Render();
                return false;
            }

            hasOptions = true;
            int index = Mathf.Clamp(visibleIndex, 0, viewModel.Options.Count - 1);
            selection = new GarageNovaPartSelection(slot, viewModel.Options[index].Id);
            SelectPartOption(selection);
            return true;
        }

        private async System.Threading.Tasks.Task InitializeRosterAsync()
        {
            if (_initializeOperation?.IsInProgress == true || _initializeGarage == null)
                return;

            _initializeOperation?.Dispose();
            _initializeOperation = new AsyncOperationHandle("로스터 초기화 중...");
            Render();

            try
            {
                var roster = await _initializeGarage.Execute();
                if (!_initializeOperation.IsCancellationRequested)
                {
                    _state.Initialize(roster ?? new GarageRoster());
                    Render();
                }
            }
            catch (OperationCanceledException)
            {
                // 취소는 정상 동작
            }
            finally
            {
                _initializeOperation?.Complete();
                _initializeOperation?.Dispose();
                _initializeOperation = null;
                Render();
            }
        }

        private void HookCallbacks()
        {
            if (_callbacksHooked || _adapter == null)
                return;

            _callbacksHooked = true;
            _adapter.Bind();
            _adapter.SlotSelected += SelectSlot;
            _adapter.PartFocusSelected += SetFocusedPart;
            _adapter.PartSearchChanged += SetPartSearchText;
            _adapter.PartOptionSelected += SelectPartOption;
            _adapter.SaveRequested += RequestSave;
            _adapter.SettingsRequested += ToggleSettings;
        }

        private void SelectPartOption(GarageNovaPartSelection selection)
        {
            if (!CanRender())
                return;

            _focusedPart = GarageNovaPartsPanelViewModelFactory.ToEditorFocus(selection.Slot);
            switch (selection.Slot)
            {
                case GarageNovaPartPanelSlot.Frame:
                    _state.SetEditingFrameId(selection.PartId);
                    break;
                case GarageNovaPartPanelSlot.Firepower:
                    _state.SetEditingFirepowerId(selection.PartId);
                    break;
                case GarageNovaPartPanelSlot.Mobility:
                    _state.SetEditingMobilityId(selection.PartId);
                    break;
            }

            _state.ClearValidationOverride();
            Render();
        }

        private async System.Threading.Tasks.Task RunSaveAsync()
        {
            if (!CanRender())
                return;

            // 이미 저장 중이면 무시
            if (_saveOperation?.IsInProgress == true)
                return;

            _saveOperation?.Dispose();
            _saveOperation = new AsyncOperationHandle("저장 중...");
            Render(); // 로딩 상태 표시

            try
            {
                var result = await _saveFlow.SaveAsync(
                    _state.BuildSelectedSlotCommitRoster(),
                    EvaluateDraft(),
                    _saveRoster,
                    _ => Render(),
                    Render);

                if (!_saveOperation.IsCancellationRequested)
                {
                    switch (result.Kind)
                    {
                        case GarageSaveFlowResultKind.Saved:
                            _state.CommitSelectedSlotDraft();
                            break;
                        case GarageSaveFlowResultKind.Blocked:
                        case GarageSaveFlowResultKind.Failed:
                            _state.SetValidationOverride(result.Message);
                            break;
                    }

                    SaveCompleted?.Invoke(result.Kind);
                    Render();
                }
            }
            catch (OperationCanceledException)
            {
                // 취소는 정상 동작
            }
            finally
            {
                _saveOperation?.Complete();
                _saveOperation?.Dispose();
                _saveOperation = null;
                Render(); // 로딩 상태 제거
            }
        }

        private void Render()
        {
            if (!CanRender())
                return;

            var context = BuildRenderContext();
            Render(context);
        }

        /// <summary>
        /// RenderContext를 받아 렌더링 수행 - 책임 분리
        /// </summary>
        private void Render(GarageRenderContext context)
        {
            if (context == null)
                return;

            _adapter.Render(
                context.SlotViewModels,
                context.PartListViewModel,
                context.EditorViewModel,
                context.ResultViewModel,
                context.FocusedPart,
                context.Snapshot.IsSaving);

            _lastSnapshot = context.Snapshot;
            Rendered?.Invoke(_lastSnapshot);
            PublishDraftState(context.Evaluation);
        }

        /// <summary>
        /// RenderContext 빌드 - 데이터 준비 책임 분리
        /// </summary>
        private GarageRenderContext BuildRenderContext()
        {
            var evaluation = EvaluateDraft();
            var operationSummary = GarageOperationRecordSummaryFormatter.BuildSummary(_recentOperations);
            var serviceTags = GarageOperationRecordServiceTagMapper.BuildByLoadoutKey(_recentOperations);
            IReadOnlyList<GarageSlotViewModel> slotViewModels = _presenter.BuildSlotViewModels(_state, serviceTags);
            var partListViewModel = BuildPartListViewModel();
            var editorViewModel = _presenter.BuildEditorViewModel(_state);
            var resultViewModel = _presenter.BuildResultViewModel(_state, evaluation, operationSummary);
            if (IsLoading)
            {
                resultViewModel = new GarageResultViewModel(
                    resultViewModel.RosterStatusText,
                    CurrentOperationName,
                    resultViewModel.StatsText,
                    resultViewModel.IsReady,
                    resultViewModel.IsDirty,
                    canSave: false,
                    primaryActionLabel: "초기화 중...",
                    resultViewModel.Radar);
            }

            var snapshot = new GarageSetBUitkPageSnapshot(
                BuildRenderStatus(slotViewModels),
                _state.SelectedSlotIndex,
                _focusedPart,
                _partSearchText,
                _isSettingsOpen,
                evaluation.HasDraftChanges,
                resultViewModel.CanSave,
                resultViewModel.ValidationText,
                IsLoading,
                IsSaving,
                CurrentOperationName);

            return new GarageRenderContext(
                slotViewModels,
                partListViewModel,
                editorViewModel,
                resultViewModel,
                snapshot,
                evaluation);
        }

        private GarageDraftEvaluation EvaluateDraft()
        {
            return GarageDraftEvaluator.Evaluate(_state, _catalog, _composeUnit, _validateRoster);
        }

        private GarageNovaPartsPanelViewModel BuildPartListViewModel()
        {
            return BuildPartListViewModel(GarageNovaPartsPanelViewModelFactory.ToPanelSlot(_focusedPart));
        }

        private GarageNovaPartsPanelViewModel BuildPartListViewModel(GarageNovaPartPanelSlot slot)
        {
            var viewModel = GarageNovaPartsPanelViewModelFactory.Build(
                _catalog,
                new GarageNovaPartsDraftSelection(
                    _state.EditingFrameId,
                    _state.EditingFirepowerId,
                    _state.EditingMobilityId),
                slot,
                _partSearchText);
            return viewModel;
        }

        private bool CanRender()
        {
            // 초기화 가드로 중앙화 - 한 번만 검증
            if (_initGuard.IsReady)
                return true;

            return _initGuard.Validate(
                _adapter,
                _state,
                _presenter,
                _catalog,
                _composeUnit,
                _validateRoster,
                _saveRoster,
                _eventPublisher);
        }

        private void PublishDraftState()
        {
            var evaluation = EvaluateDraft();
            PublishDraftState(evaluation);
        }

        private void PublishDraftState(GarageDraftEvaluation evaluation)
        {
            var draftState = _draftStatePublisher.Build(_state.CommittedRoster, _state.HasDraftChanges());
            _eventPublisher.Publish(new GarageDraftStateChangedEvent(
                _state.CommittedRoster.Count,
                draftState.HasUnsavedChanges,
                draftState.ReadyEligible,
                draftState.BlockReason));
        }

        private static string BuildRenderStatus(IReadOnlyList<GarageSlotViewModel> slots)
        {
            if (slots == null || slots.Count == 0)
                return "rendered:empty";

            var selected = slots[0];
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].IsSelected)
                {
                    selected = slots[i];
                    break;
                }
            }

            return selected.IsEmpty
                ? "rendered:selected-empty"
                : $"rendered:{selected.FrameId}/{selected.FirepowerId}/{selected.MobilityId}";
        }

        private void OnDestroy()
        {
            CancelCurrentOperation();
            UnhookCallbacks();
            _initializeOperation?.Dispose();
            _saveOperation?.Dispose();
        }

        private void UnhookCallbacks()
        {
            if (!_callbacksHooked || _adapter == null)
                return;

            _adapter.SlotSelected -= SelectSlot;
            _adapter.PartFocusSelected -= SetFocusedPart;
            _adapter.PartSearchChanged -= SetPartSearchText;
            _adapter.PartOptionSelected -= SelectPartOption;
            _adapter.SaveRequested -= RequestSave;
            _adapter.SettingsRequested -= ToggleSettings;
            _callbacksHooked = false;
        }
    }
}
