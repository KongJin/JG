using Features.Garage.Application;
using Features.Garage.Domain;
using Features.Garage.Presentation.Theme;
using Shared.Attributes;
using Shared.Kernel;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using ComposedUnit = Features.Unit.Domain.Unit;

namespace Features.Garage.Presentation
{
    public sealed class GaragePageController : MonoBehaviour
    {
        private enum MobileBodySection
        {
            Edit,
            Preview,
            Summary,
        }

        [Header("Subviews")]
        [Required, SerializeField] private GarageRosterListView _rosterListView;
        [Required, SerializeField] private GarageUnitEditorView _unitEditorView;
        [Required, SerializeField] private GarageResultPanelView _resultPanelView;

        [Header("Preview")]
        [SerializeField] private GarageUnitPreviewView _unitPreviewView;

        [Header("Responsive Layout")]
        [SerializeField] private RectTransform _responsiveRoot;
        [SerializeField] private GameObject _desktopContentRoot;
        [SerializeField] private GameObject _mobileContentRoot;
        [SerializeField] private Transform _mobileBodyHost;
        [SerializeField] private Transform _desktopSlotHost;
        [SerializeField] private Transform _mobileSlotHost;
        [SerializeField] private GameObject _rightRailRoot;
        [SerializeField] private GameObject _accountCard;
        [SerializeField] private GameObject _previewCard;
        [SerializeField] private GameObject _resultPane;
        [SerializeField] private Button _inlineSaveButton;
        [SerializeField] private GameObject _mobileTabBar;
        [SerializeField] private Button _mobileEditTabButton;
        [SerializeField] private TMP_Text _mobileEditTabLabel;
        [SerializeField] private Button _mobilePreviewTabButton;
        [SerializeField] private TMP_Text _mobilePreviewTabLabel;
        [SerializeField] private Button _mobileSummaryTabButton;
        [SerializeField] private TMP_Text _mobileSummaryTabLabel;
        [SerializeField] private Button _mobileSaveButton;
        [SerializeField] private TMP_Text _mobileSaveButtonLabel;
        [SerializeField] private float _mobileBreakpointWidth = 700f;

        private GarageSetup _setup;
        private GaragePanelCatalog _catalog;
        private GaragePageState _state;
        private GaragePagePresenter _presenter;
        private bool _callbacksHooked;
        private bool _isInitialized;
        private bool _isInitializingRoster;
        private bool _isSaving;
        private bool _isMobileLayout;
        private float _lastResponsiveWidth = -1f;
        private MobileBodySection _mobileBodySection = MobileBodySection.Edit;
        private readonly GarageKeyboardInputHandler _keyboardInputHandler = new();
        private readonly GarageSaveCommandHandler _saveCommandHandler = new();
        private readonly GarageDraftStatePublisher _draftStatePublisher = new();
        private readonly GarageResponsiveLayoutController _responsiveLayoutController = new();

        public void Initialize(GarageSetup setup, GaragePanelCatalog catalog)
        {
            _setup = setup;
            _catalog = catalog;
            _presenter = new GaragePagePresenter(_catalog);
            _state ??= new GaragePageState();

            _unitPreviewView?.Initialize();
            HookCallbacks();
            RefreshResponsiveModeIfNeeded();

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
                var roster = await _setup.InitializeGarage.Execute();
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
            _unitEditorView.PartHoverRequested += ShowPartHoverTooltip;
            _resultPanelView.SaveClicked += OnSaveClicked;

            if (_mobileEditTabButton != null)
                _mobileEditTabButton.onClick.AddListener(() => SwitchMobileSection(MobileBodySection.Edit));

            if (_mobilePreviewTabButton != null)
                _mobilePreviewTabButton.onClick.AddListener(() => SwitchMobileSection(MobileBodySection.Preview));

            if (_mobileSummaryTabButton != null)
                _mobileSummaryTabButton.onClick.AddListener(() => SwitchMobileSection(MobileBodySection.Summary));

            if (_mobileSaveButton != null)
                _mobileSaveButton.onClick.AddListener(OnSaveClicked);
        }

