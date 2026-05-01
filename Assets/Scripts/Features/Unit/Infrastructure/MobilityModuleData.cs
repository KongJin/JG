using UnityEngine;

namespace Features.Unit.Infrastructure
{
    public enum MobilitySurface
    {
        Unspecified,
        Ground,
        Air
    }

    /// <summary>
    /// 기동 모듈 데이터 (ScriptableObject).
    /// 하단 슬롯: HP, 이동범위, 방어 결정.
    /// </summary>
    [CreateAssetMenu(fileName = "NewMobilityModule", menuName = "Unit/MobilityModule")]
    public sealed class MobilityModuleData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string moduleId;
        [SerializeField] private string displayName;
        [SerializeField] private Sprite icon;

        [Header("Assembly")]
        [SerializeField] private MobilitySurface mobilitySurface;

        [Header("Defense Stats")]
        [SerializeField] private float hpBonus;
        [SerializeField] private float moveRange;

        [Header("Anchor")]
        [SerializeField] private float anchorRange;  // 앵커 반경 (m) (전투 중 교전 가능 범위)

        [Header("Description")]
        [TextArea] [SerializeField] private string description;

        [Header("Presentation")]
        [SerializeField] private GameObject previewPrefab;

        public string ModuleId => moduleId;
        public string DisplayName => displayName;
        public Sprite Icon => icon;
        public MobilitySurface MobilitySurface => mobilitySurface;
        public float HpBonus => hpBonus;
        public float MoveRange => moveRange;
        public float AnchorRange => anchorRange;
        public string Description => description;
        public GameObject PreviewPrefab => previewPrefab;
    }
}
