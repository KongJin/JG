using System.Threading.Tasks;
using Features.Garage.Domain;

namespace Features.Garage.Application.Ports
{
    public interface ICloudGaragePort
    {
        Task SaveGarageAsync(GarageRoster roster);
    }
}
