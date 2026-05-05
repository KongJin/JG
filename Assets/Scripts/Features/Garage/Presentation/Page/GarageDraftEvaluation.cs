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

        /// <summary>
        /// 초안이 완성된 뒤 스탯/에너지 블록에 쓸 조합 유닛.
        /// 실패 시 <paramref name="composeErrorForDisplay"/>에 표시용 오류(또는 null)가 설정된다.
        /// </summary>
        public bool TryGetComposedUnitForStatsBlock(out ComposedUnit unit, out string composeErrorForDisplay)
        {
            unit = null;
            composeErrorForDisplay = null;
            if (!HasCompleteDraft)
                return false;

            if (!HasCatalogData || !HasComposedUnit)
            {
                composeErrorForDisplay = ComposeError;
                return false;
            }

            unit = ComposeResult.Value;
            return true;
        }

        /// <summary>
        /// 초안 완성 후 카탈로그/조합 불가 시 검증 문구에 쓸 메시지.
        /// </summary>
        public bool TryGetComposeUnavailableMessageWhenDraftComplete(out string message)
        {
            message = null;
            if (!HasCompleteDraft)
                return false;

            if (!HasCatalogData || !HasComposedUnit)
            {
                message = ComposeError;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 드래프트 조합·편성 검증을 수행해 평가 결과를 만든다.
        /// </summary>
        public static GarageDraftEvaluation Evaluate(
            GaragePageState state,
            GaragePanelCatalog catalog,
            ComposeUnitUseCase composeUnit,
            ValidateRosterUseCase validateRoster)
        {
            bool hasState = state != null;
            bool hasCatalogData = catalog != null &&
                                  catalog.Frames.Count > 0 &&
                                  catalog.Firepower.Count > 0 &&
                                  catalog.Mobility.Count > 0;

            Result<ComposedUnit> composeResult = Result<ComposedUnit>.Failure("Draft composition was not evaluated.");
            Result<ComposedUnit> committedComposeResult = Result<ComposedUnit>.Failure("Committed composition was not evaluated.");
            if (hasCatalogData && hasState && state.HasCompleteDraft())
            {
                composeResult = composeUnit.Execute(
                    DomainEntityId.New(),
                    state.EditingFrameId,
                    state.EditingFirepowerId,
                    state.EditingMobilityId);
            }

            var committedSlot = hasState ? state.GetSelectedCommittedSlot() : null;
            // csharp-guardrails: allow-null-defense
            if (hasCatalogData && committedSlot != null && committedSlot.IsComplete)
            {
                committedComposeResult = composeUnit.Execute(
                    DomainEntityId.New(),
                    committedSlot.frameId,
                    committedSlot.firepowerModuleId,
                    committedSlot.mobilityModuleId);
            }

            Result rosterValidation = Result.Success();
            if (hasState && state.SelectedSlotHasDraftChanges())
            {
                rosterValidation = validateRoster.ExecuteDraftSave(state.BuildSelectedSlotCommitRoster(), out string validationError);
                if (rosterValidation.IsFailure &&
                    string.IsNullOrWhiteSpace(rosterValidation.Error) &&
                    !string.IsNullOrWhiteSpace(validationError))
                {
                    rosterValidation = Result.Failure(validationError);
                }
            }

            return Create(state, hasCatalogData, composeResult, committedComposeResult, rosterValidation);
        }

        public static GarageDraftEvaluation Create(
            GaragePageState state,
            bool hasCatalogData,
            Result<ComposedUnit> composeResult,
            Result<ComposedUnit> committedComposeResult,
            Result rosterValidationResult)
        {
            bool hasState = state != null;
            bool hasCompleteDraft = hasState && state.HasCompleteDraft();

            return new GarageDraftEvaluation(
                hasCatalogData,
                hasCompleteDraft,
                hasState && state.HasDraftChanges(),
                hasState && state.SelectedSlotHasDraftChanges(),
                hasState && state.DraftMatchesCommittedSelection(),
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
