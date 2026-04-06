using Features.Garage.Domain;
using Shared.Kernel;

namespace Features.Garage.Application
{
    /// <summary>
    /// 유닛 조합 계산 UseCase.
    /// 프레임 + 모듈 조합의 유효성 검증 + 스탯 계산 + 비용 산출.
    /// </summary>
    public sealed class ComposeUnitUseCase
    {
        /// <summary>
        /// 조합 계산을 위한 데이터 조회 포트.
        /// Infrastructure 레이어에서 구현 (ScriptableObject 카탈로그 조회).
        /// </summary>
        public interface IUnitCompositionDataProvider
        {
            bool TryGetFrameData(string frameId, out UnitComposition.CompositionInput frameData);
        }

        private readonly IUnitCompositionDataProvider _dataProvider;

        public ComposeUnitUseCase(IUnitCompositionDataProvider dataProvider)
        {
            _dataProvider = dataProvider;
        }

        /// <summary>
        /// 조합 계산.
        /// </summary>
        /// <param name="frameId">프레임 ID</param>
        /// <param name="firepowerModuleId">화력 모듈 ID</param>
        /// <param name="mobilityModuleId">기동 모듈 ID</param>
        /// <param name="stats">계산된 조합 결과</param>
        /// <param name="summonCost">소환 비용</param>
        /// <param name="isValid">유효한 조합인지</param>
        /// <param name="errorMessage">오류 메시지</param>
        public Result Execute(
            string frameId,
            string firepowerModuleId,
            string mobilityModuleId,
            out UnitComposition.ComposedStats stats,
            out int summonCost,
            out bool isValid,
            out string errorMessage)
        {
            stats = default;
            summonCost = 0;
            isValid = false;
            errorMessage = null;

            if (!_dataProvider.TryGetFrameData(frameId, out var input))
            {
                errorMessage = $"프레임 데이터를 찾을 수 없습니다: {frameId}";
                return Result.Failure(errorMessage);
            }

            // 조합 유효성 검증
            isValid = UnitComposition.Validate(input.MobilityMoveRange, input.FirepowerRange, out errorMessage);
            if (!isValid)
            {
                return Result.Failure(errorMessage);
            }

            // 스탯 계산
            stats = UnitComposition.Compose(input);

            // 비용 계산
            summonCost = CostCalculator.Calculate(stats);

            return Result.Success();
        }
    }
}
