using Features.Garage.Domain;

namespace Features.Garage.Application.Ports
{
    public interface IRosterMigrationPort
    {
        GarageRoster Migrate(GarageRoster roster);
    }
}
