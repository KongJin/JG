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

                string title = "Empty Hangar";
                string summary = "Empty  |  Choose frame, weapon, and mobility";
                string statusBadgeText = "EMPTY";

                if (hasDraftLoadout)
                {
                    title = Catalog?.FindFrame(draft.frameId)?.DisplayName ?? draft.frameId;
                    var firepowerName = CompactPartName(Catalog?.FindFirepower(draft.firepowerModuleId)?.DisplayName ?? draft.firepowerModuleId);
                    var mobilityName = CompactPartName(Catalog?.FindMobility(draft.mobilityModuleId)?.DisplayName ?? draft.mobilityModuleId);
                    summary = $"Saved  |  {firepowerName} / {mobilityName}";
                }
                else if (draft.HasAnySelection)
                {
                    title = "Draft In Progress";
                    summary = "Draft  |  Finish all three parts before saving";
                }

                if (hasDraftChanges)
                {
                    statusBadgeText = hasDraftLoadout ? "UNSAVED" : "DIRTY";
                    summary = hasDraftLoadout
                        ? $"Unsaved  |  {CompactPartName(Catalog?.FindFirepower(draft.firepowerModuleId)?.DisplayName ?? draft.firepowerModuleId)} / {CompactPartName(Catalog?.FindMobility(draft.mobilityModuleId)?.DisplayName ?? draft.mobilityModuleId)}"
                        : "Draft  |  Finish all three parts before saving";
                }
                else if (hasCommittedLoadout)
                {
                    statusBadgeText = "SAVED";
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
                title = $"Slot {state.SelectedSlotIndex + 1} Hangar";
                subtitle = "Build a new unit. Choose a frame, weapon, and mobility kit.";
            }
            else if (hasCommittedUnit && !hasDraftChanges)
            {
                title = $"Slot {state.SelectedSlotIndex + 1} Saved Loadout";
                subtitle = "This slot is battle-ready. Change parts to create an unsaved draft.";
            }
            else if (hasCommittedUnit)
            {
                title = $"Slot {state.SelectedSlotIndex + 1} Draft Update";
                subtitle = "Review the draft, then save to replace the current loadout.";
            }
            else
            {
                title = $"Slot {state.SelectedSlotIndex + 1} Draft";
                subtitle = "Finish this draft, then save it into your roster.";
            }

            var frame = Catalog?.FindFrame(state.EditingFrameId);
            var firepower = Catalog?.FindFirepower(state.EditingFirepowerId);
            var mobility = Catalog?.FindMobility(state.EditingMobilityId);

            return new GarageEditorViewModel(
                title,
                subtitle,
                frame != null ? frame.DisplayName : "< Select Frame >",
                frame != null
                    ? $"HP {frame.BaseHp:0}  |  ASPD {frame.BaseAttackSpeed:0.00}"
                    : "Choose chassis",
                firepower != null ? firepower.DisplayName : "< Select Firepower >",
                firepower != null
                    ? $"DMG {firepower.AttackDamage:0}  |  ASPD {firepower.AttackSpeed:0.00}  |  RNG {firepower.Range:0.0}"
                    : "Choose the main weapon",
                mobility != null ? mobility.DisplayName : "< Select Mobility >",
                mobility != null
                    ? $"HP +{mobility.HpBonus:0}  |  MOV {mobility.MoveRange:0.0}  |  ANC {mobility.AnchorRange:0.0}"
                    : "Choose the movement kit",
                hasCommittedUnit || hasAnyDraftSelection);
        }

        public GarageResultViewModel BuildResultViewModel(GaragePageState state, GarageDraftEvaluation evaluation)
        {
            int missingUnits = state.CommittedRoster.Count >= 3 ? 0 : 3 - state.CommittedRoster.Count;
            bool readyEligible = state.CommittedRoster.IsValid && !evaluation.HasDraftChanges;
            string rosterStatusText;
            if (readyEligible)
            {
                rosterStatusText = $"Ready unlocked  |  {state.CommittedRoster.Count}/6 saved units synced";
            }
            else if (evaluation.HasDraftChanges)
            {
                rosterStatusText = "Unsaved draft active  |  Save this slot to unlock Ready";
            }
            else
            {
                rosterStatusText = $"Ready locked  |  {missingUnits} more saved unit{(missingUnits == 1 ? string.Empty : "s")} needed";
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
                    ? "Room panel can Ready now."
                    : "Save at least 3 units to keep Ready enabled.";
            }

            if (!evaluation.HasCompleteDraft)
                return "Finish frame, weapon, and mobility before saving.";

            if (!evaluation.HasCatalogData)
                return evaluation.ComposeError;

            if (!evaluation.HasComposedUnit)
                return evaluation.ComposeError;

            if (!evaluation.RosterValidationResult.IsSuccess)
                return evaluation.RosterValidationError;

            if (evaluation.MatchesCommittedSelection)
                return "Draft already matches the saved loadout.";

            return "Draft ready. Save to sync this loadout.";
        }

        private static string BuildStatsText(GarageDraftEvaluation evaluation)
        {
            if (!evaluation.HasCompleteDraft)
                return "Pick all three parts to preview combat stats and deployment cost.";

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
                return "Save Roster";

            if (!evaluation.HasDraftChanges)
                return "Roster Saved";

            return "Complete Draft";
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
