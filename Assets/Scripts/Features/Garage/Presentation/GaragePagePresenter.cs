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
                string summary = "Select a frame, weapon, and mobility kit.";
                string statusBadgeText = "EMPTY";

                if (hasDraftLoadout)
                {
                    title = Catalog?.FindFrame(draft.frameId)?.DisplayName ?? draft.frameId;
                    var firepowerName = Catalog?.FindFirepower(draft.firepowerModuleId)?.DisplayName ?? draft.firepowerModuleId;
                    var mobilityName = Catalog?.FindMobility(draft.mobilityModuleId)?.DisplayName ?? draft.mobilityModuleId;
                    summary = $"{firepowerName} | {mobilityName}";
                }
                else if (draft.HasAnySelection)
                {
                    title = "Draft In Progress";
                    summary = "Finish all three parts before saving.";
                }

                if (hasDraftChanges)
                {
                    statusBadgeText = hasDraftLoadout ? "UNSAVED" : "DIRTY";
                }
                else if (hasCommittedLoadout)
                {
                    statusBadgeText = "SAVED";
                }

                slotViewModels.Add(new GarageSlotViewModel(
                    $"SLOT {i + 1}  {statusBadgeText}",
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
                    ? $"HP {frame.BaseHp:0}  ASPD {frame.BaseAttackSpeed:0.00}"
                    : "Choose chassis",
                firepower != null ? firepower.DisplayName : "< Select Firepower >",
                firepower != null
                    ? $"Weapon profile  DMG {firepower.AttackDamage:0}  ASPD {firepower.AttackSpeed:0.00}  RNG {firepower.Range:0.0}"
                    : "Choose the main weapon",
                mobility != null ? mobility.DisplayName : "< Select Mobility >",
                mobility != null
                    ? $"Mobility profile  HP +{mobility.HpBonus:0}  MOV {mobility.MoveRange:0.0}  ANC {mobility.AnchorRange:0.0}"
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
                rosterStatusText = $"Ready window open. {state.CommittedRoster.Count}/6 saved units are synced.";
            }
            else if (evaluation.HasDraftChanges)
            {
                rosterStatusText = $"Unsaved draft active. Ready is locked until you save this hangar change.";
            }
            else
            {
                rosterStatusText = $"Ready blocked. {state.CommittedRoster.Count}/6 saved units. Add {missingUnits} more saved units.";
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
                    ? "Saved roster is synced. You can Ready in the room panel."
                    : "You need at least 3 saved units before Ready can stay enabled.";
            }

            if (!evaluation.HasCompleteDraft)
                return "Draft incomplete. Finish all three parts before saving.";

            if (!evaluation.HasCatalogData)
                return evaluation.ComposeError;

            if (!evaluation.HasComposedUnit)
                return evaluation.ComposeError;

            if (!evaluation.RosterValidationResult.IsSuccess)
                return evaluation.RosterValidationError;

            if (evaluation.MatchesCommittedSelection)
                return "This draft matches the saved slot. No save needed.";

            return "Draft ready. Save to sync this loadout to Firestore and Photon.";
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
                $"Cost {unit.SummonCost}  |  Trait Bonus {unit.PassiveTraitCostBonus}  |  Trait {unit.PassiveTraitId}\n" +
                $"HP {unit.FinalHp:0}  |  DMG {unit.FinalAttackDamage:0}  |  ASPD {unit.FinalAttackSpeed:0.00}\n" +
                $"Range {unit.FinalRange:0.0}  |  Move {unit.FinalMoveRange:0.0}  |  Anchor {unit.FinalAnchorRange:0.0}";
        }

        private static string BuildPrimaryActionLabel(GarageDraftEvaluation evaluation)
        {
            if (evaluation.CanSave)
                return "Save Draft";

            if (!evaluation.HasDraftChanges)
                return "Saved";

            return "Invalid Draft";
        }
    }
}
