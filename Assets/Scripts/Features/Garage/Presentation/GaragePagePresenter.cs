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

        public IReadOnlyList<GarageSlotViewModel> BuildSlotViewModels(
            GaragePageState state,
            IReadOnlyDictionary<string, GarageUnitServiceTag> serviceTagsByLoadoutKey = null)
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
                string loadoutKey = hasDraftLoadout
                    ? GarageUnitIdentityFormatter.BuildLoadoutKey(
                        draft.frameId,
                        draft.firepowerModuleId,
                        draft.mobilityModuleId)
                    : null;
                var serviceTag = ResolveServiceTag(loadoutKey, serviceTagsByLoadoutKey);

                string title = GarageUnitIdentityFormatter.BuildEmptySlotTitle();
                string summary = "빈 슬롯";
                string statusBadgeText = GarageUnitIdentityFormatter.BuildEmptyStatusBadge();

                var frame = hasDraftLoadout ? Catalog?.FindFrame(draft.frameId) : null;
                var firepower = hasDraftLoadout ? Catalog?.FindFirepower(draft.firepowerModuleId) : null;
                var mobility = hasDraftLoadout ? Catalog?.FindMobility(draft.mobilityModuleId) : null;

                if (hasDraftLoadout)
                {
                    var frameName = frame?.DisplayName ?? draft.frameId;
                    title = GarageUnitIdentityFormatter.BuildTitle(i, frameName, hasDraftLoadout);
                    summary = GarageUnitIdentityFormatter.BuildSlotSummary(
                        firepower,
                        mobility,
                        draft.firepowerModuleId,
                        draft.mobilityModuleId);
                }
                else if (draft.HasAnySelection)
                {
                    title = "조립 중";
                    summary = "조립 중";
                }

                if (hasDraftChanges)
                {
                    statusBadgeText = GarageUnitIdentityFormatter.BuildDraftStatusBadge(hasDraftLoadout);
                    summary = hasDraftLoadout
                        ? GarageUnitIdentityFormatter.BuildSlotSummary(
                            firepower,
                            mobility,
                            draft.firepowerModuleId,
                            draft.mobilityModuleId)
                        : "조립 중";
                }
                else if (hasCommittedLoadout)
                {
                    statusBadgeText = GarageUnitIdentityFormatter.BuildActiveStatusBadge();
                }

                slotViewModels.Add(new GarageSlotViewModel(
                    GarageUnitIdentityFormatter.BuildSlotLabel(i, hasDraftLoadout),
                    title,
                    summary,
                    statusBadgeText,
                    hasCommittedLoadout,
                    hasDraftChanges,
                    isEmpty,
                    i == state.SelectedSlotIndex,
                    showArrow: i == state.SelectedSlotIndex,
                    callsign: hasDraftLoadout ? GarageUnitIdentityFormatter.BuildCallsign(i) : null,
                    roleLabel: hasDraftLoadout
                        ? GarageUnitIdentityFormatter.BuildRoleLabel(
                            firepower,
                            mobility)
                        : null,
                    serviceTagText: hasDraftLoadout
                        ? GarageUnitIdentityFormatter.BuildServiceTagText(serviceTag)
                        : null,
                    loadoutKey: loadoutKey,
                    frameId: hasDraftLoadout ? draft.frameId : null,
                    firepowerId: hasDraftLoadout ? draft.firepowerModuleId : null,
                    mobilityId: hasDraftLoadout ? draft.mobilityModuleId : null,
                    framePreviewPrefab: frame?.PreviewPrefab,
                    firepowerPreviewPrefab: firepower?.PreviewPrefab,
                    mobilityPreviewPrefab: mobility?.AssemblyPrefab ?? mobility?.PreviewPrefab,
                    frameAlignment: frame?.Alignment,
                    firepowerAlignment: firepower?.Alignment,
                    mobilityAlignment: mobility?.Alignment,
                    mobilityUsesAssemblyPivot: mobility?.UseAssemblyPivot ?? false));
            }

            return slotViewModels;
        }

        private static GarageUnitServiceTag ResolveServiceTag(
            string loadoutKey,
            IReadOnlyDictionary<string, GarageUnitServiceTag> serviceTagsByLoadoutKey)
        {
            if (string.IsNullOrWhiteSpace(loadoutKey) || serviceTagsByLoadoutKey == null)
                return GarageUnitServiceTag.Pending();

            return serviceTagsByLoadoutKey.TryGetValue(loadoutKey, out var tag)
                ? tag
                : GarageUnitServiceTag.Pending();
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
                title = $"{GarageUnitIdentityFormatter.BuildCallsign(state.SelectedSlotIndex)} {frame.DisplayName}";
                subtitle = hasCommittedUnit && !hasDraftChanges
                    ? GarageUnitIdentityFormatter.BuildServiceTagText(GarageUnitServiceTag.Pending())
                    : hasCommittedUnit
                        ? "저장 가능 | 전적 유지"
                        : "프레임부터 조립 시작";
            }
            else if (!hasCommittedUnit && !hasAnyDraftSelection)
            {
                title = GarageUnitIdentityFormatter.BuildSlotLabel(state.SelectedSlotIndex, hasLoadout: false);
                subtitle = "프레임부터 조립 시작";
            }
            else if (hasCommittedUnit && !hasDraftChanges)
            {
                title = GarageUnitIdentityFormatter.BuildCallsign(state.SelectedSlotIndex);
                subtitle = GarageUnitIdentityFormatter.BuildServiceTagText(GarageUnitServiceTag.Pending());
            }
            else if (hasCommittedUnit)
            {
                title = GarageUnitIdentityFormatter.BuildCallsign(state.SelectedSlotIndex);
                subtitle = "저장 가능 | 전적 유지";
            }
            else
            {
                title = GarageUnitIdentityFormatter.BuildSlotLabel(state.SelectedSlotIndex, hasLoadout: false);
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

        public GarageResultViewModel BuildResultViewModel(
            GaragePageState state,
            GarageDraftEvaluation evaluation,
            string operationSummary = null)
        {
            int missingUnits = state.CommittedRoster.Count >= 3 ? 0 : 3 - state.CommittedRoster.Count;
            bool readyEligible = state.CommittedRoster.IsValid && !evaluation.HasDraftChanges;
            string rosterStatusText = GarageUnitIdentityFormatter.BuildRosterStatusText(
                state.CommittedRoster.Count,
                missingUnits,
                readyEligible,
                evaluation.HasDraftChanges,
                evaluation.CanSave);

            return new GarageResultViewModel(
                rosterStatusText,
                BuildValidationText(state, evaluation),
                BuildStatsText(evaluation, operationSummary),
                isReady: readyEligible,
                isDirty: evaluation.HasDraftChanges,
                canSave: evaluation.CanSave,
                primaryActionLabel: GarageUnitIdentityFormatter.BuildPrimaryActionLabel(evaluation));
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

        private static string BuildStatsText(
            GarageDraftEvaluation evaluation,
            string operationSummary)
        {
            operationSummary = string.IsNullOrWhiteSpace(operationSummary)
                ? "최근 작전 기록 없음"
                : operationSummary;
            if (!evaluation.HasCompleteDraft)
                return operationSummary;

            if (!evaluation.HasCatalogData)
                return evaluation.ComposeError;

            if (!evaluation.HasComposedUnit)
                return evaluation.ComposeError;

            var unit = evaluation.ComposeResult.Value;
            return
                $"ATK {unit.FinalAttackDamage:0}  |  RNG {unit.FinalRange:0.0}m  |  COST {unit.SummonCost}\n" +
                $"HP {unit.FinalHp:0}  |  ASPD {unit.FinalAttackSpeed:0.00}  |  MOV {unit.FinalMoveRange:0.0}\n" +
                operationSummary;
        }

    }
}
