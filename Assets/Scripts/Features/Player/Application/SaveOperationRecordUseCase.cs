using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Features.Player.Application.Ports;
using Features.Player.Domain;
using Shared.Kernel;

namespace Features.Player.Application
{
    public sealed class SaveOperationRecordUseCase
    {
        private readonly IOperationRecordStore _store;

        public SaveOperationRecordUseCase(IOperationRecordStore store)
        {
            _store = store;
        }

        public Result Execute(OperationRecord record)
        {
            if (record == null)
                return Result.Failure("Operation record is required.");

// csharp-guardrails: allow-null-defense
            if (_store == null)
                return Result.Failure("Operation record store is not configured.");

// csharp-guardrails: allow-null-defense
            var records = _store.Load() ?? new RecentOperationRecords();
            records.AddOrReplace(record);
            return _store.Save(records);
        }
    }

    public sealed class SyncOperationRecordsUseCase
    {
        private readonly IOperationRecordStore _localStore;
        private readonly IOperationRecordCloudPort _cloudPort;
        private readonly Action<string> _logWarning;

        public SyncOperationRecordsUseCase(
            IOperationRecordStore localStore,
            IOperationRecordCloudPort cloudPort,
            Action<string> logWarning = null)
        {
            _localStore = localStore;
            _cloudPort = cloudPort;
            _logWarning = logWarning;
        }

        public async Task<Result<RecentOperationRecords>> Execute()
        {
// csharp-guardrails: allow-null-defense
            var localRecords = _localStore?.Load() ?? new RecentOperationRecords();
            localRecords.Normalize();

// csharp-guardrails: allow-null-defense
            if (_cloudPort == null)
                return Result<RecentOperationRecords>.Success(localRecords);

            try
            {
                var cloudRecords = await _cloudPort.LoadOperationRecordsAsync();
                var mergedRecords = Merge(localRecords, cloudRecords);
// csharp-guardrails: allow-null-defense
                var localSave = _localStore?.Save(mergedRecords) ?? Result.Success();
                if (localSave.IsFailure)
                    _logWarning?.Invoke($"[OperationRecord] Local sync save failed: {localSave.Error}");

                await _cloudPort.SaveOperationRecordsAsync(mergedRecords);
                return Result<RecentOperationRecords>.Success(mergedRecords);
            }
            catch (Exception ex)
            {
                _logWarning?.Invoke($"[OperationRecord] Cloud sync failed: {ex.Message}");
                return Result<RecentOperationRecords>.Failure(ex.Message);
            }
        }

        internal static RecentOperationRecords Merge(
            RecentOperationRecords localRecords,
            RecentOperationRecords cloudRecords)
        {
            var candidatesById = new Dictionary<string, OperationRecord>(StringComparer.Ordinal);
            CollectNewest(candidatesById, cloudRecords);
            CollectNewest(candidatesById, localRecords);

            var mergedRecords = new RecentOperationRecords();
            foreach (var record in candidatesById.Values)
                mergedRecords.AddOrReplace(record);

            mergedRecords.Normalize();
            return mergedRecords;
        }

        private static void CollectNewest(
            Dictionary<string, OperationRecord> candidatesById,
            RecentOperationRecords source)
        {
            if (candidatesById == null || source == null)
                return;

            foreach (var record in source.Records)
            {
// csharp-guardrails: allow-null-defense
                if (record == null)
                    continue;

                var normalizedRecord = record.Clone();
                if (candidatesById.TryGetValue(normalizedRecord.operationId, out var existing) &&
                    existing.endedAtUnixMs >= normalizedRecord.endedAtUnixMs)
                {
                    continue;
                }

                candidatesById[normalizedRecord.operationId] = normalizedRecord;
            }
        }
    }
}
