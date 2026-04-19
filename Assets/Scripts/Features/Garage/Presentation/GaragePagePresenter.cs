using System.Collections.Generic;

namespace Features.Garage.Presentation
{
    public sealed class GaragePagePresenter
    {
        public GaragePagePresenter(GaragePanelCatalog catalog)
        {
            Catalog = catalog;
        }

        private GaragePanelCatalog Catalog { get; }

        public IReadOnlyList<GarageSlotViewModel> BuildSlotViewModels(GaragePageState state)
        {
            var slotViewModels = new List<GarageSlotViewModel>(Domain.GarageRoster.MaxSlots);

            for (int i = 0; i < Domain.GarageRoster.MaxSlots; i++)
            {
                var committed = state.CommittedRoster.GetSlot(i);
                var draft = state.DraftRoster.GetSlot(i);
                bool hasCommittedLoadout = committed.IsComplete;
                bool hasDraftLoadout = draft.IsComplete;
                bool hasDraftChanges =
                    committed.frameId != draft.frameId ||
                    committed.firepowerModuleId != draft.firepowerModuleId ||
                    committed.mobilityModuleId != draft.mobilityModuleId;
                bool isEmpty = !draft.HasAnySelection;

                string title = "대기 슬롯";
                string summary = "프레임 / 무장 / 기동 선택";
                string statusBadgeText = "빈 슬롯";

                if (hasDraftLoadout)
                {
                    title = Catalog?.FindFrame(draft.frameId)?.DisplayName ?? draft.frameId;
                    var firepowerName = CompactPartName(Catalog?.FindFirepower(draft.firepowerModuleId)?.DisplayName ?? draft.firepowerModuleId);
                    var mobilityName = CompactPartName(Catalog?.FindMobility(draft.mobilityModuleId)?.DisplayName ?? draft.mobilityModuleId);
                    summary = $"저장됨  |  {firepowerName} / {mobilityName}";
                }
                else if (draft.HasAnySelection)
                {
                    title = "조립 중";
                    summary = "임시안  |  세 파츠 완성 필요";
                }

                if (hasDraftChanges)
                {
                    statusBadgeText = hasDraftLoadout ? "미저장" : "작성중";
                    summary = hasDraftLoadout
                        ? $"미저장  |  {CompactPartName(Catalog?.FindFirepower(draft.firepowerModuleId)?.DisplayName ?? draft.firepowerModuleId)} / {CompactPartName(Catalog?.FindMobility(draft.mobilityModuleId)?.DisplayName ?? draft.mobilityModuleId)}"
                        : "임시안  |  세 파츠 완성 필요";
                }
                else if (hasCommittedLoadout)
                {
                    statusBadgeText = "저장됨";
                }

                slotViewModels.Add(new GarageSlotViewModel(
                    $"SLOT {i + 1}",
                    title,
                    summary,
                    statusBadgeText,
                    hasCommittedLoadout,
                    hasDraftChanges,
                    isEmpty,
                    i == state.SelectedSlotIndex,
                    showArrow: i == state.SelectedSlotIndex,
                    frameId: hasDraftLoadout ? draft.frameId : null,
                    firepowerId: hasDraftLoadout ? draft.firepowerModuleId : null,
                    mobilityId: hasDraftLoadout ? draft.mobilityModuleId : null));
            }

            return slotViewModels;
        }

        public GarageEditorViewModel BuildEditorViewModel(GaragePageState state)
        {
            bool hasCommittedUnit = state.SelectedSlotHasCommittedLoadout();
            bool hasAnyDraftSelection = state.HasAnyDraftSelection();
            bool hasDraftChanges = state.SelectedSlotHasDraftChanges();
            string title;
            string subtitle;

            if (!hasCommittedUnit && !hasAnyDraftSelection)
            {
                title = $"슬롯 {state.SelectedSlotIndex + 1} 조립";
                subtitle = "이 슬롯의 프레임, 무장, 기동을 선택하세요.";
            }
            else if (hasCommittedUnit && !hasDraftChanges)
            {
                title = $"슬롯 {state.SelectedSlotIndex + 1} 저장 완료";
                subtitle = "저장된 편성입니다. 부품을 바꾸면 새 임시안이 됩니다.";
            }
            else if (hasCommittedUnit)
            {
                title = $"슬롯 {state.SelectedSlotIndex + 1} 수정 중";
                subtitle = "현재 임시안을 검토하고 저장하면 기존 편성을 교체합니다.";
            }
            else
            {
                title = $"슬롯 {state.SelectedSlotIndex + 1} 임시안";
                subtitle = "세 파츠를 완성한 뒤 로스터에 저장하세요.";
            }

            var frame = Catalog?.FindFrame(state.EditingFrameId);
            var firepower = Catalog?.FindFirepower(state.EditingFirepowerId);
            var mobility = Catalog?.FindMobility(state.EditingMobilityId);

            return new GarageEditorViewModel(
                title,
                subtitle,
                frame != null ? frame.DisplayName : "< 프레임 선택 >",
                frame != null
                    ? $"HP {frame.BaseHp:0}  |  ASPD {frame.BaseAttackSpeed:0.00}"
                    : "프레임 차체를 선택하세요",
                firepower != null ? firepower.DisplayName : "< 무장 선택 >",
                firepower != null
                    ? $"DMG {firepower.AttackDamage:0}  |  ASPD {firepower.AttackSpeed:0.00}  |  RNG {firepower.Range:0.0}"
                    : "주 무장을 선택하세요",
                mobility != null ? mobility.DisplayName : "< 기동 선택 >",
                mobility != null
                    ? $"HP +{mobility.HpBonus:0}  |  MOV {mobility.MoveRange:0.0}  |  ANC {mobility.AnchorRange:0.0}"
                    : "기동 키트를 선택하세요",
                hasCommittedUnit || hasAnyDraftSelection);
        }

