using Features.Garage.Application;
using Features.Garage.Domain;
using Features.Player.Domain;
using Features.Unit.Application;
using Shared.Attributes;
using Shared.EventBus;
using UnityEngine;
using UnityEngine.UI;

namespace Features.Garage.Presentation
{
    public sealed class GaragePageController : MonoBehaviour
    {
        private enum MobilePartFocus
        {
            Frame,
            Firepower,
            Mobility,
        }

        [Header("Subviews")]
        [Required, SerializeField] private GarageRosterListView _rosterListView;
        [Required, SerializeField] private GarageUnitEditorView _unitEditorView;
        [Required, SerializeField] private GarageResultPanelView _resultPanelView;
        [SerializeField] private GarageNovaPartsPanelView _novaPartsPanelView;
        [SerializeField] private GarageSetBUitkRuntimeAdapter _setBUitkAdapter;

        [Header("Preview")]
        [Required, SerializeField] private GarageUnitPreviewView _unitPreviewView;

        [Header("Chrome")]
        [Required, SerializeField] private GaragePageChromeBindings _chromeBindings;

        private InitializeGarageUseCase _initializeGarage;
        private ComposeUnitUseCase _composeUnit;
        private ValidateRosterUseCase _validateRoster;
        private SaveRosterUseCase _saveRoster;
        private IEventPublisher _eventPublisher;
        private GaragePanelCatalog _catalog;
        private RecentOperationRecords _recentOperations;
        private GaragePageState _state;
        private GaragePagePresenter _presenter;
        private bool _callbacksHooked;
        private bool _isInitialized;
        private bool _isInitializingRoster;
        private bool _isSettingsOverlayOpen;
        private MobilePartFocus _mobilePartFocus = MobilePartFocus.Frame;
        private readonly PublishGarageDraftStateUseCase _draftStatePublisher = new();
        private readonly GaragePageScrollController _scrollController = new();
        private readonly GarageSaveFlow _saveFlow = new();
        private GaragePageChromeController _chromeController;
        private GarageNovaPartsPanelCoordinator _novaPartsPanel;

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
            _chromeController ??= GaragePageChromeBindingResolver.CreateController(this, ref _chromeBindings);

            _unitPreviewView.Initialize();
            HookCallbacks();
            SyncChrome(null);

            if (_isInitialized)
            {
                Render();
                return;
            }

            _isInitialized = true;
            _state.Initialize(new GarageRoster());
            Render();
            _ = InitializeRosterAsync();
        }

