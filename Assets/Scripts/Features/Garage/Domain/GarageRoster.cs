using System;
using System.Collections.Generic;

namespace Features.Garage.Domain
{
    /// <summary>
    /// 플레이어의 차고 편성. 3~6기 유닛 + 각각의 모듈 조합 저장.
    /// 네트워크 직렬화(JSON) 지원 — 순수 C#.
    /// </summary>
    [Serializable]
    public sealed class GarageRoster
    {
        /// <summary>
        /// 편성된 유닛 한 기의 데이터.
        /// </summary>
        [Serializable]
        public sealed class UnitLoadout
        {
            public string frameId;
            public string firepowerModuleId;
            public string mobilityModuleId;

            public UnitLoadout() { }

            public UnitLoadout(string frameId, string firepowerModuleId, string mobilityModuleId)
            {
                this.frameId = frameId;
                this.firepowerModuleId = firepowerModuleId;
                this.mobilityModuleId = mobilityModuleId;
            }
        }

        public List<UnitLoadout> loadout = new List<UnitLoadout>();

        /// <summary>
        /// 편성이 유효한가 (3~6기).
        /// </summary>
        public bool IsValid => loadout != null && loadout.Count >= 3 && loadout.Count <= 6;

        public int Count => loadout != null ? loadout.Count : 0;

        public GarageRoster() { }

        public GarageRoster(List<UnitLoadout> loadout)
        {
            this.loadout = loadout != null ? new List<UnitLoadout>(loadout) : new List<UnitLoadout>();
        }

        /// <summary>
        /// 유닛을 편성에 추가.
        /// </summary>
        public void AddUnit(UnitLoadout unit)
        {
            if (loadout == null) loadout = new List<UnitLoadout>();
            loadout.Add(unit);
        }

        /// <summary>
        /// 인덱스로 유닛 제거.
        /// </summary>
        public void RemoveUnitAt(int index)
        {
            if (loadout == null || index < 0 || index >= loadout.Count) return;
            loadout.RemoveAt(index);
        }

        /// <summary>
        /// 인덱스로 유닛 업데이트 (모듈 조합 변경).
        /// </summary>
        public void UpdateUnit(int index, UnitLoadout unit)
        {
            if (loadout == null || index < 0 || index >= loadout.Count) return;
            loadout[index] = unit;
        }

        /// <summary>
        /// 편성 초기화.
        /// </summary>
        public void Clear()
        {
            loadout?.Clear();
        }
    }
}
