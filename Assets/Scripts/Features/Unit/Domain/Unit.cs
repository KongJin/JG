using System;
using Shared.Kernel;
using Shared.Math;

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
        public UnitPartIds PartIds { get; }
        public UnitCombatStats CombatStats { get; }
        public UnitEnergyCost EnergyCosts { get; }
        public string FrameId => PartIds.FrameId;
        public string DisplayName { get; }
        public string FirepowerModuleId => PartIds.FirepowerModuleId;
        public string MobilityModuleId => PartIds.MobilityModuleId;

        // 조합 결과 스탯 (계산된 값)
        public float FinalHp => CombatStats.Hp;
        public float FinalDefense => CombatStats.Defense;
        public float FinalAttackDamage => CombatStats.AttackDamage;
        public float FinalAttackSpeed => CombatStats.AttackSpeed;
        public float FinalRange => CombatStats.Range;
        public float FinalMoveSpeed => CombatStats.MoveSpeed;
        public float FinalMoveRange => CombatStats.MoveRange;
        public float FinalAnchorRange => CombatStats.AnchorRange;  // 앵커 반경 (기동 모듈이 결정)
        public int FrameEnergyCost => EnergyCosts.Frame;
        public int FirepowerEnergyCost => EnergyCosts.Firepower;
        public int MobilityEnergyCost => EnergyCosts.Mobility;
        public int SummonCost => EnergyCosts.Total;
        public string PassiveTraitId { get; }
        public int PassiveTraitCostBonus { get; }

        public Unit(
            DomainEntityId id,
            string frameId,
            string displayName,
            string firepowerModuleId,
            string mobilityModuleId,
            string passiveTraitId,
            int passiveTraitCostBonus,
            float finalHp,
            float finalDefense,
            float finalAttackDamage,
            float finalAttackSpeed,
            float finalRange,
            float finalMoveSpeed,
            float finalMoveRange,
            float finalAnchorRange,
            int frameEnergyCost,
            int firepowerEnergyCost,
            int mobilityEnergyCost,
            int summonCost)
            : this(
                id,
                new UnitPartIds(frameId, firepowerModuleId, mobilityModuleId),
                displayName,
                passiveTraitId,
                passiveTraitCostBonus,
                new UnitCombatStats(
                    finalHp,
                    finalDefense,
                    finalAttackDamage,
                    finalAttackSpeed,
                    finalRange,
                    finalMoveSpeed,
                    finalMoveRange,
                    finalAnchorRange),
                new UnitEnergyCost(frameEnergyCost, firepowerEnergyCost, mobilityEnergyCost, summonCost))
        {
        }

        public Unit(
            DomainEntityId id,
            UnitPartIds partIds,
            string displayName,
            string passiveTraitId,
            int passiveTraitCostBonus,
            UnitCombatStats combatStats,
            UnitEnergyCost energyCosts)
        {
            if (string.IsNullOrWhiteSpace(id.Value))
                throw new ArgumentException("Unit id cannot be empty.", nameof(id));

            Id = id;
            PartIds = partIds;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? Id.Value : displayName.Trim();
            PassiveTraitId = NormalizeId(passiveTraitId);
            PassiveTraitCostBonus = passiveTraitCostBonus;
            CombatStats = combatStats;
            EnergyCosts = energyCosts;
        }

        public bool HasCompleteLoadout =>
            !string.IsNullOrWhiteSpace(FrameId) &&
            !string.IsNullOrWhiteSpace(FirepowerModuleId) &&
            !string.IsNullOrWhiteSpace(MobilityModuleId);

        public bool CanBeSummonedWith(float availableEnergy)
        {
            return availableEnergy >= SummonCost;
        }

        public bool CanAttackFrom(Float3 origin, Float3 target)
        {
            return (target - origin).Magnitude <= FinalRange;
        }

        public bool IsWithinAnchorRange(Float3 anchorPosition, Float3 candidatePosition)
        {
            return FinalAnchorRange <= 0f || (candidatePosition - anchorPosition).Magnitude <= FinalAnchorRange;
        }

        private static string NormalizeId(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct UnitPartIds
    {
        public UnitPartIds(string frameId, string firepowerModuleId, string mobilityModuleId)
        {
            FrameId = NormalizeId(frameId);
            FirepowerModuleId = NormalizeId(firepowerModuleId);
            MobilityModuleId = NormalizeId(mobilityModuleId);
        }

        public string FrameId { get; }
        public string FirepowerModuleId { get; }
        public string MobilityModuleId { get; }
        public bool IsComplete =>
            !string.IsNullOrWhiteSpace(FrameId) &&
            !string.IsNullOrWhiteSpace(FirepowerModuleId) &&
            !string.IsNullOrWhiteSpace(MobilityModuleId);

        private static string NormalizeId(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct UnitCombatStats
    {
        public UnitCombatStats(
            float hp,
            float defense,
            float attackDamage,
            float attackSpeed,
            float range,
            float moveSpeed,
            float moveRange,
            float anchorRange)
        {
            if (hp <= 0f)
                throw new ArgumentOutOfRangeException(nameof(hp), hp, "Unit HP must be greater than zero.");
            if (defense < 0f)
                throw new ArgumentOutOfRangeException(nameof(defense), defense, "Unit defense cannot be negative.");
            if (attackDamage < 0f)
                throw new ArgumentOutOfRangeException(nameof(attackDamage), attackDamage, "Unit attack damage cannot be negative.");
            if (attackSpeed < 0f)
                throw new ArgumentOutOfRangeException(nameof(attackSpeed), attackSpeed, "Unit attack speed cannot be negative.");
            if (range < 0f)
                throw new ArgumentOutOfRangeException(nameof(range), range, "Unit attack range cannot be negative.");
            if (moveSpeed < 0f)
                throw new ArgumentOutOfRangeException(nameof(moveSpeed), moveSpeed, "Unit move speed cannot be negative.");
            if (moveRange < 0f)
                throw new ArgumentOutOfRangeException(nameof(moveRange), moveRange, "Unit move range cannot be negative.");
            if (anchorRange < 0f)
                throw new ArgumentOutOfRangeException(nameof(anchorRange), anchorRange, "Unit anchor range cannot be negative.");

            Hp = hp;
            Defense = defense;
            AttackDamage = attackDamage;
            AttackSpeed = attackSpeed;
            Range = range;
            MoveSpeed = moveSpeed;
            MoveRange = moveRange;
            AnchorRange = anchorRange;
        }

        public float Hp { get; }
        public float Defense { get; }
        public float AttackDamage { get; }
        public float AttackSpeed { get; }
        public float Range { get; }
        public float MoveSpeed { get; }
        public float MoveRange { get; }
        public float AnchorRange { get; }
    }

    public readonly struct UnitEnergyCost
    {
        public UnitEnergyCost(int frame, int firepower, int mobility, int total)
        {
            if (frame < 0)
                throw new ArgumentOutOfRangeException(nameof(frame), frame, "Unit frame energy cost cannot be negative.");
            if (firepower < 0)
                throw new ArgumentOutOfRangeException(nameof(firepower), firepower, "Unit firepower energy cost cannot be negative.");
            if (mobility < 0)
                throw new ArgumentOutOfRangeException(nameof(mobility), mobility, "Unit mobility energy cost cannot be negative.");
            if (total != frame + firepower + mobility)
                throw new ArgumentException("Unit summon cost must equal the sum of part energy costs.", nameof(total));

            Frame = frame;
            Firepower = firepower;
            Mobility = mobility;
            Total = total;
        }

        public int Frame { get; }
        public int Firepower { get; }
        public int Mobility { get; }
        public int Total { get; }
    }
}
