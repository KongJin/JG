using Features.Unit.Domain;
using UnityEngine;

namespace Features.Garage.Presentation
{
    /// <summary>
    /// 프리뷰 렌더링에 필요한 데이터만 담는 전용 구조체.
    /// ViewModel과 분리하여 단일 책임 원칙 준수.
    /// </summary>
    public sealed class GarageSlotPreviewData
    {
        public GarageSlotPreviewData(
            string loadoutKey,
            string frameId,
            string firepowerId,
            string mobilityId,
            GameObject framePreviewPrefab,
            GameObject firepowerPreviewPrefab,
            GameObject mobilityPreviewPrefab,
            GaragePanelCatalog.PartAlignment frameAlignment,
            GaragePanelCatalog.PartAlignment firepowerAlignment,
            GaragePanelCatalog.PartAlignment mobilityAlignment,
            bool mobilityUsesAssemblyPivot,
            AssemblyForm frameAssemblyForm,
            AssemblyForm firepowerAssemblyForm)
        {
            LoadoutKey = loadoutKey;
            FrameId = frameId;
            FirepowerId = firepowerId;
            MobilityId = mobilityId;
            FramePreviewPrefab = framePreviewPrefab;
            FirepowerPreviewPrefab = firepowerPreviewPrefab;
            MobilityPreviewPrefab = mobilityPreviewPrefab;
            FrameAlignment = frameAlignment;
            FirepowerAlignment = firepowerAlignment;
            MobilityAlignment = mobilityAlignment;
            MobilityUsesAssemblyPivot = mobilityUsesAssemblyPivot;
            FrameAssemblyForm = frameAssemblyForm;
            FirepowerAssemblyForm = firepowerAssemblyForm;
        }

        public string LoadoutKey { get; }
        public string FrameId { get; }
        public string FirepowerId { get; }
        public string MobilityId { get; }

        // Preview 전용 프리팹
        public GameObject FramePreviewPrefab { get; }
        public GameObject FirepowerPreviewPrefab { get; }
        public GameObject MobilityPreviewPrefab { get; }

        // Assembly 정렬 데이터
        public GaragePanelCatalog.PartAlignment FrameAlignment { get; }
        public GaragePanelCatalog.PartAlignment FirepowerAlignment { get; }
        public GaragePanelCatalog.PartAlignment MobilityAlignment { get; }
        public bool MobilityUsesAssemblyPivot { get; }
        public AssemblyForm FrameAssemblyForm { get; }
        public AssemblyForm FirepowerAssemblyForm { get; }

        /// <summary>
        /// 완전한 로드아웃인지 확인 (Preview 생성 가능 여부)
        /// </summary>
        public bool HasCompleteLoadout =>
            !string.IsNullOrWhiteSpace(FrameId) &&
            !string.IsNullOrWhiteSpace(FirepowerId) &&
            !string.IsNullOrWhiteSpace(MobilityId) &&
            // csharp-guardrails: allow-null-defense
            FramePreviewPrefab != null &&
            // csharp-guardrails: allow-null-defense
            FirepowerPreviewPrefab != null &&
            // csharp-guardrails: allow-null-defense
            MobilityPreviewPrefab != null;

        /// <summary>
        /// 빈 Preview 데이터 인스턴스
        /// </summary>
        public static GarageSlotPreviewData Empty => new(
            loadoutKey: null,
            frameId: null,
            firepowerId: null,
            mobilityId: null,
            framePreviewPrefab: null,
            firepowerPreviewPrefab: null,
            mobilityPreviewPrefab: null,
            frameAlignment: null,
            firepowerAlignment: null,
            mobilityAlignment: null,
            mobilityUsesAssemblyPivot: false,
            frameAssemblyForm: AssemblyForm.Unspecified,
            firepowerAssemblyForm: AssemblyForm.Unspecified);
    }
}
