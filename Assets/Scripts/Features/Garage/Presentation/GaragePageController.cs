using Features.Garage.Application;
using Features.Garage.Domain;
using Features.Unit.Application;
using Shared.Attributes;
using Shared.EventBus;
using Shared.Kernel;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;
using ComposedUnit = Features.Unit.Domain.Unit;

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

        [Header("Preview")]
        [Required, SerializeField] private GarageUnitPreviewView _unitPreviewView;

        [Header("Layout")]
        [Required, SerializeField] private GameObject _mobileContentRoot;
        [Required, SerializeField] private Transform _mobileBodyHost;
        [Required, SerializeField] private Transform _mobileSlotHost;
        [Required, SerializeField] private GameObject _rightRailRoot;
        [Required, SerializeField] private GameObject _previewCard;
        [Required, SerializeField] private GameObject _resultPane;
        [Required, SerializeField] private GameObject _mobileTabBar;
        [Required, SerializeField] private Button _mobileEditTabButton;
        [Required, SerializeField] private TMP_Text _mobileEditTabLabel;
        [FormerlySerializedAs("_mobilePreviewTabButton")]
        [Required, SerializeField] private Button _mobileFirepowerTabButton;
        [FormerlySerializedAs("_mobilePreviewTabLabel")]
        [Required, SerializeField] private TMP_Text _mobileFirepowerTabLabel;
        [Required, SerializeField] private Button _mobileSummaryTabButton;
        [Required, SerializeField] private TMP_Text _mobileSummaryTabLabel;
        [Required, SerializeField] private TMP_Text _garageHeaderSummaryText;
        [Required, SerializeField] private Button _settingsOpenButton;
        [Required, SerializeField] private TMP_Text _settingsOpenButtonLabel;
        [Required, SerializeField] private GameObject _settingsOverlayRoot;
        [Required, SerializeField] private Button _settingsCloseButton;
        [Required, SerializeField] private TMP_Text _settingsCloseButtonLabel;
        [Required, SerializeField] private GameObject _mobileSaveDockRoot;
        [Required, SerializeField] private Button _mobileSaveButton;
        [Required, SerializeField] private TMP_Text _mobileSaveButtonLabel;
        [Required, SerializeField] private TMP_Text _mobileSaveStateText;

        private InitializeGarageUseCase _initializeGarage;
        private ComposeUnitUseCase _composeUnit;
        private ValidateRosterUseCase _validateRoster;
        private SaveRosterUseCase _saveRoster;
        private IEventPublisher _eventPublisher;
        private GaragePanelCatalog _catalog;
        private GaragePageState _state;
        private GaragePagePresenter _presenter;
        private bool _callbacksHooked;
        private bool _isInitialized;
        private bool _isInitializingRoster;
        private bool _isSaving;
        private bool _isSettingsOverlayOpen;
        private MobilePartFocus _mobilePartFocus = MobilePartFocus.Frame;
        private readonly PublishGarageDraftStateUseCase _draftStatePublisher = new();
        private readonly GaragePageScrollController _scrollController = new();
        private GaragePageChromeController _chromeController;

        public void Initialize(
            InitializeGarageUseCase initializeGarage,
            ComposeUnitUseCase composeUnit,
            ValidateRosterUseCase validateRoster,
            SaveRosterUseCase saveRoster,
            IEventPublisher eventPublisher,
            GaragePanelCatalog catalog)
        {
            _initializeGarage = initializeGarage;
            _composeUnit = composeUnit;
            _validateRoster = validateRoster;
            _saveRoster = saveRoster;
            _eventPublisher = eventPublisher;
            _catalog = catalog;
            _presenter = new GaragePagePresenter(_catalog);
            _state ??= new GaragePageState();
            _chromeController ??= CreateChromeController();

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

            _rosterListView.SlotSelected += SelectSlot;
            _unitEditorView.FrameCycleRequested += CycleFrame;
            _unitEditorView.FirepowerCycleRequested += CycleFirepower;
            _unitEditorView.MobilityCycleRequested += CycleMobility;
            _unitEditorView.ClearRequested += ClearSelectedSlot;
            _resultPanelView.SaveClicked += () => _ = RunSaveAsync();

            _mobileEditTabButton.onClick.AddListener(() => SetMobilePartFocus(MobilePartFocus.Frame));
            _mobileFirepowerTabButton.onClick.AddListener(() => SetMobilePartFocus(MobilePartFocus.Firepower));
            _mobileSummaryTabButton.onClick.AddListener(() => SetMobilePartFocus(MobilePartFocus.Mobility));

            _settingsOpenButton.onClick.AddListener(() =>
            {
                _isSettingsOverlayOpen = !_isSettingsOverlayOpen;
                SyncChrome(null);
            });
            _settingsCloseButton.onClick.AddListener(() =>
            {
                if (_isSettingsOverlayOpen)
                {
                    _isSettingsOverlayOpen = false;
                    SyncChrome(null);
                }
            });
            _mobileSaveButton.onClick.AddListener(() => _ = RunSaveAsync());
        }

        private void OnDisable()
        {
            _isSettingsOverlayOpen = false;
            _chromeController?.HideSettingsOverlay();
        }

        private void OnEnable()
        {
            _chromeController ??= CreateChromeController();
            SyncChrome(null);
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            HandleKeyboardInput(keyboard);
        }

        private void SelectSlot(int slotIndex)
        {
            _state.SelectSlot(slotIndex);
            _mobilePartFocus = MobilePartFocus.Frame;
            Render();
            _scrollController.ScrollBodyToTop(_mobileBodyHost);
        }

        private void CycleFrame(int delta)
        {
            EnsureInitialized();
            _mobilePartFocus = MobilePartFocus.Frame;
            _state.SetEditingFrameId(CycleId(_state.EditingFrameId, _catalog.Frames, delta, frame => frame.Id));
            _state.ClearValidationOverride();
            Render();
        }

        private void CycleFirepower(int delta)
        {
            EnsureInitialized();
            _mobilePartFocus = MobilePartFocus.Firepower;
            _state.SetEditingFirepowerId(CycleId(_state.EditingFirepowerId, _catalog.Firepower, delta, module => module.Id));
            _state.ClearValidationOverride();
            Render();
        }

        private void CycleMobility(int delta)
        {
            EnsureInitialized();
            _mobilePartFocus = MobilePartFocus.Mobility;
            _state.SetEditingMobilityId(CycleId(_state.EditingMobilityId, _catalog.Mobility, delta, module => module.Id));
            _state.ClearValidationOverride();
            Render();
        }

        private void ClearSelectedSlot()
        {
            _state.ClearSelectedSlotDraft();
            _state.ClearValidationOverride();
            Render();
        }

        private async System.Threading.Tasks.Task RunSaveAsync()
        {
            if (_isSaving)
                return;

            EnsureInitialized();
            var evaluation = EvaluateDraft();
            if (!evaluation.CanSave)
            {
                string message = !string.IsNullOrWhiteSpace(evaluation.RosterValidationError)
                    ? evaluation.RosterValidationError
                    : evaluation.HasDraftChanges
                        ? "Draft is not ready to save."
                        : "No unsaved changes.";
                _state.SetValidationOverride(message);
                Render();
                return;
            }

            _isSaving = true;
            SyncChrome(_presenter.BuildResultViewModel(_state, evaluation));
            _resultPanelView.ShowLoading(true);

            var result = await _saveRoster.Execute(_state.DraftRoster.Clone());

            _isSaving = false;
            _resultPanelView.ShowLoading(false);

            if (!result.IsSuccess)
            {
                _state.SetValidationOverride(result.Error);
                Render();
                return;
            }

            _state.CommitDraft();
            _resultPanelView.ShowToast("Roster saved!");
            Render();
            _scrollController.ScrollBodyToTop(_mobileBodyHost);
        }

        private void Render()
        {
            EnsureInitialized();

            var evaluation = EvaluateDraft();
            var slotViewModels = _presenter.BuildSlotViewModels(_state);
            var resultViewModel = _presenter.BuildResultViewModel(_state, evaluation);

            _rosterListView.Render(slotViewModels);
            _unitEditorView.Render(_presenter.BuildEditorViewModel(_state));
            _unitEditorView.SetFocusedPart(ToEditorFocus(_mobilePartFocus));
            _resultPanelView.Render(resultViewModel);
            _resultPanelView.SetInlineSaveVisible(false);

            var selectedSlot = slotViewModels[_state.SelectedSlotIndex];
            _unitPreviewView.Render(selectedSlot);

            SyncChrome(resultViewModel);
            PublishDraftState();
        }

        private void SetMobilePartFocus(MobilePartFocus nextFocus)
        {
            _mobilePartFocus = nextFocus;
            Render();
        }

        private void SyncChrome(GarageResultViewModel resultViewModel)
        {
            _chromeController ??= CreateChromeController();
            _chromeController.ApplyState(
                transform,
                _isSettingsOverlayOpen,
                _isSaving,
                _mobilePartFocus == MobilePartFocus.Frame,
                _mobilePartFocus == MobilePartFocus.Firepower,
                _mobilePartFocus == MobilePartFocus.Mobility,
                _state.SelectedSlotIndex,
                _state.CommittedRoster.Count,
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

        private void HandleKeyboardInput(Keyboard keyboard)
        {
            if (keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed)
            {
                if (keyboard.sKey.wasPressedThisFrame)
                {
                    _ = RunSaveAsync();
                    return;
                }
            }

            if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame) { SelectSlot(0); return; }
            if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame) { SelectSlot(1); return; }
            if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame) { SelectSlot(2); return; }
            if (keyboard.digit4Key.wasPressedThisFrame || keyboard.numpad4Key.wasPressedThisFrame) { SelectSlot(3); return; }
            if (keyboard.digit5Key.wasPressedThisFrame || keyboard.numpad5Key.wasPressedThisFrame) { SelectSlot(4); return; }
            if (keyboard.digit6Key.wasPressedThisFrame || keyboard.numpad6Key.wasPressedThisFrame) { SelectSlot(5); return; }

            int delta = 0;
            if (keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame) delta = -1;
            else if (keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame) delta = 1;

            if (delta == 0)
                return;

            if (!string.IsNullOrEmpty(_state.EditingFrameId)) { CycleFrame(delta); return; }
            if (!string.IsNullOrEmpty(_state.EditingFirepowerId)) { CycleFirepower(delta); return; }
            if (!string.IsNullOrEmpty(_state.EditingMobilityId)) { CycleMobility(delta); return; }
            CycleFrame(delta);
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
            bool hasCatalogData = _catalog.Frames.Count > 0 &&
                                  _catalog.Firepower.Count > 0 &&
                                  _catalog.Mobility.Count > 0;

            Result<ComposedUnit> composeResult = Result<ComposedUnit>.Failure("Draft composition was not evaluated.");
            if (hasCatalogData && _state.HasCompleteDraft())
            {
                composeResult = _composeUnit.Execute(
                    Shared.Kernel.DomainEntityId.New(),
                    _state.EditingFrameId,
                    _state.EditingFirepowerId,
                    _state.EditingMobilityId);
            }

            Result rosterValidation = Result.Success();
            if (_state.HasDraftChanges())
            {
                rosterValidation = _validateRoster.Execute(_state.DraftRoster, out string validationError);
                if (rosterValidation.IsFailure && string.IsNullOrWhiteSpace(rosterValidation.Error) && !string.IsNullOrWhiteSpace(validationError))
                    rosterValidation = Result.Failure(validationError);
            }

            return GarageDraftEvaluation.Create(_state, hasCatalogData, composeResult, rosterValidation);
        }

        private void EnsureInitialized()
        {
            if (_initializeGarage == null ||
                _composeUnit == null ||
                _validateRoster == null ||
                _saveRoster == null ||
                _eventPublisher == null ||
                _catalog == null ||
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

        private GaragePageChromeController CreateChromeController()
        {
            return new GaragePageChromeController(
                _mobileContentRoot,
                _mobileSlotHost,
                _rightRailRoot,
                _previewCard,
                _resultPane,
                _mobileTabBar,
                _mobileEditTabButton,
                _mobileEditTabLabel,
                _mobileFirepowerTabButton,
                _mobileFirepowerTabLabel,
                _mobileSummaryTabButton,
                _mobileSummaryTabLabel,
                _garageHeaderSummaryText,
                _settingsOpenButton,
                _settingsOpenButtonLabel,
                _settingsOverlayRoot,
                _settingsCloseButton,
                _settingsCloseButtonLabel,
                _mobileSaveDockRoot,
                _mobileSaveButton,
                _mobileSaveButtonLabel,
                _mobileSaveStateText);
        }
    }
}
