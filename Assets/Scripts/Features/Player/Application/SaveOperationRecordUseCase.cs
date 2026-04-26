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

            if (_store == null)
                return Result.Failure("Operation record store is not configured.");

            var records = _store.Load() ?? new RecentOperationRecords();
            records.AddOrReplace(record);
            return _store.Save(records);
        }
    }
}
