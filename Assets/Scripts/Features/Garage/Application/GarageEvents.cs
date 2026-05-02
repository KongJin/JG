namespace Features.Garage.Application
{
    /// <summary>
    /// 차고 편성 관련 도메인 이벤트.
    /// Application 레이어에서 정의 — Unity/Photon 의존성 없음.
    /// </summary>

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
}