        private void Update()
        {
            if (RefreshResponsiveModeIfNeeded())
            {
                if (_isInitialized)
                    Render();
                return;
            }

            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            _keyboardInputHandler.Process(
                keyboard,
                _state,
                OnSaveClicked,
                SelectSlot,
                CycleFrame,
                CycleFirepower,
                CycleMobility);
        }

        private void SelectSlot(int slotIndex)
        {
            _state.SelectSlot(slotIndex);
            _mobileBodySection = MobileBodySection.Edit;
            Render();
        }

        private void CycleFrame(int delta)
        {
            _state.SetEditingFrameId(CycleId(_state.EditingFrameId, _catalog?.Frames, delta, frame => frame.Id));
            _state.ClearValidationOverride();
            Render();
        }

        private void CycleFirepower(int delta)
        {
            _state.SetEditingFirepowerId(CycleId(_state.EditingFirepowerId, _catalog?.Firepower, delta, module => module.Id));
            _state.ClearValidationOverride();
            Render();
        }

        private void CycleMobility(int delta)
        {
            _state.SetEditingMobilityId(CycleId(_state.EditingMobilityId, _catalog?.Mobility, delta, module => module.Id));
            _state.ClearValidationOverride();
            Render();
        }

        private void ShowPartHoverTooltip(string partType, int delta)
        {
            string currentId = partType switch
            {
                "frame" => _state.EditingFrameId,
                "firepower" => _state.EditingFirepowerId,
                "mobility" => _state.EditingMobilityId,
                _ => null
            };

            string nextId = partType switch
            {
                "frame" => CycleId(currentId, _catalog?.Frames, delta, m => m.Id),
                "firepower" => CycleId(currentId, _catalog?.Firepower, delta, m => m.Id),
                "mobility" => CycleId(currentId, _catalog?.Mobility, delta, m => m.Id),
                _ => null
            };

            string currentName = partType switch
            {
                "frame" => _catalog?.FindFrame(currentId)?.DisplayName ?? "—",
                "firepower" => _catalog?.FindFirepower(currentId)?.DisplayName ?? "—",
                "mobility" => _catalog?.FindMobility(currentId)?.DisplayName ?? "—",
                _ => "—"
            };

            string nextName = partType switch
            {
                "frame" => _catalog?.FindFrame(nextId)?.DisplayName ?? "—",
                "firepower" => _catalog?.FindFirepower(nextId)?.DisplayName ?? "—",
                "mobility" => _catalog?.FindMobility(nextId)?.DisplayName ?? "—",
                _ => "—"
            };

            _resultPanelView.ShowToast($"{currentName} -> {nextName}");
        }

