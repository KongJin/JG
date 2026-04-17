using Shared.Kernel;
using ComposedUnit = Features.Unit.Domain.Unit;

namespace Features.Garage.Presentation
{
    public sealed class GarageDraftEvaluation
    {
        private GarageDraftEvaluation(
            bool hasCatalogData,
            bool hasCompleteDraft,
            bool hasDraftChanges,
            bool matchesCommittedSelection,
            bool wasComposeEvaluated,
            Result<ComposedUnit> composeResult,
            Result rosterValidationResult)
        {
            HasCatalogData = hasCatalogData;
            HasCompleteDraft = hasCompleteDraft;
            HasDraftChanges = hasDraftChanges;
            MatchesCommittedSelection = matchesCommittedSelection;
            WasComposeEvaluated = wasComposeEvaluated;
            ComposeResult = composeResult;
            RosterValidationResult = rosterValidationResult;
        }

        public bool HasCatalogData { get; }
        public bool HasCompleteDraft { get; }
        public bool HasDraftChanges { get; }
        public bool MatchesCommittedSelection { get; }
        public bool WasComposeEvaluated { get; }
        public Result<ComposedUnit> ComposeResult { get; }
        public Result RosterValidationResult { get; }
        public bool HasComposedUnit => WasComposeEvaluated && ComposeResult.IsSuccess;
        public string ComposeError => WasComposeEvaluated ? ComposeResult.Error : "Garage catalog is unavailable.";
        public bool CanSave => HasDraftChanges && RosterValidationResult.IsSuccess;
        public string RosterValidationError => RosterValidationResult.Error;

        public static GarageDraftEvaluation Create(
            GaragePageState state,
            bool hasCatalogData,
            Result<ComposedUnit> composeResult,
            Result rosterValidationResult)
        {
            bool hasCompleteDraft = state != null && state.HasCompleteDraft();

            return new GarageDraftEvaluation(
                hasCatalogData,
                hasCompleteDraft,
                state != null && state.HasDraftChanges(),
                state != null && state.DraftMatchesCommittedSelection(),
                hasCatalogData && hasCompleteDraft,
                composeResult,
                rosterValidationResult);
        }
    }
}
