using System;
using System.Collections.Generic;

namespace Features.Player.Domain
{
    [Serializable]
    public sealed class RecentOperationRecords
    {
        public const int MaxRecords = 5;

        public List<OperationRecord> records = new();

        public int Count
        {
            get
            {
                Normalize();
                return records.Count;
            }
        }

        public IReadOnlyList<OperationRecord> Records
        {
            get
            {
                Normalize();
                return records;
            }
        }

        public void AddOrReplace(OperationRecord record)
        {
            if (record == null)
                return;

            record = record.Clone();
            record.Normalize();
            Normalize();

            var replaced = false;
            for (var i = 0; i < records.Count; i++)
            {
                if (!string.Equals(records[i].operationId, record.operationId, StringComparison.Ordinal))
                    continue;

                records[i] = record;
                replaced = true;
                break;
            }

            if (!replaced)
                records.Add(record);

            SortAndTrim();
        }

        public RecentOperationRecords Clone()
        {
            Normalize();
            var clone = new RecentOperationRecords();
            for (var i = 0; i < records.Count; i++)
                clone.records.Add(records[i].Clone());

            return clone;
        }

        public void Normalize()
        {
// csharp-guardrails: allow-null-defense
            records ??= new List<OperationRecord>();
            for (var i = records.Count - 1; i >= 0; i--)
            {
                if (records[i] == null)
                {
                    records.RemoveAt(i);
                    continue;
                }

                records[i].Normalize();
            }

            SortAndTrim();
        }

        private void SortAndTrim()
        {
            records.Sort((left, right) => right.endedAtUnixMs.CompareTo(left.endedAtUnixMs));
            while (records.Count > MaxRecords)
                records.RemoveAt(records.Count - 1);
        }
    }
}
