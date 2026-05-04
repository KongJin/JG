using UnityEngine;
using UnityEngine.Serialization;

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
    /// 하단 슬롯: 이동속도, 이동범위 결정.
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

        [Header("Movement Stats")]
        [FormerlySerializedAs("hpBonus")]
        [SerializeField, HideInInspector] private float legacyHpBonus;
        [SerializeField] private float moveSpeed;
        [SerializeField] private float moveRange;
        [FormerlySerializedAs("anchorRange")]
        [SerializeField, HideInInspector] private float legacyAnchorRange;

        [Header("Description")]
        [TextArea] [SerializeField] private string description;

        [Header("Presentation")]
        [SerializeField] private GameObject previewPrefab;

        public string ModuleId => moduleId;
        public string DisplayName => displayName;
        public Sprite Icon => icon;
        public MobilitySurface MobilitySurface => mobilitySurface;
        public float MoveSpeed => moveSpeed;
        public float MoveRange => moveRange;
        public string Description => description;
        public GameObject PreviewPrefab => previewPrefab;
    }
}
