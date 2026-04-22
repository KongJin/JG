using Features.Garage.Application;
using Features.Garage.Domain;
using Features.Garage.Presentation.Theme;
using Features.Unit.Application;
using Shared.Attributes;
using Shared.EventBus;
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
        [Required, SerializeField] private Button _mobilePreviewTabButton;
        [Required, SerializeField] private TMP_Text _mobilePreviewTabLabel;
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
        private readonly GarageKeyboardInputHandler _keyboardInputHandler = new();
        private readonly PublishGarageDraftStateUseCase _draftStatePublisher = new();

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

            _unitPreviewView.Initialize();
            HookCallbacks();
            ApplyMobileLayout();

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
            _unitEditorView.PartHoverRequested += ShowPartHoverTooltip;
            _resultPanelView.SaveClicked += OnSaveClicked;

            _mobileEditTabButton.onClick.AddListener(() => SetMobilePartFocus(MobilePartFocus.Frame));
            _mobilePreviewTabButton.onClick.AddListener(() => SetMobilePartFocus(MobilePartFocus.Firepower));
            _mobileSummaryTabButton.onClick.AddListener(() => SetMobilePartFocus(MobilePartFocus.Mobility));

            _settingsOpenButton.onClick.AddListener(ToggleSettingsOverlay);
            _settingsCloseButton.onClick.AddListener(HideSettingsOverlay);
            _mobileSaveButton.onClick.AddListener(OnSaveClicked);
        }

        private void OnDisable()
        {
            _isSettingsOverlayOpen = false;
            _settingsOverlayRoot.SetActive(false);
        }

        private void OnEnable()
        {
            SetActive(_mobileContentRoot, true);
            SetActive(GetLegacyContentRowRoot(), false);
            SetActive(_mobileTabBar, true);
            SetActive(_mobileSaveDockRoot, true);

            if (_mobileSlotHost != null)
                SetActive(_mobileSlotHost.gameObject, true);
        }

        private void Update()
        {
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
            _mobilePartFocus = MobilePartFocus.Frame;
            Render();
            ScrollMobileBodyToTop();
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

        private void ShowPartHoverTooltip(string partType, int delta)
        {
            EnsureInitialized();

            string currentId = partType switch
            {
                "frame" => _state.EditingFrameId,
                "firepower" => _state.EditingFirepowerId,
                "mobility" => _state.EditingMobilityId,
                _ => null
            };

            string nextId = partType switch
            {
                "frame" => CycleId(currentId, _catalog.Frames, delta, m => m.Id),
                "firepower" => CycleId(currentId, _catalog.Firepower, delta, m => m.Id),
                "mobility" => CycleId(currentId, _catalog.Mobility, delta, m => m.Id),
                _ => null
            };

            string currentName = partType switch
            {
                "frame" => _catalog.FindFrame(currentId)?.DisplayName ?? "—",
                "firepower" => _catalog.FindFirepower(currentId)?.DisplayName ?? "—",
                "mobility" => _catalog.FindMobility(currentId)?.DisplayName ?? "—",
                _ => "—"
            };

            string nextName = partType switch
            {
                "frame" => _catalog.FindFrame(nextId)?.DisplayName ?? "—",
                "firepower" => _catalog.FindFirepower(nextId)?.DisplayName ?? "—",
                "mobility" => _catalog.FindMobility(nextId)?.DisplayName ?? "—",
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
            RefreshMobileSaveButton(_presenter.BuildResultViewModel(_state, evaluation));
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
            ScrollMobileBodyToTop();
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
            _unitPreviewView.Render(selectedSlot, _catalog);

            ApplyMobileLayoutState(resultViewModel);
            PublishDraftState();
        }

        private void SetMobilePartFocus(MobilePartFocus nextFocus)
        {
            _mobilePartFocus = nextFocus;
            Render();
        }

        private void ToggleSettingsOverlay()
        {
            _isSettingsOverlayOpen = !_isSettingsOverlayOpen;
            ApplySectionVisibility();
            RefreshSettingsButtonStyles();
        }

        private void HideSettingsOverlay()
        {
            if (!_isSettingsOverlayOpen)
                return;

            _isSettingsOverlayOpen = false;
            ApplySectionVisibility();
            RefreshSettingsButtonStyles();
        }

        private void ApplyMobileLayoutState(GarageResultViewModel resultViewModel)
        {
            ApplyMobileLayout();
            ApplySectionVisibility();
            RefreshMobileTabButtonStyles();
            RefreshGarageHeaderSummary(resultViewModel);
            RefreshSettingsButtonStyles();
            RefreshMobileSaveButton(resultViewModel);
            RefreshMobileSaveStateText(resultViewModel);
        }

        private void ApplyMobileLayout()
        {
            var legacyContentRowRoot = GetLegacyContentRowRoot();

            SetActive(_mobileContentRoot, true);
            SetActive(legacyContentRowRoot, false);
            SetActive(_mobileSlotHost.gameObject, true);
            SetActive(_mobileTabBar, true);
            SetActive(_mobileSaveDockRoot, true);
        }

        private void ApplySectionVisibility()
        {
            SetActive(_mobileContentRoot, true);
            SetActive(GetLegacyContentRowRoot(), false);
            SetActive(_unitEditorView.gameObject, true);
            SetActive(_rightRailRoot, true);
            SetActive(_previewCard, true);
            SetActive(_resultPane, true);
            SetActive(_settingsOverlayRoot, _isSettingsOverlayOpen);
        }

        private void RefreshSettingsButtonStyles()
        {
            _settingsOpenButton.Apply(ButtonStyles.Ghost, _settingsOpenButtonLabel);
            _settingsOpenButton.interactable = !_isSettingsOverlayOpen;
            _settingsOpenButtonLabel.text = "⚙";

            _settingsCloseButton.Apply(ButtonStyles.Secondary, _settingsCloseButtonLabel);
            _settingsCloseButton.interactable = _isSettingsOverlayOpen;
            _settingsCloseButtonLabel.text = "닫기";
        }

        private void RefreshGarageHeaderSummary(GarageResultViewModel resultViewModel)
        {
            string readySummary = resultViewModel != null && resultViewModel.IsReady
                ? "저장본 최신"
                : resultViewModel != null && resultViewModel.IsDirty
                    ? resultViewModel.CanSave
                        ? "저장 가능"
                        : "조립 진행 중"
                    : $"활성 {_state.CommittedRoster.Count}/6";
            string focusSummary = GetMobilePartFocusLabel(_mobilePartFocus);

            _garageHeaderSummaryText.text =
                $"UNIT {_state.SelectedSlotIndex + 1:00}  |  {focusSummary}  |  {readySummary}";
            _garageHeaderSummaryText.color = ThemeColors.TextSecondary;
        }

        private void RefreshMobileTabButtonStyles()
        {
            ConfigureMobileTabButton(
                _mobileEditTabButton,
                _mobileEditTabLabel,
                "[프레임]",
                _mobilePartFocus == MobilePartFocus.Frame,
                true);
            ConfigureMobileTabButton(
                _mobilePreviewTabButton,
                _mobilePreviewTabLabel,
                "[무장]",
                _mobilePartFocus == MobilePartFocus.Firepower,
                true);
            ConfigureMobileTabButton(
                _mobileSummaryTabButton,
                _mobileSummaryTabLabel,
                "[기동]",
                _mobilePartFocus == MobilePartFocus.Mobility,
                true);
        }

        private void ConfigureMobileTabButton(
            Button button,
            TMP_Text label,
            string title,
            bool isActive,
            bool isAvailable)
        {
            var preset = isActive ? ButtonStyles.Primary : ButtonStyles.Secondary;
            button.Apply(preset, label);
            button.interactable = isAvailable && !isActive;

            label.text = title;
            label.color = isAvailable
                ? isActive ? ThemeColors.TextPrimary : ThemeColors.TextSecondary
                : ThemeColors.TextMuted;

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
            bool canSave = resultViewModel != null && resultViewModel.CanSave && !_isSaving;
            bool isDirty = resultViewModel != null && resultViewModel.IsDirty;
            bool isReady = resultViewModel != null && resultViewModel.IsReady;

            SetActive(_mobileSaveButton.gameObject, true);
            _mobileSaveButton.Apply(ButtonStyles.Primary, _mobileSaveButtonLabel);
            _mobileSaveButton.interactable = canSave;

            _mobileSaveButtonLabel.text = _isSaving
                ? "저장 중..."
                : "저장 및 배치";

            if (_mobileSaveButton.TryGetComponent<Image>(out var background))
            {
                Color readyBaseline = Color.Lerp(ThemeColors.AccentOrange, ThemeColors.BackgroundCard, 0.32f);
                background.color = _isSaving
                    ? ThemeColors.AccentOrange
                    : canSave
                        ? ThemeColors.AccentOrange
                        : isDirty
                            ? ThemeColors.BackgroundCard
                            : isReady
                                ? readyBaseline
                                : ThemeColors.StateDisabled;

                var feedback = _mobileSaveButton.GetComponent<ButtonFeedback>();
                if (feedback != null)
                    feedback.UpdateBaseColor(background.color);
            }
        }

        private void RefreshMobileSaveStateText(GarageResultViewModel resultViewModel)
        {
            if (_isSaving)
            {
                _mobileSaveStateText.text = "빌드 동기화 중...";
                _mobileSaveStateText.color = ThemeColors.TextPrimary;
                return;
            }

            if (resultViewModel == null)
            {
                _mobileSaveStateText.text = string.Empty;
                return;
            }

            if (resultViewModel.IsDirty && resultViewModel.CanSave)
            {
                _mobileSaveStateText.text = "현재 조합을 저장해 출격 편성에 반영";
                _mobileSaveStateText.color = ThemeColors.AccentAmber;
                return;
            }

            if (resultViewModel.IsDirty)
            {
                _mobileSaveStateText.text = resultViewModel.ValidationText;
                _mobileSaveStateText.color = ThemeColors.TextSecondary;
                return;
            }

            if (resultViewModel.IsReady)
            {
                _mobileSaveStateText.text = "저장본이 최신입니다 | 룸 패널에서 바로 출격 가능";
                _mobileSaveStateText.color = ThemeColors.AccentGreen;
                return;
            }

            _mobileSaveStateText.text = resultViewModel.ValidationText;
            _mobileSaveStateText.color = ThemeColors.TextSecondary;
        }

        private static void SetActive(GameObject target, bool isActive)
        {
            if (target != null && target.activeSelf != isActive)
                target.SetActive(isActive);
        }

        private GameObject GetLegacyContentRowRoot()
        {
            return transform.Find("GarageContentRow")?.gameObject;
        }

        private void ScrollMobileBodyToTop()
        {
            if (_mobileBodyHost == null)
                return;

            ScrollRect scrollRect = _mobileBodyHost.GetComponentInParent<ScrollRect>();
            if (scrollRect == null)
                return;

            Canvas.ForceUpdateCanvases();
            if (_mobileBodyHost.TryGetComponent<RectTransform>(out var contentRoot))
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);
            scrollRect.StopMovement();
            scrollRect.verticalNormalizedPosition = 1f;
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

        private static string GetMobilePartFocusLabel(MobilePartFocus focus)
        {
            return focus switch
            {
                MobilePartFocus.Frame => "프레임 포커스",
                MobilePartFocus.Firepower => "무장 포커스",
                _ => "기동 포커스",
            };
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
