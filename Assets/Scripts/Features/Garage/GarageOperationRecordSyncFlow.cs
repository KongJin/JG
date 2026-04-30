using System;
using System.Threading.Tasks;
using Features.Player.Application;
using Features.Player.Application.Ports;
using Features.Player.Domain;
using Features.Player.Infrastructure;
using UnityEngine;

namespace Features.Garage
{
    internal static class GarageOperationRecordSyncFlow
    {
        public static async Task SyncAsync(
            OperationRecordJsonStore operationRecordStore,
            IOperationRecordCloudPort cloudPort,
            Action<RecentOperationRecords> applySyncedRecords)
        {
            if (operationRecordStore == null || cloudPort == null)
                return;

            var syncOperationRecords = new SyncOperationRecordsUseCase(
                operationRecordStore,
                cloudPort,
                Debug.LogWarning);
            var result = await syncOperationRecords.Execute();
            if (result.IsFailure)
                return;

            applySyncedRecords?.Invoke(result.Value);
        }
    }
}
