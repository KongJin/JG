using System.Threading.Tasks;
using Features.Garage.Domain;

namespace Features.Garage.Application.Ports
{
    public interface ICloudGarageLoadPort
    {
        Task<GarageRoster> LoadGarageAsync();
    }
}
