using System;
using Features.Player.Application.Events;
using Features.Player.Domain;

namespace Features.Player.Application
{
    public sealed class OperationRecordFactory
    {
        public OperationRecord Build(GameEndReportRequestedEvent report, long endedAtUnixMs)
        {
            var hasCoreHealth = report.CoreMaxHealth > 0f;
            var coreHealthPercent = hasCoreHealth
                ? report.CoreRemainingHealth / report.CoreMaxHealth
                : OperationRecord.UnknownCoreHealthPercent;

            var record = new OperationRecord
            {
                operationId = BuildOperationId(endedAtUnixMs),
                endedAtUnixMs = Math.Max(0L, endedAtUnixMs),
                result = report.IsVictory
                    ? OperationRecordResult.Held
                    : OperationRecordResult.BaseCollapsed,
                survivalSeconds = report.PlayTimeSeconds,
                reachedWave = report.ReachedWave,
                hasCoreHealthPercent = hasCoreHealth,
                coreHealthPercent = coreHealthPercent,
                summonCount = report.SummonCount,
                unitKillCount = report.UnitKillCount,
                pressureSummaryKey = BuildPressureSummaryKey(report)
            };

            AddPrimaryUnits(record, report.ContributionCards);
            record.Normalize();
            return record;
        }

        private static string BuildOperationId(long endedAtUnixMs)
        {
            return $"operation-{Math.Max(0L, endedAtUnixMs)}";
        }

        private static string BuildPressureSummaryKey(GameEndReportRequestedEvent report)
        {
            if (report.UnitKillCount > 0)
                return "pressure.cleared";

            if (report.CoreMaxHealth > 0f && report.CoreRemainingHealth <= 0f)
                return "pressure.core-collapsed";

            return "pressure.wave-reached";
        }

        private static void AddPrimaryUnits(OperationRecord record, ResultContributionCard[] cards)
        {
            if (cards == null)
                return;

            for (var i = 0; i < cards.Length && record.primaryRosterUnits.Count < 2; i++)
            {
                var unitKey = !string.IsNullOrWhiteSpace(cards[i].LoadoutKey)
                    ? cards[i].LoadoutKey
                    : cards[i].UnitId.Value;
                if (string.IsNullOrWhiteSpace(unitKey))
                    continue;

                unitKey = unitKey.Trim();
                if (record.primaryRosterUnits.Contains(unitKey))
                    continue;

                record.primaryRosterUnits.Add(unitKey);
            }
        }
    }
}
