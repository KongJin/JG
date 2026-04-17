using Features.Garage.Application;
using Features.Garage.Domain;
using Shared.Attributes;
using Shared.Kernel;
using UnityEngine;
using UnityEngine.InputSystem;
using ComposedUnit = Features.Unit.Domain.Unit;

namespace Features.Garage.Presentation
{
    public sealed class GaragePageController : MonoBehaviour
    {
        [Header("Subviews")]
        [Required, SerializeField] private GarageRosterListView _rosterListView;
        [Required, SerializeField] private GarageUnitEditorView _unitEditorView;
        [Required, SerializeField] private GarageResultPanelView _resultPanelView;

        [Header("Preview")]
        [SerializeField] private GarageUnitPreviewView _unitPreviewView;

        [Header("Layout")]
        [Required, SerializeField] private RectTransform _rosterListPaneRect;
        [Required, SerializeField] private RectTransform _unitEditorPaneRect;
        [Required, SerializeField] private RectTransform _resultPaneRect;
        [Required, SerializeField] private RectTransform _previewRect;
        [SerializeField] private RectTransform _accountCardRect;

        [Header("Account (optional — 분리 가능)")]
        [SerializeField] private UnityEngine.Component _accountSettingsView;
        [SerializeField] private GameObject _accountPanelRoot;

        private GarageSetup _setup;
        private GaragePanelCatalog _catalog;
        private GaragePageState _state;
        private GaragePagePresenter _presenter;
        private bool _callbacksHooked;
        private bool _isInitialized;
        private bool _isInitializingRoster;

        public void Initialize(GarageSetup setup, GaragePanelCatalog catalog)
        {
            _setup = setup;
            _catalog = catalog;
            _presenter = new GaragePagePresenter(_catalog);
            _state ??= new GaragePageState();

            ConfigureWorkbenchLayout();
            SeparateAccountPanel();
            HookCallbacks();

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

        private void ConfigureWorkbenchLayout()
        {
            SetStretch(_rosterListPaneRect, 0.02f, 0.08f, 0.26f, 0.84f);
            SetStretch(_previewRect, 0.30f, 0.58f, 0.66f, 0.84f);
            SetStretch(_unitEditorPaneRect, 0.30f, 0.08f, 0.66f, 0.54f);
            SetStretch(_resultPaneRect, 0.70f, 0.22f, 0.98f, 0.84f);
            SetStretch(_accountCardRect, 0.72f, 0.86f, 0.98f, 0.98f);
        }

        private static void SetStretch(RectTransform rect, float minX, float minY, float maxX, float maxY)
        {
            if (rect == null)
                return;

            rect.anchorMin = new Vector2(minX, minY);
            rect.anchorMax = new Vector2(maxX, maxY);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void SeparateAccountPanel()
        {
            if (_accountSettingsView == null)
                return;

            var accountTransform = _accountSettingsView.transform;

            if (_accountPanelRoot != null && accountTransform.parent == _accountPanelRoot.transform)
                return;

            if (_accountPanelRoot != null)
            {
                accountTransform.SetParent(_accountPanelRoot.transform, false);
                accountTransform.localPosition = Vector3.zero;
                accountTransform.localRotation = Quaternion.identity;
                accountTransform.localScale = Vector3.one;
            }
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
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed)
            {
                if (keyboard.sKey.wasPressedThisFrame)
                {
                    OnSaveClicked();
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

            if (delta != 0)
            {
                if (!string.IsNullOrEmpty(_state.EditingFrameId)) { CycleFrame(delta); return; }
                if (!string.IsNullOrEmpty(_state.EditingFirepowerId)) { CycleFirepower(delta); return; }
                if (!string.IsNullOrEmpty(_state.EditingMobilityId)) { CycleMobility(delta); return; }
                CycleFrame(delta);
            }
        }

        private void SelectSlot(int slotIndex)
        {
            _state.SelectSlot(slotIndex);
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

        private async void OnSaveClicked()
        {
            var evaluation = EvaluateDraft();
            if (!evaluation.CanSave)
            {
                var message = !string.IsNullOrWhiteSpace(evaluation.RosterValidationError)
                    ? evaluation.RosterValidationError
                    : evaluation.HasDraftChanges
                        ? "Draft is not ready to save."
                        : "No unsaved changes.";
                _state.SetValidationOverride(message);
                Render();
                return;
            }

            _resultPanelView.ShowLoading(true);
            var result = await _setup.SaveRoster.Execute(_state.DraftRoster.Clone());
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
        }

        private void Render()
        {
            var evaluation = EvaluateDraft();
            var slotViewModels = _presenter.BuildSlotViewModels(_state);

            _rosterListView.Render(slotViewModels);
            _unitEditorView.Render(_presenter.BuildEditorViewModel(_state));
            _resultPanelView.Render(_presenter.BuildResultViewModel(_state, evaluation));

            if (_unitPreviewView != null && _catalog != null)
            {
                var selectedSlot = slotViewModels[_state.SelectedSlotIndex];
                _unitPreviewView.Render(selectedSlot, _catalog);
            }

            PublishDraftState();
        }

        private void PublishDraftState()
        {
            string blockReason;
            bool readyEligible;

            if (_state.HasDraftChanges())
            {
                readyEligible = false;
                blockReason = "Unsaved Garage changes";
            }
            else if (!_state.CommittedRoster.IsValid)
            {
                readyEligible = false;
                int missingUnits = Mathf.Max(0, 3 - _state.CommittedRoster.Count);
                blockReason = missingUnits > 0
                    ? $"Need {missingUnits} more saved unit{(missingUnits == 1 ? string.Empty : "s")}"
                    : "Saved roster is not ready";
            }
            else
            {
                readyEligible = true;
                blockReason = "Ready available";
            }

            _setup?.EventPublisher?.Publish(new GarageDraftStateChangedEvent(
                _state.CommittedRoster.Count,
                _state.HasDraftChanges(),
                readyEligible,
                blockReason));
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
    }
}
