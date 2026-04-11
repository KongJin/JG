using Features.Garage.Domain;
using Shared.Kernel;

namespace Features.Garage.Application
{
    /// <summary>
    /// 편성 유효성 검증 UseCase.
    /// 3~6기 체크 + 금지조합 검사.
    /// </summary>
    public sealed class ValidateRosterUseCase
    {
        /// <summary>
        /// 편성 검증용 조합 데이터 조회 포트.
        /// </summary>
        public interface IRosterValidationProvider
        {
            bool TryValidateComposition(
                string frameId,
                string firepowerModuleId,
                string mobilityModuleId,
                out string errorMessage);
        }

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

            // 각 유닛 조합 검증
            for (int i = 0; i < roster.loadout.Count; i++)
            {
                var unit = roster.loadout[i];
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