        private async System.Threading.Tasks.Task InitializeRosterAsync()
        {
            if (_isInitializingRoster)
                return;

            _isInitializingRoster = true;

            try
            {
                EnsureInitialized();
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
            if (_callbacksHooked)
                return;

            _callbacksHooked = true;

            _rosterListView.Bind();
            _unitEditorView.Bind();
            if (_novaPartsPanelView != null)
            {
                _novaPartsPanel ??= new GarageNovaPartsPanelCoordinator(_novaPartsPanelView);
                _novaPartsPanel.Bind();
                _novaPartsPanel.ApplyRequested += selection =>
                {
                    EnsureInitialized();
                    switch (selection.Slot)
                    {
                        case GarageNovaPartPanelSlot.Frame:
                            _mobilePartFocus = MobilePartFocus.Frame;
                            _state.SetEditingFrameId(selection.PartId);
                            break;
                        case GarageNovaPartPanelSlot.Firepower:
                            _mobilePartFocus = MobilePartFocus.Firepower;
                            _state.SetEditingFirepowerId(selection.PartId);
                            break;
                        case GarageNovaPartPanelSlot.Mobility:
                            _mobilePartFocus = MobilePartFocus.Mobility;
                            _state.SetEditingMobilityId(selection.PartId);
                            break;
                    }

                    _state.ClearValidationOverride();
                    Render();
                };
            }

            _rosterListView.SlotSelected += SelectSlot;
            _unitEditorView.FrameCycleRequested += CycleFrame;
            _unitEditorView.FirepowerCycleRequested += CycleFirepower;
            _unitEditorView.MobilityCycleRequested += CycleMobility;
            _unitEditorView.ClearRequested += ClearSelectedSlot;
            _resultPanelView.SaveClicked += RequestSave;
            if (_setBUitkAdapter != null)
            {
                _setBUitkAdapter.Bind();
                _setBUitkAdapter.SlotSelected += SelectSlot;
                _setBUitkAdapter.PartFocusSelected += focus => SetMobilePartFocus(ToMobilePartFocus(focus));
                _setBUitkAdapter.SaveRequested += RequestSave;
                _setBUitkAdapter.SettingsRequested += () => SetSettingsOverlayOpen(!_isSettingsOverlayOpen);
            }

            var chromeBindings = GaragePageChromeBindingResolver.Resolve(this, ref _chromeBindings);
            if (chromeBindings == null)
                return;

            void AddButtonListener(Button button, UnityEngine.Events.UnityAction action)
            {
                if (button == null || action == null)
                    return;

                button.onClick.AddListener(action);
            }

            AddButtonListener(chromeBindings.MobileEditTabButton, () => SetMobilePartFocus(MobilePartFocus.Frame));
            AddButtonListener(chromeBindings.MobileFirepowerTabButton, () => SetMobilePartFocus(MobilePartFocus.Firepower));
            AddButtonListener(chromeBindings.MobileSummaryTabButton, () => SetMobilePartFocus(MobilePartFocus.Mobility));

            AddButtonListener(chromeBindings.SettingsOpenButton, () => SetSettingsOverlayOpen(!_isSettingsOverlayOpen));
            AddButtonListener(chromeBindings.SettingsCloseButton, () => SetSettingsOverlayOpen(false));
            AddButtonListener(chromeBindings.MobileSaveButton, RequestSave);
        }

        private void OnDisable()
        {
            _isSettingsOverlayOpen = false;
            _chromeController?.HideSettingsOverlay();
        }

        private void OnEnable()
        {
            _chromeController ??= GaragePageChromeBindingResolver.CreateController(this, ref _chromeBindings);
            if (_isInitialized && _state != null && _presenter != null)
            {
                Render();
                return;
            }

            SyncChrome(null);
        }

        private void Update()
        {
            if (!GaragePageKeyboardShortcuts.TryGetCurrentAction(out var action))
                return;

            HandleKeyboardAction(action);
        }

        private void SelectSlot(int slotIndex)
        {
            _state.SelectSlot(slotIndex);
            _mobilePartFocus = MobilePartFocus.Frame;
            Render();
            _scrollController.ScrollBodyToTop(_chromeBindings != null ? _chromeBindings.MobileBodyHost : null);
        }

        private void CycleFrame(int delta)
        {
            CyclePart(MobilePartFocus.Frame, _state.EditingFrameId, _catalog.Frames, delta, frame => frame.Id, _state.SetEditingFrameId);
        }

        private void CycleFirepower(int delta)
        {
            CyclePart(MobilePartFocus.Firepower, _state.EditingFirepowerId, _catalog.Firepower, delta, module => module.Id, _state.SetEditingFirepowerId);
        }

        private void CycleMobility(int delta)
        {
            CyclePart(MobilePartFocus.Mobility, _state.EditingMobilityId, _catalog.Mobility, delta, module => module.Id, _state.SetEditingMobilityId);
        }

        private void CyclePart<T>(
            MobilePartFocus focus,
            string currentId,
            System.Collections.Generic.IReadOnlyList<T> items,
            int delta,
            System.Func<T, string> getId,
            System.Action<string> setId)
        {
            EnsureInitialized();
            _mobilePartFocus = focus;
            setId(CycleId(currentId, items, delta, getId));
            _state.ClearValidationOverride();
            Render();
        }

        private void ClearSelectedSlot()
        {
            _state.ClearSelectedSlotDraft();
            _state.ClearValidationOverride();
            Render();
        }

        private void RequestSave()
        {
            _ = RunSaveAsync();
        }

        private void SetSettingsOverlayOpen(bool isOpen)
        {
            if (_isSettingsOverlayOpen == isOpen)
                return;

            _isSettingsOverlayOpen = isOpen;
            SyncChrome(null);
        }

        private async System.Threading.Tasks.Task RunSaveAsync()
        {
            EnsureInitialized();
            var result = await _saveFlow.SaveAsync(
                _state.DraftRoster,
                EvaluateDraft(),
                _saveRoster,
                BeginSave,
                EndSave);

            ApplySaveResult(result);
        }

        private void Render()
        {
            EnsureInitialized();

            var evaluation = EvaluateDraft();
            var operationSummary = GarageOperationRecordSummaryFormatter.BuildSummary(_recentOperations);
            var serviceTagsByLoadoutKey = GarageOperationRecordServiceTagMapper.BuildByLoadoutKey(_recentOperations);
            var slotViewModels = _presenter.BuildSlotViewModels(_state, serviceTagsByLoadoutKey);
            var editorViewModel = _presenter.BuildEditorViewModel(_state);
            var resultViewModel = _presenter.BuildResultViewModel(_state, evaluation, operationSummary);

            _rosterListView.Render(slotViewModels);
            _unitEditorView.Render(editorViewModel);
            _unitEditorView.SetFocusedPart(ToEditorFocus(_mobilePartFocus));
            _novaPartsPanel?.Render(
                _catalog,
                new GarageNovaPartsDraftSelection(
                    _state.EditingFrameId,
                    _state.EditingFirepowerId,
                    _state.EditingMobilityId),
                ToEditorFocus(_mobilePartFocus));
            _resultPanelView.Render(resultViewModel);
            _resultPanelView.SetInlineSaveVisible(false);
            _unitPreviewView.Render(slotViewModels[_state.SelectedSlotIndex]);
            _setBUitkAdapter?.Render(
                slotViewModels,
                editorViewModel,
                resultViewModel,
                ToEditorFocus(_mobilePartFocus),
                _saveFlow.IsSaving);

            SyncChrome(resultViewModel);
            if (_chromeBindings != null &&
                _chromeBindings.MobileSaveStateText != null &&
                !string.IsNullOrWhiteSpace(operationSummary))
            {
                _chromeBindings.MobileSaveStateText.text = operationSummary;
            }

            PublishDraftState();
        }

        private void SetMobilePartFocus(MobilePartFocus nextFocus)
        {
            _mobilePartFocus = nextFocus;
            Render();
        }

        private void SyncChrome(GarageResultViewModel resultViewModel)
        {
            _chromeController ??= GaragePageChromeBindingResolver.CreateController(this, ref _chromeBindings);
            if (_chromeController == null)
                return;

            var selectedSlotIndex = _state != null ? _state.SelectedSlotIndex : 0;
            var committedRosterCount = _state != null ? _state.CommittedRoster.Count : 0;

            _chromeController.ApplyState(
                transform,
                _isSettingsOverlayOpen,
                _saveFlow.IsSaving,
                _mobilePartFocus == MobilePartFocus.Frame,
                _mobilePartFocus == MobilePartFocus.Firepower,
                _mobilePartFocus == MobilePartFocus.Mobility,
                selectedSlotIndex,
                committedRosterCount,
                resultViewModel);
        }

        private static GarageEditorFocus ToEditorFocus(MobilePartFocus focus)
        {
            return focus switch
            {
                MobilePartFocus.Frame => GarageEditorFocus.Frame,
                MobilePartFocus.Firepower => GarageEditorFocus.Firepower,
                _ => GarageEditorFocus.Mobility,
            };
        }

        private static MobilePartFocus ToMobilePartFocus(GarageEditorFocus focus)
        {
            return focus switch
            {
                GarageEditorFocus.Firepower => MobilePartFocus.Firepower,
                GarageEditorFocus.Mobility => MobilePartFocus.Mobility,
                _ => MobilePartFocus.Frame,
            };
        }

        private void HandleKeyboardAction(GaragePageKeyboardAction action)
        {
            switch (action.Kind)
            {
                case GaragePageKeyboardActionKind.Save:
                    _ = RunSaveAsync();
                    break;
                case GaragePageKeyboardActionKind.SelectSlot:
                    SelectSlot(action.SlotIndex);
                    break;
                case GaragePageKeyboardActionKind.CyclePart:
                    CycleFocusedPart(action.Delta);
                    break;
            }
        }

        private void CycleFocusedPart(int delta)
        {
            if (!string.IsNullOrEmpty(_state.EditingFrameId))
            {
                CycleFrame(delta);
                return;
            }

            if (!string.IsNullOrEmpty(_state.EditingFirepowerId))
            {
                CycleFirepower(delta);
                return;
            }

            if (!string.IsNullOrEmpty(_state.EditingMobilityId))
            {
                CycleMobility(delta);
                return;
            }

            CycleFrame(delta);
        }

        private void BeginSave(GarageDraftEvaluation evaluation)
        {
            var operationSummary = GarageOperationRecordSummaryFormatter.BuildSummary(_recentOperations);
            SyncChrome(_presenter.BuildResultViewModel(_state, evaluation, operationSummary));
            _resultPanelView.ShowLoading(true);
        }

        private void EndSave()
        {
            _resultPanelView.ShowLoading(false);
        }

        private void ApplySaveResult(GarageSaveFlowResult result)
        {
            switch (result.Kind)
            {
                case GarageSaveFlowResultKind.Ignored:
                    return;
                case GarageSaveFlowResultKind.Saved:
                    CompleteSave();
                    return;
                case GarageSaveFlowResultKind.Blocked:
                case GarageSaveFlowResultKind.Failed:
                    ShowValidationOverride(result.Message);
                    return;
            }
        }

        private void ShowValidationOverride(string message)
        {
            _state.SetValidationOverride(message);
            Render();
        }

        private void CompleteSave()
        {
            _state.CommitDraft();
            _resultPanelView.ShowToast("Roster saved!");
            Render();
            _scrollController.ScrollBodyToTop(_chromeBindings != null ? _chromeBindings.MobileBodyHost : null);
        }

        private void PublishDraftState()
        {
            EnsureInitialized();
            var draftState = _draftStatePublisher.Build(_state.CommittedRoster, _state.HasDraftChanges());
            _eventPublisher.Publish(new GarageDraftStateChangedEvent(
                _state.CommittedRoster.Count,
                draftState.HasUnsavedChanges,
                draftState.ReadyEligible,
                draftState.BlockReason));
        }

        private GarageDraftEvaluation EvaluateDraft()
        {
            EnsureInitialized();
            return GarageDraftEvaluator.Evaluate(_state, _catalog, _composeUnit, _validateRoster);
        }

        private void EnsureInitialized()
        {
            if (_initializeGarage == null ||
                _composeUnit == null ||
                _validateRoster == null ||
                _saveRoster == null ||
                _eventPublisher == null ||
                _catalog == null ||
                _chromeBindings == null ||
                _state == null)
                throw new System.InvalidOperationException("GaragePageController.Initialize must be called before interaction.");
        }

        private static string CycleId<T>(
            string currentId,
            System.Collections.Generic.IReadOnlyList<T> items,
            int delta,
            System.Func<T, string> getId)
        {
            if (items == null || items.Count == 0)
                return null;

            int currentIndex = -1;
            for (int i = 0; i < items.Count; i++)
            {
                if (getId(items[i]) == currentId)
                {
                    currentIndex = i;
                    break;
                }
            }

            if (currentIndex < 0)
                currentIndex = 0;

            int nextIndex = (currentIndex + delta + items.Count) % items.Count;
            return getId(items[nextIndex]);
        }

    }
}
