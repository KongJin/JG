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
        }

        private void SelectSlot(int slotIndex)
        {
            _state.SelectSlot(slotIndex);
            Render();
        }

        private void CycleFrame(int delta)
        {
            _state.SetEditingFrameId(CycleId(_state.EditingFrameId, _catalog?.Frames, delta, frame => frame.Id));
            TryCommitEditingDraft();
            Render();
        }

        private void CycleFirepower(int delta)
        {
            _state.SetEditingFirepowerId(CycleId(_state.EditingFirepowerId, _catalog?.Firepower, delta, module => module.Id));
            TryCommitEditingDraft();
            Render();
        }

        private void CycleMobility(int delta)
        {
            _state.SetEditingMobilityId(CycleId(_state.EditingMobilityId, _catalog?.Mobility, delta, module => module.Id));
            TryCommitEditingDraft();
            Render();
        }

        private async void ClearSelectedSlot()
        {
            var updatedRoster = _state.CommittedRoster.Clone();
            updatedRoster.ClearSlot(_state.SelectedSlotIndex);

            var result = await _setup.SaveRoster.Execute(updatedRoster);
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
            _rosterListView.Render(_presenter.BuildSlotViewModels(_state));
            _unitEditorView.Render(_presenter.BuildEditorViewModel(_state));
            _resultPanelView.Render(_presenter.BuildResultViewModel(_state, evaluation));
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
