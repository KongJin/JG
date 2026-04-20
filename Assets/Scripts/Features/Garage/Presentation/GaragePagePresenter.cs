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

                string title = "EMPTY";
                string summary = "빈 슬롯";
                string statusBadgeText = "EMPTY";

                if (hasDraftLoadout)
                {
                    title = Catalog?.FindFrame(draft.frameId)?.DisplayName ?? draft.frameId;
                    var firepowerName = CompactPartName(Catalog?.FindFirepower(draft.firepowerModuleId)?.DisplayName ?? draft.firepowerModuleId);
                    var mobilityName = CompactPartName(Catalog?.FindMobility(draft.mobilityModuleId)?.DisplayName ?? draft.mobilityModuleId);
                    summary = $"{firepowerName} | {mobilityName}";
                }
                else if (draft.HasAnySelection)
                {
                    title = "DRAFT";
                    summary = "조립 중";
                }

                if (hasDraftChanges)
                {
                    statusBadgeText = hasDraftLoadout ? "DRAFT" : "EDIT";
                    summary = hasDraftLoadout
                        ? $"{CompactPartName(Catalog?.FindFirepower(draft.firepowerModuleId)?.DisplayName ?? draft.firepowerModuleId)} | {CompactPartName(Catalog?.FindMobility(draft.mobilityModuleId)?.DisplayName ?? draft.mobilityModuleId)}"
                        : "조립 중";
                }
                else if (hasCommittedLoadout)
                {
                    statusBadgeText = "ACTIVE";
                }

                slotViewModels.Add(new GarageSlotViewModel(
                    $"UNIT {i + 1:00}",
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

            var frame = Catalog?.FindFrame(state.EditingFrameId);
            var firepower = Catalog?.FindFirepower(state.EditingFirepowerId);
            var mobility = Catalog?.FindMobility(state.EditingMobilityId);

            if (frame != null)
            {
                title = frame.DisplayName;
                subtitle = hasCommittedUnit && !hasDraftChanges
                    ? "저장본 검토 중"
                    : hasCommittedUnit
                        ? "저장 시 현재 슬롯 갱신"
                        : "프레임부터 조립 시작";
            }
            else if (!hasCommittedUnit && !hasAnyDraftSelection)
            {
                title = $"UNIT {state.SelectedSlotIndex + 1:00}";
                subtitle = "프레임부터 조립 시작";
            }
            else if (hasCommittedUnit && !hasDraftChanges)
            {
                title = $"UNIT {state.SelectedSlotIndex + 1:00}";
                subtitle = "저장본 검토 중";
            }
            else if (hasCommittedUnit)
            {
                title = $"UNIT {state.SelectedSlotIndex + 1:00}";
                subtitle = "저장 시 현재 슬롯 갱신";
            }
            else
            {
                title = $"UNIT {state.SelectedSlotIndex + 1:00}";
                subtitle = "세 파츠를 완성하면 저장 가능";
            }

            return new GarageEditorViewModel(
                title,
                subtitle,
                frame != null ? frame.DisplayName : "< 프레임 >",
                frame != null
                    ? $"HP {frame.BaseHp:0}  |  ASPD {frame.BaseAttackSpeed:0.00}"
                    : "차체를 선택하세요",
                firepower != null ? firepower.DisplayName : "< 무장 >",
                firepower != null
                    ? $"ATK {firepower.AttackDamage:0}  |  RNG {firepower.Range:0.0}"
                    : "주 무장을 선택하세요",
                mobility != null ? mobility.DisplayName : "< 기동 >",
                mobility != null
                    ? $"MOV {mobility.MoveRange:0.0}  |  ANC {mobility.AnchorRange:0.0}"
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
                rosterStatusText = $"SYNCED ROSTER  |  ACTIVE {state.CommittedRoster.Count}/6";
            }
            else if (evaluation.HasDraftChanges)
            {
                rosterStatusText = evaluation.CanSave
                    ? "SAVE REQUIRED  |  SLOT UPDATE READY"
                    : "DRAFT LOADOUT  |  SAVE BLOCKED";
            }
            else
            {
                rosterStatusText = $"ACTIVE {state.CommittedRoster.Count}/6  |  NEED +{missingUnits}";
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
                    ? "저장본이 최신입니다. 룸 패널에서 바로 준비할 수 있습니다."
                    : "최소 3기 이상 저장하면 준비 가능합니다.";
            }

            if (!evaluation.HasCompleteDraft)
                return "세 파츠를 모두 선택";

            if (!evaluation.HasCatalogData)
                return evaluation.ComposeError;

            if (!evaluation.HasComposedUnit)
                return evaluation.ComposeError;

            if (!evaluation.RosterValidationResult.IsSuccess)
                return evaluation.RosterValidationError;

            if (evaluation.MatchesCommittedSelection)
                return "현재 저장본과 동일합니다.";

            return "저장 시 선택 슬롯과 전체 편성이 동시에 갱신됩니다.";
        }

        private static string BuildStatsText(GarageDraftEvaluation evaluation)
        {
            if (!evaluation.HasCompleteDraft)
                return "세 파츠를 선택하면 전투 수치가 열립니다.";

            if (!evaluation.HasCatalogData)
                return evaluation.ComposeError;

            if (!evaluation.HasComposedUnit)
                return evaluation.ComposeError;

            var unit = evaluation.ComposeResult.Value;
            return
                $"ATK {unit.FinalAttackDamage:0}  |  RNG {unit.FinalRange:0.0}m  |  COST {unit.SummonCost}\n" +
                $"HP {unit.FinalHp:0}  |  ASPD {unit.FinalAttackSpeed:0.00}  |  MOV {unit.FinalMoveRange:0.0}";
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
