using Features.Unit.Domain;
using UnityEngine;

namespace Features.Garage.Presentation
{
    public sealed class GarageSlotViewModel
    {
        /// <summary>
        /// UI 표시용 데이터
        /// </summary>
        public GarageSlotDisplayData Display { get; }

        /// <summary>
        /// Preview 렌더링용 데이터
        /// </summary>
        public GarageSlotPreviewData Preview { get; }

        // 하위 호환성을 위한 프로퍼티 (Display/Preview 위임)
        public string SlotLabel => Display.SlotLabel;
        public string Title => Display.Title;
        public string Summary => Display.Summary;
        public string StatusBadgeText => Display.StatusBadgeText;
        public bool HasCommittedLoadout => Display.HasCommittedLoadout;
        public bool HasDraftChanges => Display.HasDraftChanges;
        public bool IsEmpty => Display.IsEmpty;
        public bool IsSelected => Display.IsSelected;
        public bool ShowArrow => Display.ShowArrow;
        public string Callsign => Display.Callsign;
        public string RoleLabel => Display.RoleLabel;
        public string ServiceTagText => Display.ServiceTagText;
        public string LoadoutKey => Preview.LoadoutKey;
        public string FrameId => Preview.FrameId;
        public string FirepowerId => Preview.FirepowerId;
        public string MobilityId => Preview.MobilityId;
        public GameObject FramePreviewPrefab => Preview.FramePreviewPrefab;
        public GameObject FirepowerPreviewPrefab => Preview.FirepowerPreviewPrefab;
        public GameObject MobilityPreviewPrefab => Preview.MobilityPreviewPrefab;
        public GaragePanelCatalog.PartAlignment FrameAlignment => Preview.FrameAlignment;
        public GaragePanelCatalog.PartAlignment FirepowerAlignment => Preview.FirepowerAlignment;
        public GaragePanelCatalog.PartAlignment MobilityAlignment => Preview.MobilityAlignment;
        public bool MobilityUsesAssemblyPivot => Preview.MobilityUsesAssemblyPivot;
        public AssemblyForm FrameAssemblyForm => Preview.FrameAssemblyForm;
        public AssemblyForm FirepowerAssemblyForm => Preview.FirepowerAssemblyForm;

        /// <summary>
        /// 구조화된 데이터로부터 ViewModel 생성
        /// </summary>
        public GarageSlotViewModel(GarageSlotDisplayData display, GarageSlotPreviewData preview)
        {
            Display = display ?? GarageSlotDisplayData.Empty;
            Preview = preview ?? GarageSlotPreviewData.Empty;
        }
    }

    public sealed class GarageEditorViewModel
    {
        public GarageEditorViewModel(
            string title,
            string subtitle,
            string frameValueText,
            string frameHintText,
            string firepowerValueText,
            string firepowerHintText,
            string mobilityValueText,
            string mobilityHintText,
            bool isClearInteractable)
        {
            Title = title;
            Subtitle = subtitle;
            FrameValueText = frameValueText;
            FrameHintText = frameHintText;
            FirepowerValueText = firepowerValueText;
            FirepowerHintText = firepowerHintText;
            MobilityValueText = mobilityValueText;
            MobilityHintText = mobilityHintText;
            IsClearInteractable = isClearInteractable;
        }

        public string Title { get; }
        public string Subtitle { get; }
        public string FrameValueText { get; }
        public string FrameHintText { get; }
        public string FirepowerValueText { get; }
        public string FirepowerHintText { get; }
        public string MobilityValueText { get; }
        public string MobilityHintText { get; }
        public bool IsClearInteractable { get; }
    }

    public sealed class GarageResultViewModel
    {
        public GarageResultViewModel(
            string rosterStatusText,
            string validationText,
            string statsText,
            bool isReady,
            bool isDirty,
            bool canSave,
            string primaryActionLabel,
            GarageStatRadarViewModel radar = null)
        {
            RosterStatusText = rosterStatusText;
            ValidationText = validationText;
            StatsText = statsText;
            IsReady = isReady;
            IsDirty = isDirty;
            CanSave = canSave;
            PrimaryActionLabel = primaryActionLabel;
            Radar = radar;
        }

        public string RosterStatusText { get; }
        public string ValidationText { get; }
        public string StatsText { get; }
        public bool IsReady { get; }
        public bool IsDirty { get; }
        public bool CanSave { get; }
        public string PrimaryActionLabel { get; }
        public GarageStatRadarViewModel Radar { get; }
    }

    public sealed class GarageStatRadarViewModel
    {
        public static readonly string[] AxisLabels =
        {
            "공격력",
            "공격속도",
            "사거리",
            "HP",
            "방어",
            "이동속도",
            "이동범위"
        };

        public GarageStatRadarViewModel(
            float[] currentValues,
            float[] previousValues,
            int summonCost)
        {
            CurrentValues = currentValues ?? new float[AxisLabels.Length];
            PreviousValues = previousValues;
            SummonCost = summonCost;
        }

        public float[] CurrentValues { get; }
        public float[] PreviousValues { get; }
        public int SummonCost { get; }
        public bool HasPrevious => PreviousValues != null && PreviousValues.Length == AxisLabels.Length;
    }
}
