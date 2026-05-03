using Features.Unit.Domain;
using UnityEngine;

namespace Features.Unit.Infrastructure
{
    /// <summary>
    /// 화력 모듈 데이터 (ScriptableObject).
    /// 상단 슬롯: 공격 방식, 사거리, DPS 결정.
    /// </summary>
    [CreateAssetMenu(fileName = "NewFirepowerModule", menuName = "Unit/FirepowerModule")]
    public sealed class FirepowerModuleData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string moduleId;
        [SerializeField] private string displayName;
        [SerializeField] private Sprite icon;

        [Header("Assembly")]
        [SerializeField] private AssemblyForm assemblyForm;

        [Header("Combat Stats")]
        [SerializeField] private float attackDamage;
        [SerializeField] private float attackSpeed;
        [SerializeField] private float range;

        [Header("Description")]
        [TextArea] [SerializeField] private string description;

        [Header("Presentation")]
        [SerializeField] private GameObject previewPrefab;

        public string ModuleId => moduleId;
        public string DisplayName => displayName;
        public Sprite Icon => icon;
        public AssemblyForm AssemblyForm => assemblyForm;
        public float AttackDamage => attackDamage;
        public float AttackSpeed => attackSpeed;
        public float Range => range;
        public string Description => description;
        public GameObject PreviewPrefab => previewPrefab;
    }
}
