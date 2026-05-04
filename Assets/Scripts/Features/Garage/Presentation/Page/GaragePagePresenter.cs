using System.Collections.Generic;

namespace Features.Garage.Presentation
{
    public sealed class GaragePagePresenter
    {
        private readonly GarageSlotViewModelBuilder _slotBuilder;
        private readonly GarageEditorViewModelBuilder _editorBuilder;
        private readonly GarageResultViewModelBuilder _resultBuilder;

        public GaragePagePresenter(GaragePanelCatalog catalog)
        {
            _slotBuilder = new GarageSlotViewModelBuilder(catalog);
            _editorBuilder = new GarageEditorViewModelBuilder(catalog);
            _resultBuilder = new GarageResultViewModelBuilder(catalog);
        }

        public IReadOnlyList<GarageSlotViewModel> BuildSlotViewModels(
            GaragePageState state,
            IReadOnlyDictionary<string, GarageUnitServiceTag> serviceTagsByLoadoutKey = null)
            => _slotBuilder.Build(state, serviceTagsByLoadoutKey);

        public GarageEditorViewModel BuildEditorViewModel(GaragePageState state)
            => _editorBuilder.Build(state);

        public GarageResultViewModel BuildResultViewModel(
            GaragePageState state,
            GarageDraftEvaluation evaluation,
            string operationSummary = null)
            => _resultBuilder.Build(state, evaluation, operationSummary);
    }
}
