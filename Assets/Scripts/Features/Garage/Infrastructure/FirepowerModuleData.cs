using UnityEngine;

namespace Features.Garage.Infrastructure
{
    /// <summary>
    /// 화력 모듈 데이터 (ScriptableObject).
    /// 상단 슬롯: 공격 방식, 사거리, DPS 결정.
    /// </summary>
    [CreateAssetMenu(fileName = "NewFirepowerModule", menuName = "Garage/FirepowerModule")]
    public sealed class FirepowerModuleData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string moduleId;
        [SerializeField] private string displayName;
        [SerializeField] private Sprite icon;

        [Header("Combat Stats")]
        [SerializeField] private float attackDamage;
        [SerializeField] private float attackSpeed;
        [SerializeField] private float range;

        [Header("Description")]
        [TextArea] [SerializeField] private string description;

        public string ModuleId => moduleId;
        public string DisplayName => displayName;
        public Sprite Icon => icon;
        public float AttackDamage => attackDamage;
        public float AttackSpeed => attackSpeed;
        public float Range => range;
        public string Description => description;
    }
}
