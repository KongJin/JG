using Features.Garage.Application;
using Features.Unit.Application;
using Shared.Kernel;
using ComposedUnit = Features.Unit.Domain.Unit;

namespace Features.Garage.Presentation
{
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
