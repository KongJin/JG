using System;
using System.IO;
using Features.Player.Application.Ports;
using Features.Player.Domain;
using Shared.Kernel;
using UnityEngine;

namespace Features.Player.Infrastructure
{
    public sealed class OperationRecordJsonStore : IOperationRecordStore
    {
        private const string FileName = "recent_operation_records.json";
        private readonly string _path;

        public OperationRecordJsonStore()
            : this(Path.Combine(UnityEngine.Application.persistentDataPath, FileName))
        {
        }

        public OperationRecordJsonStore(string path)
        {
            _path = path;
        }

        public RecentOperationRecords Load()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_path) || !File.Exists(_path))
                    return new RecentOperationRecords();

                var json = File.ReadAllText(_path);
                var wrapper = JsonUtility.FromJson<OperationRecordWrapper>(json);
                var records = wrapper?.records ?? new RecentOperationRecords();
                records.Normalize();
                return records;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[OperationRecordJsonStore] Load failed: {ex.Message}");
                return new RecentOperationRecords();
            }
        }

        public Result Save(RecentOperationRecords records)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_path))
                    return Result.Failure("Operation record path is empty.");

                records ??= new RecentOperationRecords();
                records.Normalize();
                var directory = Path.GetDirectoryName(_path);
                if (!string.IsNullOrWhiteSpace(directory))
                    Directory.CreateDirectory(directory);

                var json = JsonUtility.ToJson(new OperationRecordWrapper { records = records }, true);
                File.WriteAllText(_path, json);
                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure($"Operation record save failed: {ex.Message}");
            }
        }

        [Serializable]
        private sealed class OperationRecordWrapper
        {
            public RecentOperationRecords records;
        }
    }
}
