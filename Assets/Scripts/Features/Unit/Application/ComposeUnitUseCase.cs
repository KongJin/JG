using Features.Garage.Application.Ports;
using Features.Unit.Domain;
using Shared.Kernel;

namespace Features.Unit.Application
{
    /// <summary>
    /// 모듈 ID들로 유닛을 조합한다.
    /// Unit 도메인의 UnitComposition을 사용하며, SO 데이터 조회는 Port를 통해 수행한다.
    /// </summary>
    public sealed class ComposeUnitUseCase
    {
        private readonly IUnitCompositionPort _port;

        public ComposeUnitUseCase(IUnitCompositionPort port)
        {
            _port = port;
        }

        /// <summary>
        /// 조합 계산.
        /// </summary>
        /// <param name="unitId">유닛 엔티티 ID</param>
        /// <param name="frameId">프레임 ID</param>
        /// <param name="firepowerModuleId">화력 모듈 ID</param>
        /// <param name="mobilityModuleId">기동 모듈 ID</param>
        public Result<Domain.Unit> Execute(
            DomainEntityId unitId,
            string frameId,
            string firepowerModuleId,
            string mobilityModuleId)
        {
            var frameBase = _port.GetFrameBaseStats(frameId);
            var firepower = _port.GetFirepowerStats(firepowerModuleId);
            var mobility = _port.GetMobilityStats(mobilityModuleId);

            // 검증
            if (!UnitComposition.Validate(mobility.MoveRange, firepower.Range, out var error))
            {
                return Result<Domain.Unit>.Failure(error);
            }

            var passiveTraitId = _port.GetPassiveTraitId(frameId);
            var passiveCostBonus = _port.GetPassiveTraitCostBonus(frameId);

            // CompositionInput 구성
            var input = new UnitComposition.CompositionInput(
                baseHp: frameBase.HpBonus,
                baseAttackSpeed: frameBase.AttackSpeed,
                firepowerDamage: firepower.AttackDamage,
                firepowerAttackSpeed: firepower.AttackSpeed,
                firepowerRange: firepower.Range,
                mobilityHpBonus: mobility.HpBonus,
                mobilityMoveRange: mobility.MoveRange,
                mobilityAnchorRange: mobility.AnchorRange,
                passiveTraitCostBonus: passiveCostBonus);

            // 조합
            var composed = UnitComposition.Compose(
                frameId,
                firepowerModuleId,
                mobilityModuleId,
                input);

            return Result<Domain.Unit>.Success(composed.ToUnit(unitId));
        }
    }
}
