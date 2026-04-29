using System.Collections.Generic;
using Features.Garage.Application;
using Features.Garage.Domain;
using Features.Player.Domain;
using Features.Unit.Application;
using Shared.EventBus;
using UnityEngine;

namespace Features.Garage.Presentation
{
    public sealed class GarageSetBUitkPageController : MonoBehaviour
    {
        [SerializeField] private GarageSetBUitkRuntimeAdapter _adapter;

        [Header("Smoke State")]
        [SerializeField] private string _lastRenderStatus;
        [SerializeField] private string _lastInteractionStatus;
        [SerializeField] private int _selectedSlotIndex;
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

        public bool IsInitialized => _state != null && _presenter != null;

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

        public void SelectSlotForMcpSmoke(int slotIndex)
        {
            SelectSlot(slotIndex);
            _lastInteractionStatus = $"slot:{slotIndex}";
        }

        public void SelectFocusForMcpSmoke(string focus)
        {
            if (System.Enum.TryParse(focus, ignoreCase: true, out GarageEditorFocus parsed))
            {
                SetFocusedPart(parsed);
                _lastInteractionStatus = $"focus:{parsed}";
                return;
            }

            _lastInteractionStatus = $"focus-invalid:{focus}";
        }

        public void ToggleSettingsForMcpSmoke()
        {
            _isSettingsOpen = !_isSettingsOpen;
            Render();
            _lastInteractionStatus = $"settings:{_isSettingsOpen}";
        }

        public void RequestSaveForMcpSmoke()
        {
            _ = RunSaveAsync();
            _lastInteractionStatus = "save-requested";
        }

        public void SetPartSearchForMcpSmoke(string value)
        {
            SetPartSearchText(value);
            _lastInteractionStatus = $"part-search:{value}";
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
            _adapter.SaveRequested += () => _ = RunSaveAsync();
            _adapter.SettingsRequested += ToggleSettingsForMcpSmoke;
        }

        private void SelectSlot(int slotIndex)
        {
            if (!CanRender())
                return;

            _state.SelectSlot(slotIndex);
            _focusedPart = GarageEditorFocus.Mobility;
            _partSearchText = string.Empty;
            Render();
        }

        private void SetFocusedPart(GarageEditorFocus focus)
        {
            if (_focusedPart != focus)
                _partSearchText = string.Empty;

            _focusedPart = focus;
            Render();
        }

        private void SetPartSearchText(string value)
        {
            string next = value ?? string.Empty;
            if (_partSearchText == next)
                return;

            _partSearchText = next;
            Render();
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
            _lastInteractionStatus = $"part:{selection.Slot}:{selection.PartId}";
            Render();
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
                    _lastInteractionStatus = "save:saved";
                    break;
                case GarageSaveFlowResultKind.Blocked:
                case GarageSaveFlowResultKind.Failed:
                    _state.SetValidationOverride(result.Message);
                    _lastInteractionStatus = $"save:{result.Kind}";
                    break;
            }

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

            _selectedSlotIndex = _state.SelectedSlotIndex;
            _lastRenderStatus = BuildRenderStatus(slotViewModels);
            PublishDraftState();
        }

        public void SelectVisiblePartForMcpSmoke(string slot, int visibleIndex)
        {
            if (!CanRender())
                return;

            if (!System.Enum.TryParse(slot, ignoreCase: true, out GarageNovaPartPanelSlot parsedSlot))
            {
                _lastInteractionStatus = $"part-slot-invalid:{slot}";
                return;
            }

            _focusedPart = GarageNovaPartsPanelViewModelFactory.ToEditorFocus(parsedSlot);
            var viewModel = BuildPartListViewModel(parsedSlot);
            if (viewModel.Options == null || viewModel.Options.Count == 0)
            {
                _lastInteractionStatus = $"part-empty:{parsedSlot}";
                Render();
                return;
            }

            int index = Mathf.Clamp(visibleIndex, 0, viewModel.Options.Count - 1);
            SelectPartOption(new GarageNovaPartSelection(parsedSlot, viewModel.Options[index].Id));
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