        public GarageResultViewModel BuildResultViewModel(GaragePageState state, GarageDraftEvaluation evaluation)
        {
            int missingUnits = state.CommittedRoster.Count >= 3 ? 0 : 3 - state.CommittedRoster.Count;
            bool readyEligible = state.CommittedRoster.IsValid && !evaluation.HasDraftChanges;
            string rosterStatusText;
            if (readyEligible)
            {
                rosterStatusText = $"출격 가능  |  저장된 유닛 {state.CommittedRoster.Count}/6";
            }
            else if (evaluation.HasDraftChanges)
            {
                rosterStatusText = "미저장 임시안 존재  |  저장 후 출격 가능";
            }
            else
            {
                rosterStatusText = $"출격 잠김  |  저장된 유닛 {missingUnits}기 더 필요";
            }

            return new GarageResultViewModel(
                rosterStatusText,
                BuildValidationText(state, evaluation),
                BuildStatsText(evaluation),
                isReady: readyEligible,
                isDirty: evaluation.HasDraftChanges,
                canSave: evaluation.CanSave,
                primaryActionLabel: BuildPrimaryActionLabel(evaluation));
        }

        private static string BuildValidationText(GaragePageState state, GarageDraftEvaluation evaluation)
        {
            if (!string.IsNullOrWhiteSpace(state.ValidationOverride))
                return state.ValidationOverride;

            if (!evaluation.HasDraftChanges)
            {
                return state.CommittedRoster.IsValid
                    ? "룸 패널에서 바로 출격할 수 있습니다."
                    : "최소 3기를 저장해야 출격할 수 있습니다.";
            }

            if (!evaluation.HasCompleteDraft)
                return "프레임, 무장, 기동을 모두 완성하세요.";

            if (!evaluation.HasCatalogData)
                return evaluation.ComposeError;

            if (!evaluation.HasComposedUnit)
                return evaluation.ComposeError;

            if (!evaluation.RosterValidationResult.IsSuccess)
                return evaluation.RosterValidationError;

            if (evaluation.MatchesCommittedSelection)
                return "임시안이 저장된 편성과 같습니다.";

            return "임시안 준비 완료. 저장하면 동기화됩니다.";
        }

        private static string BuildStatsText(GarageDraftEvaluation evaluation)
        {
            if (!evaluation.HasCompleteDraft)
                return "세 파츠를 모두 선택하면 전투 능력과 비용을 미리 볼 수 있습니다.";

            if (!evaluation.HasCatalogData)
                return evaluation.ComposeError;

            if (!evaluation.HasComposedUnit)
                return evaluation.ComposeError;

            var unit = evaluation.ComposeResult.Value;
            return
                $"HP {unit.FinalHp:0}  |  DMG {unit.FinalAttackDamage:0}  |  ASPD {unit.FinalAttackSpeed:0.00}\n" +
                $"Cost {unit.SummonCost}  |  Range {unit.FinalRange:0.0}  |  Move {unit.FinalMoveRange:0.0}\n" +
                $"Anchor {unit.FinalAnchorRange:0.0}  |  Trait {unit.PassiveTraitId}";
        }

        private static string BuildPrimaryActionLabel(GarageDraftEvaluation evaluation)
        {
            if (evaluation.CanSave)
                return "편성 저장";

            if (!evaluation.HasDraftChanges)
                return "저장됨";

            return "임시안 완성";
        }

        private static string CompactPartName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Unknown";

            int separatorIndex = value.IndexOf(' ');
            if (separatorIndex > 0)
                return value[..separatorIndex];

            return value;
        }
    }
}
