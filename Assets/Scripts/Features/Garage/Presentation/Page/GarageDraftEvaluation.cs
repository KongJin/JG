using Shared.Kernel;
using ComposedUnit = Features.Unit.Domain.Unit;

namespace Features.Garage.Presentation
{
    public sealed class GarageDraftEvaluation
    {
        private const string DraftNotReadyMessage = "Draft is not ready to save.";
        private const string NoUnsavedChangesMessage = "No unsaved changes.";

        private GarageDraftEvaluation(
            bool hasCatalogData,
            bool hasCompleteDraft,
            bool hasDraftChanges,
            bool hasSelectedDraftChanges,
            bool matchesCommittedSelection,
            bool wasComposeEvaluated,
            Result<ComposedUnit> composeResult,
            Result<ComposedUnit> committedComposeResult,
            Result rosterValidationResult)
        {
            HasCatalogData = hasCatalogData;
            HasCompleteDraft = hasCompleteDraft;
            HasDraftChanges = hasDraftChanges;
            HasSelectedDraftChanges = hasSelectedDraftChanges;
            MatchesCommittedSelection = matchesCommittedSelection;
            WasComposeEvaluated = wasComposeEvaluated;
            ComposeResult = composeResult;
            CommittedComposeResult = committedComposeResult;
            RosterValidationResult = rosterValidationResult;
        }

        public bool HasCatalogData { get; }
        public bool HasCompleteDraft { get; }
        public bool HasDraftChanges { get; }
        public bool HasSelectedDraftChanges { get; }
        public bool MatchesCommittedSelection { get; }
        public bool WasComposeEvaluated { get; }
        public Result<ComposedUnit> ComposeResult { get; }
        public Result<ComposedUnit> CommittedComposeResult { get; }
        public Result RosterValidationResult { get; }
        public bool HasComposedUnit => WasComposeEvaluated && ComposeResult.IsSuccess;
        public string ComposeError => WasComposeEvaluated ? ComposeResult.Error : "Garage catalog is unavailable.";
        public bool CanSave => HasSelectedDraftChanges && RosterValidationResult.IsSuccess;
        public string RosterValidationError => RosterValidationResult.Error;
        public string SaveBlockedMessage => !string.IsNullOrWhiteSpace(RosterValidationError)
            ? RosterValidationError
            : HasDraftChanges
                ? DraftNotReadyMessage
                : NoUnsavedChangesMessage;

        public static GarageDraftEvaluation Create(
            GaragePageState state,
            bool hasCatalogData,
            Result<ComposedUnit> composeResult,
            Result<ComposedUnit> committedComposeResult,
            Result rosterValidationResult)
        {
            bool hasCompleteDraft = state != null && state.HasCompleteDraft();

            return new GarageDraftEvaluation(
                hasCatalogData,
                hasCompleteDraft,
                state != null && state.HasDraftChanges(),
                state != null && state.SelectedSlotHasDraftChanges(),
                state != null && state.DraftMatchesCommittedSelection(),
                hasCatalogData && hasCompleteDraft,
                composeResult,
                committedComposeResult,
                rosterValidationResult);
        }

        public static GarageDraftEvaluation Create(
            GaragePageState state,
            bool hasCatalogData,
            Result<ComposedUnit> composeResult,
            Result rosterValidationResult)
        {
            return Create(
                state,
                hasCatalogData,
                composeResult,
                Result<ComposedUnit>.Failure("Committed composition was not evaluated."),
                rosterValidationResult);
        }
    }
}
