using System.Collections.Generic;
using Features.Garage.Domain;
using Shared.Attributes;
using Shared.Kernel;
using UnityEngine;
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

        [Header("Account (optional — 분리 가능)")]
        [SerializeField] private UnityEngine.Component _accountSettingsView;
        [SerializeField] private GameObject _accountPanelRoot;

        private GarageSetup _setup;
        private GaragePanelCatalog _catalog;
        private GaragePageState _state;
        private GaragePagePresenter _presenter;
        private bool _callbacksHooked;
        private bool _isInitialized;

        public void Initialize(GarageSetup setup, GaragePanelCatalog catalog)
        {
            _setup = setup;
            _catalog = catalog;
            _presenter = new GaragePagePresenter(_catalog);
            _state ??= new GaragePageState();

            // AccountSettingsView를 GaragePageRoot 바깥으로 분리 (레이아웃 겹침 방지)
            SeparateAccountPanel();

            HookCallbacks();

            if (_isInitialized)
            {
                Render();
                return;
            }

            _isInitialized = true;
            _state.Initialize(_setup.InitializeGarage.Execute() ?? new GarageRoster());

            _ = SaveRosterAsync(_state.CommittedRoster);

            Render();
        }

        /// <summary>
        /// AccountSettingsView를 GaragePageRoot에서 분리하여 우측 패널과 겹치지 않게 배치.
        /// </summary>
        private void SeparateAccountPanel()
        {
            if (_accountSettingsView == null) return;
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

        private async System.Threading.Tasks.Task SaveRosterAsync(GarageRoster roster)
        {
            var result = await _setup.SaveRoster.Execute(roster);
            if (!result.IsSuccess)
                _state.SetValidationOverride(result.Error);
            Render();
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
            _resultPanelView.SaveClicked += OnSaveClicked;
        }

        private void SelectSlot(int slotIndex)
        {
            _state.SelectSlot(slotIndex);
            Render();
        }

        private void CycleFrame(int delta)
        {
            _state.SetEditingFrameId(CycleId(_state.EditingFrameId, _catalog?.Frames, delta, frame => frame.Id));
            var frameName = _catalog?.FindFrame(_state.EditingFrameId)?.DisplayName ?? _state.EditingFrameId;
            _resultPanelView.ShowToast($"Frame → {frameName}");
            TryCommitEditingDraft();
            Render();
        }

        private void CycleFirepower(int delta)
        {
            _state.SetEditingFirepowerId(CycleId(_state.EditingFirepowerId, _catalog?.Firepower, delta, module => module.Id));
            var wpName = _catalog?.FindFirepower(_state.EditingFirepowerId)?.DisplayName ?? _state.EditingFirepowerId;
            _resultPanelView.ShowToast($"Firepower → {wpName}");
            TryCommitEditingDraft();
            Render();
        }

        private void CycleMobility(int delta)
        {
            _state.SetEditingMobilityId(CycleId(_state.EditingMobilityId, _catalog?.Mobility, delta, module => module.Id));
            var mobName = _catalog?.FindMobility(_state.EditingMobilityId)?.DisplayName ?? _state.EditingMobilityId;
            _resultPanelView.ShowToast($"Mobility → {mobName}");
            TryCommitEditingDraft();
            Render();
        }

        private async void ClearSelectedSlot()
        {
            var updatedRoster = _state.CommittedRoster.Clone();
            updatedRoster.ClearSlot(_state.SelectedSlotIndex);

            _resultPanelView.ShowLoading(true);
            var result = await _setup.SaveRoster.Execute(updatedRoster);
            _resultPanelView.ShowLoading(false);

            if (!result.IsSuccess)
            {
                _state.SetValidationOverride(result.Error);
                Render();
                return;
            }

            _state.SetCommittedRoster(updatedRoster);
            _state.ClearSelectedSlotDraft();

            Render();
        }

        private async void OnSaveClicked()
        {
            // 저장 중 버튼 비활성화 + 로딩 표시 — 중복 클릭 방지
            _resultPanelView.ShowLoading(true);

            var result = await _setup.SaveRoster.Execute(_state.CommittedRoster);

            _resultPanelView.ShowLoading(false);

            if (result.IsSuccess)
            {
                _resultPanelView.ShowToast("Roster saved!");
            }
            else
            {
                _resultPanelView.ShowToast(result.Error, isError: true);
            }
        }

        private async void TryCommitEditingDraft()
        {
            _state.ClearValidationOverride();

            var evaluation = EvaluateDraft();
            if (!evaluation.HasCatalogData || !evaluation.HasCompleteDraft)
                return;

            if (!evaluation.HasComposedUnit)
            {
                _state.SetValidationOverride(evaluation.ComposeResult.Error);
                return;
            }

            var updatedRoster = _state.CommittedRoster.Clone();
            updatedRoster.SetSlot(_state.SelectedSlotIndex, new GarageRoster.UnitLoadout(
                _state.EditingFrameId,
                _state.EditingFirepowerId,
                _state.EditingMobilityId));

            var result = await _setup.SaveRoster.Execute(updatedRoster);
            if (!result.IsSuccess)
            {
                _state.SetValidationOverride(result.Error);
                return;
            }

            _state.SetCommittedRoster(updatedRoster);
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
                    DomainEntityId.New(),
                    _state.EditingFrameId,
                    _state.EditingFirepowerId,
                    _state.EditingMobilityId);
            }

            return GarageDraftEvaluation.Create(_state, hasCatalogData, composeResult);
        }

        private static string CycleId<T>(
            string currentId,
            IReadOnlyList<T> items,
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

            int nextIndex = currentIndex + delta;
            if (nextIndex < 0)
                nextIndex = items.Count - 1;
            if (nextIndex >= items.Count)
                nextIndex = 0;

            return getId(items[nextIndex]);
        }
    }
}
