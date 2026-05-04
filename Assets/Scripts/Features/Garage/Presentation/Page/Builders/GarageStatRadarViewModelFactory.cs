using ComposedUnit = Features.Unit.Domain.Unit;

namespace Features.Garage.Presentation
{
    internal static class GarageStatRadarViewModelFactory
    {
        public static GarageStatRadarViewModel Build(
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
