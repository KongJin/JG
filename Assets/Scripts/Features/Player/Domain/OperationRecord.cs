using System;
using System.Collections.Generic;

namespace Features.Player.Domain
{
    public enum OperationRecordResult
    {
        Held = 0,
        BaseCollapsed = 1
    }

    [Serializable]
    public sealed class OperationRecord
    {
        public const int CurrentSchemaVersion = 1;
        public const float UnknownCoreHealthPercent = -1f;

        public int schemaVersion = CurrentSchemaVersion;
        public string operationId;
        public long endedAtUnixMs;
        public OperationRecordResult result;
        public float survivalSeconds;
        public int reachedWave;
        public bool hasCoreHealthPercent;
        public float coreHealthPercent = UnknownCoreHealthPercent;
        public int summonCount;
        public int unitKillCount;
        public List<string> primaryRosterUnits = new();
        public string pressureSummaryKey;

        public OperationRecord Clone()
        {
            Normalize();
            return new OperationRecord
            {
                schemaVersion = schemaVersion,
                operationId = operationId,
                endedAtUnixMs = endedAtUnixMs,
                result = result,
                survivalSeconds = survivalSeconds,
                reachedWave = reachedWave,
                hasCoreHealthPercent = hasCoreHealthPercent,
                coreHealthPercent = coreHealthPercent,
                summonCount = summonCount,
                unitKillCount = unitKillCount,
                primaryRosterUnits = new List<string>(primaryRosterUnits),
                pressureSummaryKey = pressureSummaryKey
            };
        }

        public void Normalize()
        {
            schemaVersion = schemaVersion <= 0 ? CurrentSchemaVersion : schemaVersion;
            operationId = string.IsNullOrWhiteSpace(operationId)
                ? $"operation-{endedAtUnixMs}"
                : operationId.Trim();
            survivalSeconds = Math.Max(0f, survivalSeconds);
            reachedWave = Math.Max(0, reachedWave);
            summonCount = Math.Max(0, summonCount);
            unitKillCount = Math.Max(0, unitKillCount);
            coreHealthPercent = hasCoreHealthPercent
                ? Clamp01(coreHealthPercent)
                : UnknownCoreHealthPercent;
            primaryRosterUnits ??= new List<string>();
            for (var i = primaryRosterUnits.Count - 1; i >= 0; i--)
            {
                if (string.IsNullOrWhiteSpace(primaryRosterUnits[i]))
                    primaryRosterUnits.RemoveAt(i);
                else
                    primaryRosterUnits[i] = primaryRosterUnits[i].Trim();
            }

            pressureSummaryKey = string.IsNullOrWhiteSpace(pressureSummaryKey)
                ? "pressure.wave-reached"
                : pressureSummaryKey.Trim();
        }

        private static float Clamp01(float value)
        {
            if (value < 0f)
                return 0f;

            return value > 1f ? 1f : value;
        }
    }
}