        private void ClearSelectedSlot()
        {
            _state.ClearSelectedSlotDraft();
            _state.ClearValidationOverride();
            Render();
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public void WebglSmokeSelectSlot(string slotIndexText)
        {
            if (!TryParseIndex(slotIndexText, out int slotIndex))
            {
                Debug.LogWarning($"[GarageSmoke] Invalid slot index: {slotIndexText}");
                return;
            }

            SelectSlot(slotIndex);
        }

        public void WebglSmokeCycleFrame(string deltaText)
        {
            if (!TryParseDelta(deltaText, out int delta))
            {
                Debug.LogWarning($"[GarageSmoke] Invalid frame delta: {deltaText}");
                return;
            }

            CycleFrame(delta);
        }

        public void WebglSmokeCycleFirepower(string deltaText)
        {
            if (!TryParseDelta(deltaText, out int delta))
            {
                Debug.LogWarning($"[GarageSmoke] Invalid firepower delta: {deltaText}");
                return;
            }

            CycleFirepower(delta);
        }

        public void WebglSmokeCycleMobility(string deltaText)
        {
            if (!TryParseDelta(deltaText, out int delta))
            {
                Debug.LogWarning($"[GarageSmoke] Invalid mobility delta: {deltaText}");
                return;
            }

            CycleMobility(delta);
        }

        public void WebglSmokeSaveDraft()
        {
            OnSaveClicked();
        }
#endif

        private void OnSaveClicked()
        {
            _ = RunSaveAsync();
        }

        private async System.Threading.Tasks.Task RunSaveAsync()
        {
            if (_isSaving)
                return;

            var evaluation = EvaluateDraft();
            var saveResult = await _saveCommandHandler.ExecuteAsync(
                _state,
                evaluation,
                _setup,
                _resultPanelView,
                () =>
                {
                    _isSaving = true;
                    RefreshMobileSaveButton(_presenter.BuildResultViewModel(_state, evaluation));
                },
                () =>
                {
                    _isSaving = false;
                });

            if (!saveResult.ShouldRender)
                return;

            Render();
        }

        private void Render()
        {
            RefreshResponsiveModeIfNeeded();

            var evaluation = EvaluateDraft();
            var slotViewModels = _presenter.BuildSlotViewModels(_state);
            var resultViewModel = _presenter.BuildResultViewModel(_state, evaluation);

            _rosterListView.Render(slotViewModels);
            _unitEditorView.Render(_presenter.BuildEditorViewModel(_state));
            _resultPanelView.Render(resultViewModel);

            if (_unitPreviewView != null && _catalog != null)
            {
                var selectedSlot = slotViewModels[_state.SelectedSlotIndex];
                _unitPreviewView.Render(selectedSlot, _catalog);
            }

            ApplyResponsiveLayoutState(resultViewModel);
            PublishDraftState();
        }

        private bool RefreshResponsiveModeIfNeeded()
        {
            if (!TryGetResponsiveWidth(out float width))
                return false;

            var state = _responsiveLayoutController.Evaluate(width, _mobileBreakpointWidth, _lastResponsiveWidth, _isMobileLayout);
            if (!state.ShouldRefresh)
                return false;

            _lastResponsiveWidth = width;
            _isMobileLayout = state.IsMobileLayout;
            return true;
        }

        private bool TryGetResponsiveWidth(out float width)
        {
            if (Screen.width > 0)
            {
                width = Screen.width;
                return true;
            }

            var rectTransform = _responsiveRoot != null ? _responsiveRoot : transform as RectTransform;
            if (rectTransform != null && rectTransform.rect.width > 0f)
            {
                width = rectTransform.rect.width;
                return true;
            }

            width = 0f;
            return false;
        }

        private void SwitchMobileSection(MobileBodySection nextSection)
        {
            _mobileBodySection = nextSection;
            Render();
        }

        private void ApplyResponsiveLayoutState(GarageResultViewModel resultViewModel)
        {
            ApplyResponsiveParenting();

            if (_mobileTabBar != null)
                _mobileTabBar.SetActive(_isMobileLayout);

            if (_mobileSaveButton != null)
                _mobileSaveButton.gameObject.SetActive(_isMobileLayout);

            if (_inlineSaveButton != null)
                _inlineSaveButton.gameObject.SetActive(!_isMobileLayout);

            ApplySectionVisibility();
            RefreshMobileTabButtonStyles();
            RefreshMobileSaveButton(resultViewModel);
        }

        private void ApplyResponsiveParenting()
        {
            var rosterPane = _rosterListView != null ? _rosterListView.gameObject : null;
            var editorPane = _unitEditorView != null ? _unitEditorView.gameObject : null;
            if (rosterPane == null || editorPane == null || _rightRailRoot == null)
                return;

            if (_isMobileLayout)
            {
                SetActive(_desktopContentRoot, false);
                SetActive(_mobileContentRoot, true);
                SetActive(_mobileSlotHost != null ? _mobileSlotHost.gameObject : null, true);

                MoveToParent(rosterPane.transform, _mobileContentRoot != null ? _mobileContentRoot.transform : null, 0);
                MoveSlotsToHost(_mobileSlotHost);
                MoveToParent(_mobileTabBar != null ? _mobileTabBar.transform : null, _mobileContentRoot != null ? _mobileContentRoot.transform : null, 1);
                MoveToParent(editorPane.transform, _mobileBodyHost, 0);
                MoveToParent(_rightRailRoot.transform, _mobileBodyHost, 1);
                return;
            }

            SetActive(_mobileContentRoot, false);
            SetActive(_desktopContentRoot, true);
            SetActive(_mobileSlotHost != null ? _mobileSlotHost.gameObject : null, false);

            MoveToParent(rosterPane.transform, _desktopContentRoot != null ? _desktopContentRoot.transform : null, 0);
            MoveSlotsToHost(_desktopSlotHost);
            MoveToParent(editorPane.transform, _desktopContentRoot != null ? _desktopContentRoot.transform : null, 1);
            MoveToParent(_rightRailRoot.transform, _desktopContentRoot != null ? _desktopContentRoot.transform : null, 2);
        }

        private void MoveSlotsToHost(Transform host)
        {
            if (host == null || _rosterListView == null)
                return;

            var slotViews = _rosterListView.GetComponentsInChildren<GarageSlotItemView>(true);
            for (int index = 0; index < slotViews.Length; index++)
            {
                MoveToParent(slotViews[index].transform, host, index);
            }
        }

        private void ApplySectionVisibility()
        {
            if (!_isMobileLayout)
            {
                SetActive(_unitEditorView != null ? _unitEditorView.gameObject : null, true);
                SetActive(_rightRailRoot, true);
                SetActive(_accountCard, true);
                SetActive(_previewCard, true);
                SetActive(_resultPane, true);
                return;
            }

            bool showEdit = _mobileBodySection == MobileBodySection.Edit;
            bool showPreview = _mobileBodySection == MobileBodySection.Preview;
            bool showSummary = _mobileBodySection == MobileBodySection.Summary;

            SetActive(_unitEditorView != null ? _unitEditorView.gameObject : null, showEdit);
            SetActive(_rightRailRoot, showEdit || showPreview || showSummary);
            SetActive(_accountCard, showEdit);
            SetActive(_previewCard, showPreview);
            SetActive(_resultPane, showSummary);
        }

        private void RefreshMobileTabButtonStyles()
        {
            if (!_isMobileLayout)
                return;

            ConfigureMobileTabButton(
                _mobileEditTabButton,
                _mobileEditTabLabel,
                "Edit",
                _mobileBodySection == MobileBodySection.Edit,
                true);
            ConfigureMobileTabButton(
                _mobilePreviewTabButton,
                _mobilePreviewTabLabel,
                "Preview",
                _mobileBodySection == MobileBodySection.Preview,
                _unitPreviewView != null);
            ConfigureMobileTabButton(
                _mobileSummaryTabButton,
                _mobileSummaryTabLabel,
                "Summary",
                _mobileBodySection == MobileBodySection.Summary,
                _resultPanelView != null);
        }

        private void ConfigureMobileTabButton(
            Button button,
            TMP_Text label,
            string title,
            bool isActive,
            bool isAvailable)
        {
            if (button == null)
                return;

            var preset = isActive ? ButtonStyles.Primary : ButtonStyles.Secondary;
            button.Apply(preset, label);
            button.interactable = isAvailable && !isActive;

            if (label != null)
            {
                label.text = title;
                label.color = isAvailable
                    ? isActive ? ThemeColors.TextPrimary : ThemeColors.TextSecondary
                    : ThemeColors.TextMuted;
            }

            if (button.TryGetComponent<Image>(out var background))
            {
                background.color = !isAvailable
                    ? ThemeColors.StateDisabled
                    : isActive
                        ? ThemeColors.AccentBlue
                        : ThemeColors.BackgroundCard;

                var feedback = button.GetComponent<ButtonFeedback>();
                if (feedback != null)
                    feedback.UpdateBaseColor(background.color);
            }
        }

        private void RefreshMobileSaveButton(GarageResultViewModel resultViewModel)
        {
            if (_mobileSaveButton == null)
                return;

            bool canSave = resultViewModel != null && resultViewModel.CanSave && !_isSaving;
            bool isDirty = resultViewModel != null && resultViewModel.IsDirty;

            _mobileSaveButton.Apply(ButtonStyles.Primary, _mobileSaveButtonLabel);
            _mobileSaveButton.interactable = canSave;

            if (_mobileSaveButtonLabel != null)
                _mobileSaveButtonLabel.text = _isSaving
                    ? "Saving..."
                    : resultViewModel?.PrimaryActionLabel ?? "Save Roster";

            if (_mobileSaveButton.TryGetComponent<Image>(out var background))
            {
                background.color = _isSaving
                    ? ThemeColors.AccentBlue
                    : canSave
                        ? ThemeColors.AccentGreen
                        : isDirty
                            ? ThemeColors.AccentOrange
                            : ThemeColors.StateDisabled;

                var feedback = _mobileSaveButton.GetComponent<ButtonFeedback>();
                if (feedback != null)
                    feedback.UpdateBaseColor(background.color);
            }
        }

        private static void SetActive(GameObject target, bool isActive)
        {
            if (target != null && target.activeSelf != isActive)
                target.SetActive(isActive);
        }

        private static void MoveToParent(Transform child, Transform parent, int siblingIndex)
        {
            if (child == null || parent == null)
                return;

            if (child.parent != parent)
                child.SetParent(parent, false);

            if (siblingIndex >= 0 && child.GetSiblingIndex() != siblingIndex)
                child.SetSiblingIndex(siblingIndex);
        }

        private void PublishDraftState()
        {
            var draftState = _draftStatePublisher.Build(_state);
            _setup?.EventPublisher?.Publish(new GarageDraftStateChangedEvent(
                _state.CommittedRoster.Count,
                draftState.HasUnsavedChanges,
                draftState.ReadyEligible,
                draftState.BlockReason));
        }

        private GarageDraftEvaluation EvaluateDraft()
        {
            bool hasCatalogData = _catalog != null &&
                                  _catalog.Frames.Count > 0 &&
                                  _catalog.Firepower.Count > 0 &&
                                  _catalog.Mobility.Count > 0;

            Result<ComposedUnit> composeResult = Result<ComposedUnit>.Failure("Draft composition was not evaluated.");
            if (hasCatalogData && _state.HasCompleteDraft())
            {
                composeResult = _setup.ComposeUnit.Execute(
                    Shared.Kernel.DomainEntityId.New(),
                    _state.EditingFrameId,
                    _state.EditingFirepowerId,
                    _state.EditingMobilityId);
            }

            Result rosterValidation = Result.Success();
            if (_state.HasDraftChanges())
            {
                rosterValidation = _setup.ValidateRoster.Execute(_state.DraftRoster, out string validationError);
                if (rosterValidation.IsFailure && string.IsNullOrWhiteSpace(rosterValidation.Error) && !string.IsNullOrWhiteSpace(validationError))
                    rosterValidation = Result.Failure(validationError);
            }

            return GarageDraftEvaluation.Create(_state, hasCatalogData, composeResult, rosterValidation);
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

        private static bool TryParseIndex(string raw, out int slotIndex)
        {
            if (int.TryParse(raw, out int parsed))
            {
                slotIndex = Mathf.Clamp(parsed, 0, 5);
                return true;
            }

            slotIndex = 0;
            return false;
        }

        private static bool TryParseDelta(string raw, out int delta)
        {
            if (int.TryParse(raw, out int parsed) && parsed != 0)
            {
                delta = parsed;
                return true;
            }

            delta = 0;
            return false;
        }
    }
}
