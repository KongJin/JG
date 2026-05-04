namespace Features.Garage.Presentation
{
    internal sealed class GarageResultViewModelBuilder
    {
        private readonly GaragePanelCatalog _catalog;

        public GarageResultViewModelBuilder(GaragePanelCatalog catalog)
        {
            _catalog = catalog;
        }

        public GarageResultViewModel Build(
            GaragePageState state,
            GarageDraftEvaluation evaluation,
            string operationSummary = null)
        {
            int missingUnits = state.CommittedRoster.Count >= Domain.GarageRoster.MinReadySlots
                ? 0
                : Domain.GarageRoster.MinReadySlots - state.CommittedRoster.Count;
            bool readyEligible = state.CommittedRoster.IsValid && !evaluation.HasDraftChanges;
            string rosterStatusText = GarageUnitIdentityFormatter.BuildRosterStatusText(
                state.CommittedRoster.Count,
                missingUnits,
                readyEligible,
                evaluation.HasDraftChanges,
                evaluation.CanSave);

            return new GarageResultViewModel(
                rosterStatusText,
                BuildValidationText(state, evaluation),
                BuildStatsText(evaluation, operationSummary),
                isReady: readyEligible,
                isDirty: evaluation.HasDraftChanges,
                canSave: evaluation.CanSave,
                primaryActionLabel: GarageUnitIdentityFormatter.BuildPrimaryActionLabel(evaluation),
                radar: GarageStatRadarViewModelFactory.Build(evaluation, _catalog?.RadarScale));
        }

        private static string BuildValidationText(GaragePageState state, GarageDraftEvaluation evaluation)
        {
            if (!string.IsNullOrWhiteSpace(state.ValidationOverride))
                return state.ValidationOverride;

            if (!evaluation.HasDraftChanges)
            {
                return state.CommittedRoster.IsValid
                    ? "저장본이 최신입니다. 룸 패널에서 바로 준비할 수 있습니다."
                    : "최소 3기 이상 저장하면 준비 가능합니다.";
            }

            if (!evaluation.HasSelectedDraftChanges)
                return "선택 슬롯의 변경사항이 없습니다.";

            if (!evaluation.HasCompleteDraft)
                return "세 파츠를 모두 선택";

            if (!evaluation.HasCatalogData)
                return evaluation.ComposeError;

            if (!evaluation.HasComposedUnit)
                return evaluation.ComposeError;

            if (!evaluation.RosterValidationResult.IsSuccess)
                return evaluation.RosterValidationError;

            if (evaluation.MatchesCommittedSelection)
                return "현재 저장본과 동일합니다.";

            return "저장 시 선택 슬롯과 전체 편성이 동시에 갱신됩니다.";
        }

        private static string BuildStatsText(
            GarageDraftEvaluation evaluation,
            string operationSummary)
        {
            operationSummary = string.IsNullOrWhiteSpace(operationSummary)
                ? "최근 작전 기록 없음"
                : operationSummary;
            if (!evaluation.HasCompleteDraft)
                return operationSummary;

            if (!evaluation.HasCatalogData)
                return evaluation.ComposeError;

            if (!evaluation.HasComposedUnit)
                return evaluation.ComposeError;

            var unit = evaluation.ComposeResult.Value;
            return
                $"ENERGY {unit.SummonCost}  |  FRAME {unit.FrameEnergyCost}  |  FIRE {unit.FirepowerEnergyCost}  |  MOB {unit.MobilityEnergyCost}\n" +
                $"ATK {unit.FinalAttackDamage:0}  |  ASPD {unit.FinalAttackSpeed:0.00}  |  RNG {unit.FinalRange:0.0}m\n" +
                operationSummary;
        }
    }
}
