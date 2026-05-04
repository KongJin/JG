using System.Collections.Generic;

namespace Features.Garage.Presentation
{
    /// <summary>
    /// Page 렌더링에 필요한 모든 데이터를 캡슐화한 컨텍스트.
    /// Render() 메서드의 책임을 분리하여 단일 책임 원칙 준수.
    /// </summary>
    public sealed class GarageRenderContext
    {
        public GarageRenderContext(
            IReadOnlyList<GarageSlotViewModel> slotViewModels,
            GarageNovaPartsPanelViewModel partListViewModel,
            GarageEditorViewModel editorViewModel,
            GarageResultViewModel resultViewModel,
            GarageSetBUitkPageSnapshot snapshot,
            GarageDraftEvaluation evaluation)
        {
            SlotViewModels = slotViewModels;
            PartListViewModel = partListViewModel;
            EditorViewModel = editorViewModel;
            ResultViewModel = resultViewModel;
            Snapshot = snapshot;
            Evaluation = evaluation;
        }

        /// <summary>
        /// 슬롯 표시용 ViewModel 목록
        /// </summary>
        public IReadOnlyList<GarageSlotViewModel> SlotViewModels { get; }

        /// <summary>
        /// 부품 목록 ViewModel
        /// </summary>
        public GarageNovaPartsPanelViewModel PartListViewModel { get; }

        /// <summary>
        /// 에디터 상태 ViewModel
        /// </summary>
        public GarageEditorViewModel EditorViewModel { get; }

        /// <summary>
        /// 결과/검증 ViewModel
        /// </summary>
        public GarageResultViewModel ResultViewModel { get; }

        /// <summary>
        /// 렌더링 후 캡처된 스냅샷
        /// </summary>
        public GarageSetBUitkPageSnapshot Snapshot { get; }

        /// <summary>
        /// 드래프트 평가 결과
        /// </summary>
        public GarageDraftEvaluation Evaluation { get; }

        /// <summary>
        /// 저장 가능 여부
        /// </summary>
        public bool CanSave => ResultViewModel?.CanSave ?? false;

        /// <summary>
        /// 드래프트 변경사항 존재 여부
        /// </summary>
        public bool HasDraftChanges => Evaluation?.HasDraftChanges ?? false;

        /// <summary>
        /// 선택된 슬롯 인덱스
        /// </summary>
        public int SelectedSlotIndex => Snapshot?.SelectedSlotIndex ?? 0;

        /// <summary>
        /// 현재 포커스된 부품
        /// </summary>
        public GarageEditorFocus FocusedPart => Snapshot?.FocusedPart ?? GarageEditorFocus.Mobility;

        /// <summary>
        /// 저장 중인지 여부
        /// </summary>
        public bool IsSaving { get; set; }

        /// <summary>
        /// 빈 컨텍스트 (디폴트)
        /// </summary>
        public static GarageRenderContext Empty => new(
            slotViewModels: new List<GarageSlotViewModel>(),
            partListViewModel: GarageNovaPartsPanelViewModel.Empty,
            editorViewModel: new GarageEditorViewModel(
                title: string.Empty,
                subtitle: string.Empty,
                frameValueText: string.Empty,
                frameHintText: string.Empty,
                firepowerValueText: string.Empty,
                firepowerHintText: string.Empty,
                mobilityValueText: string.Empty,
                mobilityHintText: string.Empty,
                isClearInteractable: false),
            resultViewModel: new GarageResultViewModel(
                rosterStatusText: string.Empty,
                validationText: string.Empty,
                statsText: string.Empty,
                isReady: false,
                isDirty: false,
                canSave: false,
                primaryActionLabel: string.Empty),
            snapshot: GarageSetBUitkPageSnapshot.Empty,
            evaluation: GarageDraftEvaluation.Create(
                state: null,
                hasCatalogData: false,
                composeResult: Shared.Kernel.Result<Features.Unit.Domain.Unit>.Failure("No state"),
                rosterValidationResult: Shared.Kernel.Result.Success()));
    }
}
