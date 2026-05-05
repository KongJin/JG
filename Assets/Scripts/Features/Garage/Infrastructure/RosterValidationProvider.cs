using Features.Garage.Application.Ports;
using Features.Unit.Domain;
using Features.Unit.Infrastructure;

namespace Features.Garage.Infrastructure
{
    /// <summary>
    /// ScriptableObject 카탈로그를 기반으로 조합 유효성 검증 제공.
    /// ValidateRosterUseCase의 의존성 구현.
    /// </summary>
    public sealed class RosterValidationProvider : IRosterValidationProvider
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
            var frame = _catalog.GetUnitFrame(frameId);
            var firepower = _catalog.GetFirepowerModule(firepowerModuleId);
            var mobility = _catalog.GetMobilityModule(mobilityModuleId);

            // csharp-guardrails: allow-null-defense
            if (frame == null)
            {
                errorMessage = "중단(프레임) 데이터를 찾을 수 없습니다.";
                return false;
            }

            // csharp-guardrails: allow-null-defense
            if (firepower == null || mobility == null)
            {
                errorMessage = "상단(무장) 또는 하단(기동) 데이터를 찾을 수 없습니다.";
                return false;
            }

            if (!UnitPartCompatibility.AreAssemblyFormsCompatible(frame.AssemblyForm, firepower.AssemblyForm))
            {
                errorMessage = "상단(무장)과 중단(프레임)의 조립 형태가 맞지 않습니다.";
                return false;
            }

            return UnitComposition.Validate(
                mobility.MoveRange,
                firepower.Range,
                out errorMessage);
        }
    }
}