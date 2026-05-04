namespace Features.Garage.Presentation
{
    /// <summary>
    /// UI 표시용 데이터만 담는 전용 구조체.
    /// ViewModel과 분리하여 단일 책임 원칙 준수.
    /// </summary>
    public sealed class GarageSlotDisplayData
    {
        public GarageSlotDisplayData(
            string slotLabel,
            string title,
            string summary,
            string statusBadgeText,
            bool hasCommittedLoadout,
            bool hasDraftChanges,
            bool isEmpty,
            bool isSelected,
            bool showArrow = false,
            string callsign = null,
            string roleLabel = null,
            string serviceTagText = null)
        {
            SlotLabel = slotLabel;
            Title = title;
            Summary = summary;
            StatusBadgeText = statusBadgeText;
            HasCommittedLoadout = hasCommittedLoadout;
            HasDraftChanges = hasDraftChanges;
            IsEmpty = isEmpty;
            IsSelected = isSelected;
            ShowArrow = showArrow;
            Callsign = callsign;
            RoleLabel = roleLabel;
            ServiceTagText = serviceTagText;
        }

        public string SlotLabel { get; }
        public string Title { get; }
        public string Summary { get; }
        public string StatusBadgeText { get; }
        public bool HasCommittedLoadout { get; }
        public bool HasDraftChanges { get; }
        public bool IsEmpty { get; }
        public bool IsSelected { get; }
        public bool ShowArrow { get; }
        public string Callsign { get; }
        public string RoleLabel { get; }
        public string ServiceTagText { get; }

        /// <summary>
        /// 빈 Display 데이터 인스턴스
        /// </summary>
        public static GarageSlotDisplayData Empty => new(
            slotLabel: "A-01",
            title: string.Empty,
            summary: "빈 슬롯",
            statusBadgeText: string.Empty,
            hasCommittedLoadout: false,
            hasDraftChanges: false,
            isEmpty: true,
            isSelected: false,
            showArrow: false);
    }
}
