using System.Collections.Generic;
using UnityEngine;

namespace Features.Unit.Infrastructure
{
    /// <summary>
    /// 유닛 모듈 카탈로그.
    /// 모든 프레임/모듈/특성 ScriptableObject 자산을 한 곳에서 조회.
    /// </summary>
    [CreateAssetMenu(fileName = "ModuleCatalog", menuName = "Unit/ModuleCatalog")]
    public sealed class ModuleCatalog : ScriptableObject
    {
        [Header("Unit Frames")]
        [SerializeField] private List<UnitFrameData> unitFrames = new List<UnitFrameData>();

        [Header("Firepower Modules")]
        [SerializeField] private List<FirepowerModuleData> firepowerModules = new List<FirepowerModuleData>();

        [Header("Mobility Modules")]
        [SerializeField] private List<MobilityModuleData> mobilityModules = new List<MobilityModuleData>();

        public IReadOnlyList<UnitFrameData> UnitFrames => unitFrames;
        public IReadOnlyList<FirepowerModuleData> FirepowerModules => firepowerModules;
        public IReadOnlyList<MobilityModuleData> MobilityModules => mobilityModules;

        public FirepowerModuleData GetFirepowerModule(string id) =>
            firepowerModules.Find(m => m.ModuleId == id);

        public MobilityModuleData GetMobilityModule(string id) =>
            mobilityModules.Find(m => m.ModuleId == id);

        public UnitFrameData GetUnitFrame(string id) =>
            unitFrames.Find(f => f.FrameId == id);
    }
}
