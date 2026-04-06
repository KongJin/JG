using UnityEngine;

namespace Features.Garage.Infrastructure
{
    /// <summary>
    /// 고유 특성 데이터 (ScriptableObject).
    /// 중단 슬롯: 프레임당 고정, 패시브 효과.
    /// </summary>
    [CreateAssetMenu(fileName = "NewPassiveTrait", menuName = "Garage/PassiveTrait")]
    public sealed class PassiveTraitData : ScriptableObject
    {
        public enum TraitStrength
        {
            Weak = 2,
            Medium = 5,
            Strong = 10
        }

        [Header("Identity")]
        [SerializeField] private string traitId;
        [SerializeField] private string displayName;

        [Header("Cost")]
        [SerializeField] private TraitStrength strength;

        [Header("Description")]
        [TextArea] [SerializeField] private string description;

        public string TraitId => traitId;
        public string DisplayName => displayName;
        public int CostBonus => (int)strength;
        public TraitStrength Strength => strength;
        public string Description => description;
    }
}
