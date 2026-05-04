namespace Features.Garage.Application.Ports
{
    public interface IRosterValidationProvider
    {
        bool TryValidateComposition(
            string frameId,
            string firepowerModuleId,
            string mobilityModuleId,
            out string errorMessage);
    }
}
