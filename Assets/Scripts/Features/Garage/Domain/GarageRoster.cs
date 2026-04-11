using System;
using System.Collections.Generic;
using System.Linq;

namespace Features.Garage.Domain
{
    /// <summary>
    /// 플레이어의 차고 편성. 3~6기 유닛 + 각각의 모듈 조합 저장.
    /// 네트워크 직렬화(JSON) 지원 — 순수 C#.
    /// </summary>
    [Serializable]
    public sealed class GarageRoster
    {
        public const int MaxSlots = 6;

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

            public bool HasAnySelection =>
                !string.IsNullOrWhiteSpace(frameId) ||
                !string.IsNullOrWhiteSpace(firepowerModuleId) ||
                !string.IsNullOrWhiteSpace(mobilityModuleId);

            public bool IsComplete =>
                !string.IsNullOrWhiteSpace(frameId) &&
                !string.IsNullOrWhiteSpace(firepowerModuleId) &&
                !string.IsNullOrWhiteSpace(mobilityModuleId);

            public UnitLoadout Clone() => new(frameId, firepowerModuleId, mobilityModuleId);
        }

        public List<UnitLoadout> loadout = new List<UnitLoadout>();

        /// <summary>
        /// 편성이 유효한가 (3~6기).
        /// </summary>
        public bool IsValid => Count >= 3 && Count <= MaxSlots;

        public int Count
        {
            get
            {
                Normalize();
                int count = 0;
                for (int i = 0; i < loadout.Count; i++)
                {
                    if (loadout[i]?.IsComplete == true)
                        count++;
                }

                return count;
            }
        }

        public GarageRoster()
        {
            Normalize();
        }

        public GarageRoster(List<UnitLoadout> loadout)
        {
            this.loadout = loadout != null ? new List<UnitLoadout>(loadout) : new List<UnitLoadout>();
            Normalize();
        }

        /// <summary>
        /// 직렬화 호환을 위해 슬롯 개수를 6칸으로 정규화한다.
        /// </summary>
        public void Normalize()
        {
            loadout ??= new List<UnitLoadout>();

            for (int i = 0; i < loadout.Count; i++)
                loadout[i] ??= new UnitLoadout();

            while (loadout.Count < MaxSlots)
                loadout.Add(new UnitLoadout());

            if (loadout.Count > MaxSlots)
                loadout = loadout.Take(MaxSlots).ToList();
        }

        /// <summary>
        /// 슬롯 조회.
        /// </summary>
        public UnitLoadout GetSlot(int index)
        {
            Normalize();
            if (index < 0 || index >= MaxSlots)
                return new UnitLoadout();

            return loadout[index] ?? new UnitLoadout();
        }

        /// <summary>
        /// 슬롯 저장/갱신.
        /// </summary>
        public void SetSlot(int index, UnitLoadout unit)
        {
            Normalize();
            if (index < 0 || index >= MaxSlots)
                return;

            loadout[index] = unit?.Clone() ?? new UnitLoadout();
        }

        /// <summary>
        /// 슬롯 비우기.
        /// </summary>
        public void ClearSlot(int index)
        {
            Normalize();
            if (index < 0 || index >= MaxSlots)
                return;

            loadout[index] = new UnitLoadout();
        }

        /// <summary>
        /// 편성 초기화.
        /// </summary>
        public void Clear()
        {
            loadout = new List<UnitLoadout>();
            Normalize();
        }

        public GarageRoster Clone()
        {
            Normalize();

            var cloned = new List<UnitLoadout>(MaxSlots);
            for (int i = 0; i < MaxSlots; i++)
                cloned.Add(GetSlot(i).Clone());

            return new GarageRoster(cloned);
        }

        public UnitLoadout[] GetFilledLoadouts()
        {
            Normalize();

            var filled = new List<UnitLoadout>();
            for (int i = 0; i < MaxSlots; i++)
            {
                var slot = GetSlot(i);
                if (slot.IsComplete)
                    filled.Add(slot.Clone());
            }

            return filled.ToArray();
        }
    }
}
