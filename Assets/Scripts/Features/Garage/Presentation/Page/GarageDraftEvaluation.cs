using Features.Garage.Application;
using Features.Unit.Application;
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

    internal static class GarageDraftEvaluator
    {
        public static GarageDraftEvaluation Evaluate(
            GaragePageState state,
            GaragePanelCatalog catalog,
            ComposeUnitUseCase composeUnit,
            ValidateRosterUseCase validateRoster)
        {
            bool hasCatalogData = catalog != null &&
                                  catalog.Frames.Count > 0 &&
                                  catalog.Firepower.Count > 0 &&
                                  catalog.Mobility.Count > 0;

            Result<ComposedUnit> composeResult = Result<ComposedUnit>.Failure("Draft composition was not evaluated.");
            Result<ComposedUnit> committedComposeResult = Result<ComposedUnit>.Failure("Committed composition was not evaluated.");
            if (hasCatalogData && state != null && state.HasCompleteDraft())
            {
                composeResult = composeUnit.Execute(
                    DomainEntityId.New(),
                    state.EditingFrameId,
                    state.EditingFirepowerId,
                    state.EditingMobilityId);
            }

            var committed = state?.GetSelectedCommittedSlot();
            if (hasCatalogData && committed != null && committed.IsComplete)
            {
                committedComposeResult = composeUnit.Execute(
                    DomainEntityId.New(),
                    committed.frameId,
                    committed.firepowerModuleId,
                    committed.mobilityModuleId);
            }

            Result rosterValidation = Result.Success();
            if (state != null && state.SelectedSlotHasDraftChanges())
            {
                rosterValidation = validateRoster.ExecuteDraftSave(state.BuildSelectedSlotCommitRoster(), out string validationError);
                if (rosterValidation.IsFailure &&
                    string.IsNullOrWhiteSpace(rosterValidation.Error) &&
                    !string.IsNullOrWhiteSpace(validationError))
                {
                    rosterValidation = Result.Failure(validationError);
                }
            }

            return GarageDraftEvaluation.Create(state, hasCatalogData, composeResult, committedComposeResult, rosterValidation);
        }
    }
}
