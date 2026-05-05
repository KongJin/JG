using Features.Garage.Domain;
using Features.Garage.Application.Ports;
using Shared.Kernel;

namespace Features.Garage.Application
{
    /// <summary>
    /// 편성 유효성 검증 UseCase.
    /// 3~6기 체크 + 금지조합 검사.
    /// </summary>
    public sealed class ValidateRosterUseCase
    {
        private readonly IRosterValidationProvider _validationProvider;

        public ValidateRosterUseCase(IRosterValidationProvider validationProvider)
        {
            _validationProvider = validationProvider;
        }

        /// <summary>
        /// 편성 전체 유효성 검증.
        /// </summary>
        /// <param name="roster">검증할 편성</param>
        /// <param name="errorMessage">실패 시 오류 메시지</param>
        public Result Execute(GarageRoster roster, out string errorMessage)
        {
            errorMessage = null;

            if (roster == null)
            {
                errorMessage = "편성 데이터가 없습니다.";
                return Result.Failure(errorMessage);
            }

            if (!roster.IsValid)
            {
                errorMessage = $"편성 유닛 수는 3~6기여야 합니다. (현재: {roster.Count}기)";
                return Result.Failure(errorMessage);
            }

            return ValidateCompositions(roster, out errorMessage);
        }

        /// <summary>
        /// Garage draft 저장 검증.
        /// 저장은 진행 중인 편성을 보존할 수 있어야 하므로 전투 준비용 3~6기 제한과 분리한다.
        /// </summary>
        public Result ExecuteDraftSave(GarageRoster roster, out string errorMessage)
        {
            errorMessage = null;

            if (roster == null)
            {
                errorMessage = "편성 데이터가 없습니다.";
                return Result.Failure(errorMessage);
            }

            if (roster.Count <= 0)
            {
                errorMessage = "최소 1기 이상 조립해야 저장할 수 있습니다.";
                return Result.Failure(errorMessage);
            }

            return ValidateCompositions(roster, out errorMessage);
        }

        private Result ValidateCompositions(GarageRoster roster, out string errorMessage)
        {
            errorMessage = null;

            for (int i = 0; i < roster.loadout.Count; i++)
            {
                var unit = roster.loadout[i];
                // csharp-guardrails: allow-null-defense
                if (unit == null || !unit.HasAnySelection)
                    continue;

                if (!unit.IsComplete)
                {
                    errorMessage = $"슬롯 {i + 1}: 프레임과 모듈을 모두 선택해야 합니다.";
                    return Result.Failure(errorMessage);
                }

                bool isValid = _validationProvider.TryValidateComposition(
                    unit.frameId,
                    unit.firepowerModuleId,
                    unit.mobilityModuleId,
                    out string unitError);

                if (!isValid)
                {
                    errorMessage = $"슬롯 {i + 1}: {unitError}";
                    return Result.Failure(errorMessage);
                }
            }

            return Result.Success();
        }
    }
}