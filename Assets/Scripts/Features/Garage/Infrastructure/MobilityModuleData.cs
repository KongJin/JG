using UnityEngine;

namespace Features.Garage.Infrastructure
{
    /// <summary>
    /// 기동 모듈 데이터 (ScriptableObject).
    /// 하단 슬롯: HP, 이동범위, 방어 결정.
    /// </summary>
    [CreateAssetMenu(fileName = "NewMobilityModule", menuName = "Garage/MobilityModule")]
    public sealed class MobilityModuleData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string moduleId;
        [SerializeField] private string displayName;
        [SerializeField] private Sprite icon;

        [Header("Defense Stats")]
        [SerializeField] private float hpBonus;
        [SerializeField] private float moveRange;

        [Header("Description")]
        [TextArea] [SerializeField] private string description;

        public string ModuleId => moduleId;
        public string DisplayName => displayName;
        public Sprite Icon => icon;
        public float HpBonus => hpBonus;
        public float MoveRange => moveRange;
        public string Description => description;
    }
}
