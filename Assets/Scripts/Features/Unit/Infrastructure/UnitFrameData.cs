using UnityEngine;

namespace Features.Unit.Infrastructure
{
    public enum AssemblyForm
    {
        Unspecified,
        Tower,
        Shoulder,
        Humanoid
    }

    public static class UnitPartCompatibility
    {
        public static bool AreAssemblyFormsCompatible(AssemblyForm frameForm, AssemblyForm firepowerForm)
        {
            return frameForm == AssemblyForm.Unspecified ||
                   firepowerForm == AssemblyForm.Unspecified ||
                   frameForm == firepowerForm;
        }
    }

    /// <summary>
    /// 유닛 프레임 데이터 (ScriptableObject).
    /// 프레임의 기본 스탯 + 고유 특성 + 프리팹 참조.
    /// </summary>
    [CreateAssetMenu(fileName = "NewUnitFrame", menuName = "Unit/UnitFrame")]
    public sealed class UnitFrameData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string frameId;
        [SerializeField] private string displayName;
        [SerializeField] private Sprite icon;

        [Header("Assembly")]
        [SerializeField] private AssemblyForm assemblyForm;

        [Header("Base Stats")]
        [SerializeField] private float baseHp;
        [SerializeField] private float baseMoveRange;
        [SerializeField] private float baseAttackSpeed;

        [Header("Passive Trait")]
        [SerializeField] private PassiveTraitData passiveTrait;

        [Header("Presentation")]
        [SerializeField] private GameObject unitPrefab;
        [SerializeField] private GameObject previewPrefab;

        public string FrameId => frameId;
        public string DisplayName => displayName;
        public Sprite Icon => icon;
        public AssemblyForm AssemblyForm => assemblyForm;
        public float BaseHp => baseHp;
        public float BaseMoveRange => baseMoveRange;
        public float BaseAttackSpeed => baseAttackSpeed;
        public PassiveTraitData PassiveTrait => passiveTrait;
        public GameObject UnitPrefab => unitPrefab;
        public GameObject PreviewPrefab => previewPrefab;
    }
}
