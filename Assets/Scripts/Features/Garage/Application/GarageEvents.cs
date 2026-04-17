using Features.Garage.Domain;
using Features.Unit.Domain;

namespace Features.Garage.Application
{
    /// <summary>
    /// 차고 편성 관련 도메인 이벤트.
    /// Application 레이어에서 정의 — Unity/Photon 의존성 없음.
    /// </summary>

    /// <summary>
    /// 차고가 초기화되었을 때.
    /// </summary>
    public readonly struct GarageInitializedEvent
    {
        public GarageRoster Roster { get; }
        public GarageInitializedEvent(GarageRoster roster) { Roster = roster; }
    }

    /// <summary>
    /// 유닛 조합이 계산되었을 때.
    /// </summary>
    public readonly struct UnitComposedEvent
    {
        public int SlotIndex { get; }
        public float Hp { get; }
        public float AttackDamage { get; }
        public float AttackSpeed { get; }
        public float Range { get; }
        public float MoveRange { get; }
        public float AnchorRange { get; }
        public int SummonCost { get; }
        public UnitRole Role { get; }
        public bool IsValid { get; }
        public string ErrorMessage { get; }

        public UnitComposedEvent(
            int slotIndex,
            float hp,
            float attackDamage,
            float attackSpeed,
            float range,
            float moveRange,
            float anchorRange,
            int summonCost,
            UnitRole role,
            bool isValid,
            string errorMessage)
        {
            SlotIndex = slotIndex;
            Hp = hp;
            AttackDamage = attackDamage;
            AttackSpeed = attackSpeed;
            Range = range;
            MoveRange = moveRange;
            AnchorRange = anchorRange;
            SummonCost = summonCost;
            Role = role;
            IsValid = isValid;
            ErrorMessage = errorMessage;
        }
    }

    /// <summary>
    /// 편성이 저장되었을 때.
    /// </summary>
    public readonly struct RosterSavedEvent
    {
        public GarageRoster Roster { get; }
        public RosterSavedEvent(GarageRoster roster) { Roster = roster; }
    }

    /// <summary>
    /// 편집 중인 Draft 상태가 바뀌었을 때.
    /// Ready 가능 여부는 저장된 편성과 unsaved 상태를 함께 반영한다.
    /// </summary>
    public readonly struct GarageDraftStateChangedEvent
    {
        public int SavedUnitCount { get; }
        public bool HasUnsavedChanges { get; }
        public bool ReadyEligible { get; }
        public string BlockReason { get; }

        public GarageDraftStateChangedEvent(int savedUnitCount, bool hasUnsavedChanges, bool readyEligible, string blockReason)
        {
            SavedUnitCount = savedUnitCount;
            HasUnsavedChanges = hasUnsavedChanges;
            ReadyEligible = readyEligible;
            BlockReason = blockReason;
        }
    }

    /// <summary>
    /// 편성 유효성 검증 결과.
    /// </summary>
    public readonly struct RosterValidatedEvent
    {
        public bool IsValid { get; }
        public string ErrorMessage { get; }
        public int UnitCount { get; }

        public RosterValidatedEvent(bool isValid, string errorMessage, int unitCount)
        {
            IsValid = isValid;
            ErrorMessage = errorMessage;
            UnitCount = unitCount;
        }
    }

    /// <summary>
    /// 편성 완료 토글 상태 변경.
    /// </summary>
    public readonly struct GarageReadyToggledEvent
    {
        public bool IsReady { get; }
        public GarageReadyToggledEvent(bool isReady) { IsReady = isReady; }
    }
}
