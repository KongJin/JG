using Features.Garage.Application;
using Features.Garage.Domain;
using Features.Unit.Application;
using Shared.Attributes;
using Shared.EventBus;
using Shared.Kernel;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
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
            _resultPanelView.SaveClicked += RequestSave;

            _mobileEditTabButton.onClick.AddListener(() => SetMobilePartFocus(MobilePartFocus.Frame));
            _mobileFirepowerTabButton.onClick.AddListener(() => SetMobilePartFocus(MobilePartFocus.Firepower));
            _mobileSummaryTabButton.onClick.AddListener(() => SetMobilePartFocus(MobilePartFocus.Mobility));

            _settingsOpenButton.onClick.AddListener(() => SetSettingsOverlayOpen(!_isSettingsOverlayOpen));
            _settingsCloseButton.onClick.AddListener(() => SetSettingsOverlayOpen(false));
            _mobileSaveButton.onClick.AddListener(RequestSave);
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
            if (!GaragePageKeyboardShortcuts.TryGetCurrentAction(out var action))
                return;

            HandleKeyboardAction(action);
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
            if (_isSaving)
                return;

            EnsureInitialized();
            var evaluation = EvaluateDraft();
            if (!evaluation.CanSave)
            {
                ShowValidationOverride(evaluation.SaveBlockedMessage);
                return;
            }

            BeginSave(evaluation);
            Result result;
            try
            {
                result = await _saveRoster.Execute(_state.DraftRoster.Clone());
            }
            finally
            {
                EndSave();
            }

            if (!result.IsSuccess)
            {
                ShowValidationOverride(result.Error);
                return;
            }

            CompleteSave();
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
            _isSaving = true;
            SyncChrome(_presenter.BuildResultViewModel(_state, evaluation));
            _resultPanelView.ShowLoading(true);
        }

        private void EndSave()
        {
            _isSaving = false;
            _resultPanelView.ShowLoading(false);
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
            _scrollController.ScrollBodyToTop(_mobileBodyHost);
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
