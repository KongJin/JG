using System;
using System.Collections.Generic;
using Features.Garage.Application;
using Features.Garage.Domain;
using Features.Player.Domain;
using Features.Unit.Application;
using Features.Unit.Infrastructure;
using Shared.EventBus;
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
        private bool _isInitializingRoster;
        private GarageSetBUitkPageSnapshot _lastSnapshot;

        public bool IsInitialized => _state != null && _presenter != null;
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
            _initializeGarage = initializeGarage;
            _composeUnit = composeUnit;
            _validateRoster = validateRoster;
            _saveRoster = saveRoster;
            _eventPublisher = eventPublisher;
            _catalog = catalog;
            _recentOperations = recentOperations;
            _presenter = new GaragePagePresenter(_catalog);
            _state ??= new GaragePageState();
            _focusedPart = GarageEditorFocus.Mobility;
            _partSearchText = string.Empty;

            HookCallbacks();
            _state.Initialize(new GarageRoster());
            Render();
            _ = InitializeRosterAsync();
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
            if (_isInitializingRoster || _initializeGarage == null)
                return;

            _isInitializingRoster = true;
            try
            {
                var roster = await _initializeGarage.Execute();
                _state.Initialize(roster ?? new GarageRoster());
                Render();
            }
            finally
            {
                _isInitializingRoster = false;
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
                    ClearIncompatibleFirepower();
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

        private void ClearIncompatibleFirepower()
        {
            var frame = _catalog.FindFrame(_state.EditingFrameId);
            var firepower = _catalog.FindFirepower(_state.EditingFirepowerId);
            if (frame == null ||
                firepower == null ||
                UnitPartCompatibility.AreAssemblyFormsCompatible(frame.AssemblyForm, firepower.AssemblyForm))
            {
                return;
            }

            _state.SetEditingFirepowerId(null);
        }

        private async System.Threading.Tasks.Task RunSaveAsync()
        {
            if (!CanRender())
                return;

            var result = await _saveFlow.SaveAsync(
                _state.DraftRoster,
                EvaluateDraft(),
                _saveRoster,
                _ => Render(),
                Render);

            switch (result.Kind)
            {
                case GarageSaveFlowResultKind.Saved:
                    _state.CommitDraft();
                    break;
                case GarageSaveFlowResultKind.Blocked:
                case GarageSaveFlowResultKind.Failed:
                    _state.SetValidationOverride(result.Message);
                    break;
            }

            SaveCompleted?.Invoke(result.Kind);
            Render();
        }

        private void Render()
        {
            if (!CanRender())
                return;

            var evaluation = EvaluateDraft();
            var operationSummary = GarageOperationRecordSummaryFormatter.BuildSummary(_recentOperations);
            var serviceTags = GarageOperationRecordServiceTagMapper.BuildByLoadoutKey(_recentOperations);
            IReadOnlyList<GarageSlotViewModel> slotViewModels = _presenter.BuildSlotViewModels(_state, serviceTags);
            var partListViewModel = BuildPartListViewModel();
            var editorViewModel = _presenter.BuildEditorViewModel(_state);
            var resultViewModel = _presenter.BuildResultViewModel(_state, evaluation, operationSummary);

            _adapter.Render(
                slotViewModels,
                partListViewModel,
                editorViewModel,
                resultViewModel,
                _focusedPart,
                _saveFlow.IsSaving);

            _lastSnapshot = new GarageSetBUitkPageSnapshot(
                BuildRenderStatus(slotViewModels),
                _state.SelectedSlotIndex,
                _focusedPart,
                _partSearchText,
                _isSettingsOpen);
            Rendered?.Invoke(_lastSnapshot);
            PublishDraftState();
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
            return GarageNovaPartsPanelViewModelFactory.Build(
                _catalog,
                new GarageNovaPartsDraftSelection(
                    _state.EditingFrameId,
                    _state.EditingFirepowerId,
                    _state.EditingMobilityId),
                slot,
                _partSearchText);
        }

        private bool CanRender()
        {
            return _adapter != null &&
                   _state != null &&
                   _presenter != null &&
                   _catalog != null &&
                   _composeUnit != null &&
                   _validateRoster != null &&
                   _saveRoster != null &&
                   _eventPublisher != null;
        }

        private void PublishDraftState()
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
    }
}
