using Features.Unit.Domain;
using UnityEngine;

namespace Features.Unit.Infrastructure
{
    [CreateAssetMenu(fileName = "UnitStatTuning", menuName = "Unit/UnitStatTuning")]
    public sealed class UnitStatTuningData : ScriptableObject
    {
        [Header("Energy Cost Weights")]
        [SerializeField] private float hpEnergyWeight = 0.02f;
        [SerializeField] private float defenseEnergyWeight = 2.0f;
        [SerializeField] private float attackDamageEnergyWeight = 0.5f;
        [SerializeField] private float attackSpeedEnergyWeight = 3.0f;
        [SerializeField] private float rangeEnergyWeight = 2.0f;
        [SerializeField] private float moveSpeedEnergyWeight = 3.0f;
        [SerializeField] private float moveRangeEnergyWeight = 1.5f;
        [SerializeField] private float dispersionPenaltyFactor = 0.3f;

        [Header("Radar Normalization Max")]
        [SerializeField] private float attackDamageRadarMax = 60f;
        [SerializeField] private float attackSpeedRadarMax = 1.5f;
        [SerializeField] private float rangeRadarMax = 8f;
        [SerializeField] private float hpRadarMax = 700f;
        [SerializeField] private float defenseRadarMax = 8f;
        [SerializeField] private float moveSpeedRadarMax = 5f;
        [SerializeField] private float moveRangeRadarMax = 6f;

        public CostCalculator.StatCostTuning ToCostTuning() => new(
            hpEnergyWeight,
            defenseEnergyWeight,
            attackDamageEnergyWeight,
            attackSpeedEnergyWeight,
            rangeEnergyWeight,
            moveSpeedEnergyWeight,
            moveRangeEnergyWeight,
            dispersionPenaltyFactor);

        public float AttackDamageRadarMax => attackDamageRadarMax;
        public float AttackSpeedRadarMax => attackSpeedRadarMax;
        public float RangeRadarMax => rangeRadarMax;
        public float HpRadarMax => hpRadarMax;
        public float DefenseRadarMax => defenseRadarMax;
        public float MoveSpeedRadarMax => moveSpeedRadarMax;
        public float MoveRangeRadarMax => moveRangeRadarMax;
    }
}
