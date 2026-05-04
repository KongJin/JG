using System.Collections.Generic;
using Features.Garage.Application;
using Features.Player.Domain;
using Features.Unit.Application;

namespace Features.Garage.Presentation
{
    /// <summary>
    /// SetB 차고 페이지의 <see cref="GarageRenderContext"/> 조립 전담.
    /// <see cref="GarageSetBUitkPageController"/>는 입력·비동기·어댑터에 집중한다.
    /// </summary>
    internal sealed class GarageSetBUitkPageRenderContextFactory
    {
        private readonly GaragePagePresenter _presenter;
        private readonly GaragePanelCatalog _catalog;
        private readonly ComposeUnitUseCase _composeUnit;
        private readonly ValidateRosterUseCase _validateRoster;

        public GarageSetBUitkPageRenderContextFactory(
            GaragePagePresenter presenter,
            GaragePanelCatalog catalog,
            ComposeUnitUseCase composeUnit,
            ValidateRosterUseCase validateRoster)
        {
            _presenter = presenter;
            _catalog = catalog;
            _composeUnit = composeUnit;
            _validateRoster = validateRoster;
        }

        public GarageDraftEvaluation EvaluateDraft(GaragePageState state)
        {
            return GarageDraftEvaluation.Evaluate(state, _catalog, _composeUnit, _validateRoster);
        }

        public GarageRenderContext Build(
            GaragePageState state,
            RecentOperationRecords recentOperations,
            GarageEditorFocus focusedPart,
            string partSearchText,
            bool isSettingsOpen,
            bool isLoading,
            bool isSaving,
            string currentOperationName)
        {
            var evaluation = EvaluateDraft(state);
            var operationSummary = GarageOperationRecordSummaryFormatter.BuildSummary(recentOperations);
            var serviceTags = GarageOperationRecordServiceTagMapper.BuildByLoadoutKey(recentOperations);
            IReadOnlyList<GarageSlotViewModel> slotViewModels = _presenter.BuildSlotViewModels(state, serviceTags);
            var partListViewModel = BuildPartListViewModel(
                state,
                GarageNovaPartsPanelViewModelFactory.ToPanelSlot(focusedPart),
                partSearchText);
            var editorViewModel = _presenter.BuildEditorViewModel(state);
            var resultViewModel = _presenter.BuildResultViewModel(state, evaluation, operationSummary);

            if (isLoading)
            {
                resultViewModel = new GarageResultViewModel(
                    resultViewModel.RosterStatusText,
                    currentOperationName,
                    resultViewModel.StatsText,
                    resultViewModel.IsReady,
                    resultViewModel.IsDirty,
                    canSave: false,
                    primaryActionLabel: "초기화 중...",
                    resultViewModel.Radar);
            }

            var snapshot = new GarageSetBUitkPageSnapshot(
                BuildRenderStatus(slotViewModels),
                state.SelectedSlotIndex,
                focusedPart,
                partSearchText,
                isSettingsOpen,
                evaluation.HasDraftChanges,
                resultViewModel.CanSave,
                resultViewModel.ValidationText,
                isLoading,
                isSaving,
                currentOperationName);

            return new GarageRenderContext(
                slotViewModels,
                partListViewModel,
                editorViewModel,
                resultViewModel,
                snapshot,
                evaluation);
        }

        public GarageNovaPartsPanelViewModel BuildPartListViewModel(
            GaragePageState state,
            GarageNovaPartPanelSlot slot,
            string partSearchText)
        {
            return GarageNovaPartsPanelViewModelFactory.Build(
                _catalog,
                new GarageNovaPartsDraftSelection(
                    state.EditingFrameId,
                    state.EditingFirepowerId,
                    state.EditingMobilityId),
                slot,
                partSearchText);
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
