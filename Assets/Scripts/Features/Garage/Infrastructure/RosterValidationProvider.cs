using Features.Garage.Application;
using Features.Unit.Domain;
using Features.Unit.Infrastructure;

namespace Features.Garage.Infrastructure
{
    /// <summary>
    /// ScriptableObject 카탈로그를 기반으로 조합 유효성 검증 제공.
    /// ValidateRosterUseCase의 의존성 구현.
    /// </summary>
    public sealed class RosterValidationProvider : ValidateRosterUseCase.IRosterValidationProvider
    {
        private readonly ModuleCatalog _catalog;

        public RosterValidationProvider(ModuleCatalog catalog)
        {
            _catalog = catalog;
        }

        public bool TryValidateComposition(
            string frameId,
            string firepowerModuleId,
            string mobilityModuleId,
            out string errorMessage)
        {
            var firepower = _catalog.GetFirepowerModule(firepowerModuleId);
            var mobility = _catalog.GetMobilityModule(mobilityModuleId);

            if (firepower == null || mobility == null)
            {
                errorMessage = "모듈 데이터를 찾을 수 없습니다.";
                return false;
            }

            return UnitComposition.Validate(
                mobility.MoveRange,
                firepower.Range,
                out errorMessage);
        }
    }
}
