using ComposedUnit = Features.Unit.Domain.Unit;

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
                radar: BuildStatRadarViewModel(evaluation, _catalog?.RadarScale));
        }

        private static string BuildValidationText(GaragePageState state, GarageDraftEvaluation evaluation)
        {
            if (!string.IsNullOrWhiteSpace(state.ValidationOverride))
                return state.ValidationOverride;

            if (!evaluation.HasDraftChanges)
            {
                return state.CommittedRoster.IsValid
                    ? "저장본이 최신입니다. 룸 패널에서 바로 준비할 수 있습니다."
                    : $"최소 {Domain.GarageRoster.MinReadySlots}기 이상 저장하면 준비 가능합니다.";
            }

            if (!evaluation.HasSelectedDraftChanges)
                return "선택 슬롯의 변경사항이 없습니다.";

            if (!evaluation.HasCompleteDraft)
                return "세 파츠를 모두 선택";

            if (evaluation.TryGetComposeUnavailableMessageWhenDraftComplete(out var composeBlock))
                return composeBlock;

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
            if (!evaluation.TryGetComposedUnitForStatsBlock(out var unit, out var composeErr))
            {
                if (!evaluation.HasCompleteDraft)
                    return operationSummary;

                return composeErr;
            }

            return
                $"ENERGY {unit.SummonCost}  |  FRAME {unit.FrameEnergyCost}  |  FIRE {unit.FirepowerEnergyCost}  |  MOB {unit.MobilityEnergyCost}\n" +
                $"ATK {unit.FinalAttackDamage:0}  |  ASPD {unit.FinalAttackSpeed:0.00}  |  RNG {unit.FinalRange:0.0}m\n" +
                operationSummary;
        }

        private static GarageStatRadarViewModel BuildStatRadarViewModel(
            GarageDraftEvaluation evaluation,
            GaragePanelCatalog.StatRadarScale scale)
        {
            if (evaluation == null || !evaluation.HasComposedUnit)
                return null;

            var current = evaluation.ComposeResult.Value;
            float[] previousValues = null;
            if (evaluation.HasDraftChanges && evaluation.CommittedComposeResult.IsSuccess)
                previousValues = BuildRadarValues(evaluation.CommittedComposeResult.Value, scale);

            return new GarageStatRadarViewModel(
                BuildRadarValues(current, scale),
                previousValues,
                current.SummonCost);
        }

        private static float[] BuildRadarValues(ComposedUnit unit, GaragePanelCatalog.StatRadarScale scale)
        {
            scale ??= new GaragePanelCatalog.StatRadarScale();
            return new[]
            {
                Normalize(unit.FinalAttackDamage, scale.AttackDamageMax),
                Normalize(unit.FinalAttackSpeed, scale.AttackSpeedMax),
                Normalize(unit.FinalRange, scale.RangeMax),
                Normalize(unit.FinalHp, scale.HpMax),
                Normalize(unit.FinalDefense, scale.DefenseMax),
                Normalize(unit.FinalMoveSpeed, scale.MoveSpeedMax),
                Normalize(unit.FinalMoveRange, scale.MoveRangeMax)
            };
        }

        private static float Normalize(float value, float max)
        {
            if (max <= 0f)
                return 0f;

            var normalized = value / max;
            if (normalized < 0f) return 0f;
            return normalized > 1f ? 1f : normalized;
        }
    }
}
