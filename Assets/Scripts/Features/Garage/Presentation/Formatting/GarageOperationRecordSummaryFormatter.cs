using Features.Player.Domain;
using System.Collections.Generic;
using System.Text;

namespace Features.Garage.Presentation
{
    public static class GarageOperationRecordSummaryFormatter
    {
        public static string BuildSummary(RecentOperationRecords records)
        {
            if (records == null || records.Count <= 0)
                return "최근 작전 기록 없음";

            var latest = records.Records[0];
            var builder = new StringBuilder();
            builder.Append("작전 ");
            builder.Append(records.Count);
            builder.Append("/5: ");
            builder.Append(latest.result == OperationRecordResult.Held ? "버텨냄" : "거점붕괴");
            builder.Append(" | 공세");
            builder.Append(latest.reachedWave);

            if (latest.hasCoreHealthPercent)
            {
                builder.Append(" | 코어");
                builder.Append((latest.coreHealthPercent * 100f).ToString("0"));
                builder.Append('%');
            }

            builder.Append(" | ");
            builder.Append(FormatDuration(latest.survivalSeconds));

            if (latest.unitKillCount > 0)
            {
                builder.Append(" | 제거");
                builder.Append(latest.unitKillCount);
            }
            else if (latest.summonCount > 0)
            {
                builder.Append(" | 출격");
                builder.Append(latest.summonCount);
            }

            return builder.ToString();
        }

        private static string FormatDuration(float seconds)
        {
            var safeSeconds = seconds < 0f ? 0f : seconds;
            var minutes = (int)(safeSeconds / 60f);
            var remainingSeconds = (int)(safeSeconds % 60f);
            return $"{minutes:0}:{remainingSeconds:00}";
        }
    }

    public static class GarageOperationRecordServiceTagMapper
    {
        public static IReadOnlyDictionary<string, GarageUnitServiceTag> BuildByLoadoutKey(
            RecentOperationRecords records)
        {
            var result = new Dictionary<string, GarageUnitServiceTag>();
            if (records == null || records.Count <= 0)
                return result;

            var operationRecords = records.Records;
            for (var i = 0; i < operationRecords.Count; i++)
            {
                var record = operationRecords[i];
// csharp-guardrails: allow-null-defense
                if (record?.primaryRosterUnits == null)
                    continue;

                for (var unitIndex = 0; unitIndex < record.primaryRosterUnits.Count; unitIndex++)
                {
                    var loadoutKey = record.primaryRosterUnits[unitIndex];
                    if (string.IsNullOrWhiteSpace(loadoutKey) || result.ContainsKey(loadoutKey))
                        continue;

                    result.Add(loadoutKey, new GarageUnitServiceTag(
                        GarageUnitServiceTagKind.RecentOperationContributor,
                        0));
                }
            }

            return result;
        }
    }
}
