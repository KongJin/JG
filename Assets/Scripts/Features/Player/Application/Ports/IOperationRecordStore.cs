using System.Threading.Tasks;
using Features.Player.Domain;
using Shared.Kernel;

namespace Features.Player.Application.Ports
{
    public interface IOperationRecordStore
    {
        RecentOperationRecords Load();
        Result Save(RecentOperationRecords records);
    }

    public interface IOperationRecordCloudPort
    {
        Task<RecentOperationRecords> LoadOperationRecordsAsync();
        Task SaveOperationRecordsAsync(RecentOperationRecords records);
    }
}
