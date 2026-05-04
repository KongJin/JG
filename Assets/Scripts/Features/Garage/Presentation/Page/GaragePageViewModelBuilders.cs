using System.Collections.Generic;

namespace Features.Garage.Presentation
{
    /// <summary>
    /// SetB 차고 페이지용 슬롯·에디터·결과 ViewModel 조립. 빌더 타입만 묶는 얇은 소유자.
    /// </summary>
    public sealed class GaragePageViewModelBuilders
    {
        private readonly GarageSlotViewModelBuilder _slotBuilder;
        private readonly GarageEditorViewModelBuilder _editorBuilder;
        private readonly GarageResultViewModelBuilder _resultBuilder;

        public GaragePageViewModelBuilders(GaragePanelCatalog catalog)
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
