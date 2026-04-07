using Shared.Kernel;

namespace Features.Unit.Domain
{
    /// <summary>
    /// 전투에서 실제 동작하는 유닛 엔티티.
    /// 프레임 + 모듈 조합 결과로 생성됨.
    /// 순수 C# — Unity/Photon 의존성 없음.
    /// </summary>
    public sealed class Unit
    {
        public DomainEntityId Id { get; }
        public string FrameId { get; }
        public string FirepowerModuleId { get; }
        public string MobilityModuleId { get; }

        // 조합 결과 스탯 (계산된 값)
        public float FinalHp { get; }
        public float FinalAttackDamage { get; }
        public float FinalAttackSpeed { get; }
        public float FinalRange { get; }
        public float FinalMoveRange { get; }
        public float FinalAnchorRange { get; }  // 앵커 반경 (기동 모듈이 결정)
        public int SummonCost { get; }
        public string PassiveTraitId { get; }
        public int PassiveTraitCostBonus { get; }

        public Unit(
            DomainEntityId id,
            string frameId,
            string firepowerModuleId,
            string mobilityModuleId,
            string passiveTraitId,
            int passiveTraitCostBonus,
            float finalHp,
            float finalAttackDamage,
            float finalAttackSpeed,
            float finalRange,
            float finalMoveRange,
            float finalAnchorRange,
            int summonCost)
        {
            Id = id;
            FrameId = frameId;
            FirepowerModuleId = firepowerModuleId;
            MobilityModuleId = mobilityModuleId;
            PassiveTraitId = passiveTraitId;
            PassiveTraitCostBonus = passiveTraitCostBonus;
            FinalHp = finalHp;
            FinalAttackDamage = finalAttackDamage;
            FinalAttackSpeed = finalAttackSpeed;
            FinalRange = finalRange;
            FinalMoveRange = finalMoveRange;
            FinalAnchorRange = finalAnchorRange;
            SummonCost = summonCost;
        }
    }
}
