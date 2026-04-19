using System.Threading.Tasks;

namespace Features.Account.Infrastructure
{
    public interface IAccountSessionAccess
    {
        string GetCurrentUid();
        Task<string> GetIdToken();
    }
}
