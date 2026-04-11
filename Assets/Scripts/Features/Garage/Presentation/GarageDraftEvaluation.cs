using Shared.Kernel;
using ComposedUnit = Features.Unit.Domain.Unit;

namespace Features.Garage.Presentation
{
    public sealed class GarageDraftEvaluation
    {
        private GarageDraftEvaluation(
            bool hasCatalogData,
            bool hasCompleteDraft,
            bool matchesCommittedSelection,
            bool wasComposeEvaluated,
            Result<ComposedUnit> composeResult)
        {
            HasCatalogData = hasCatalogData;
            HasCompleteDraft = hasCompleteDraft;
            MatchesCommittedSelection = matchesCommittedSelection;
            WasComposeEvaluated = wasComposeEvaluated;
            ComposeResult = composeResult;
        }

        public bool HasCatalogData { get; }
        public bool HasCompleteDraft { get; }
        public bool MatchesCommittedSelection { get; }
        public bool WasComposeEvaluated { get; }
        public Result<ComposedUnit> ComposeResult { get; }
        public bool HasComposedUnit => WasComposeEvaluated && ComposeResult.IsSuccess;
        public string ComposeError => WasComposeEvaluated ? ComposeResult.Error : "Garage catalog is unavailable.";

        public static GarageDraftEvaluation Create(
            GaragePageState state,
            bool hasCatalogData,
            Result<ComposedUnit> composeResult)
        {
            bool hasCompleteDraft = state != null && state.HasCompleteDraft();

            return new GarageDraftEvaluation(
                hasCatalogData,
                hasCompleteDraft,
                state != null && state.DraftMatchesCommittedSelection(),
                hasCatalogData && hasCompleteDraft,
                composeResult);
        }
    }
}
