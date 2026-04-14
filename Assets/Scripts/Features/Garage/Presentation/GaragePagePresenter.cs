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
                var loadout = state.CommittedRoster.GetSlot(i);
                bool hasCommittedLoadout = loadout.IsComplete;

                string title = "Empty Slot";
                string summary = $"Slot {i + 1} | Empty";

                if (hasCommittedLoadout)
                {
                    title = Catalog?.FindFrame(loadout.frameId)?.DisplayName ?? loadout.frameId;
                    var firepowerName = Catalog?.FindFirepower(loadout.firepowerModuleId)?.DisplayName ?? loadout.firepowerModuleId;
                    summary = $"{title} | {firepowerName}";
                }

                slotViewModels.Add(new GarageSlotViewModel(
                    $"SLOT {i + 1}",
                    title,
                    summary,
                    hasCommittedLoadout,
                    i == state.SelectedSlotIndex,
                    showArrow: i == state.SelectedSlotIndex,
                    frameId: hasCommittedLoadout ? loadout.frameId : null,
                    firepowerId: hasCommittedLoadout ? loadout.firepowerModuleId : null,
                    mobilityId: hasCommittedLoadout ? loadout.mobilityModuleId : null));
            }

            return slotViewModels;
        }

        public GarageEditorViewModel BuildEditorViewModel(GaragePageState state)
        {
            bool hasCommittedUnit = state.SelectedSlotHasCommittedLoadout();
            bool hasAnyDraftSelection = state.HasAnyDraftSelection();
            bool hasDraftChanges = state.HasDraftChanges();
            string title;
            string subtitle;

            if (!hasCommittedUnit && !hasAnyDraftSelection)
            {
                title = $"Slot {state.SelectedSlotIndex + 1} Empty";
                subtitle = "Select parts below. Changes auto-save when valid.";
            }
            else if (hasCommittedUnit && !hasDraftChanges)
            {
                title = $"Slot {state.SelectedSlotIndex + 1} Loadout";
                subtitle = "Saved. Change any part to update — auto-saves when valid.";
            }
            else if (hasCommittedUnit)
            {
                title = $"Slot {state.SelectedSlotIndex + 1} Editing";
                subtitle = "Changes will replace the saved loadout. Complete all 3 parts to auto-save.";
            }
            else
            {
                title = $"Slot {state.SelectedSlotIndex + 1} Draft";
                subtitle = "Complete all 3 parts to save. Select Frame, Firepower, and Mobility.";
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
                    ? $"DMG {firepower.AttackDamage:0}  ASPD {firepower.AttackSpeed:0.00}  RNG {firepower.Range:0.0}"
                    : "Choose weapon",
                mobility != null ? mobility.DisplayName : "< Select Mobility >",
                mobility != null
                    ? $"HP +{mobility.HpBonus:0}  MOV {mobility.MoveRange:0.0}  ANC {mobility.AnchorRange:0.0}"
                    : "Choose mobility",
                hasCommittedUnit || hasAnyDraftSelection);
        }

        public GarageResultViewModel BuildResultViewModel(GaragePageState state, GarageDraftEvaluation evaluation)
        {
            int missingUnits = state.CommittedRoster.Count >= 3 ? 0 : 3 - state.CommittedRoster.Count;
            string rosterStatusText = state.CommittedRoster.IsValid
                ? $"Roster ready: {state.CommittedRoster.Count}/6 saved units. Lobby Ready can stay enabled."
                : $"Roster incomplete: {state.CommittedRoster.Count}/6 saved units. Add {missingUnits} more for Ready.";

            return new GarageResultViewModel(
                rosterStatusText,
                BuildValidationText(state, evaluation),
                BuildStatsText(evaluation));
        }

        private static string BuildValidationText(GaragePageState state, GarageDraftEvaluation evaluation)
        {
            if (!string.IsNullOrWhiteSpace(state.ValidationOverride))
                return state.ValidationOverride;

            if (!evaluation.HasCompleteDraft)
                return "Select frame, firepower, and mobility to save this slot.";

            if (!evaluation.HasCatalogData)
                return evaluation.ComposeError;

            if (!evaluation.HasComposedUnit)
                return evaluation.ComposeError;

            if (evaluation.MatchesCommittedSelection)
            {
                return state.CommittedRoster.IsValid
                    ? "Saved. Valid roster is synced to local storage and Photon."
                    : "Saved. Slot committed, but Ready still needs at least 3 saved units.";
            }

            return "Draft is valid.";
        }

        private static string BuildStatsText(GarageDraftEvaluation evaluation)
        {
            if (!evaluation.HasCompleteDraft)
                return "Pick all three parts to see composed HP, damage, role, and summon cost.";

            if (!evaluation.HasCatalogData)
                return evaluation.ComposeError;

            if (!evaluation.HasComposedUnit)
                return evaluation.ComposeError;

            var unit = evaluation.ComposeResult.Value;
            return
                $"Cost {unit.SummonCost}  |  Trait Bonus {unit.PassiveTraitCostBonus}\n" +
                $"HP {unit.FinalHp:0}  |  DMG {unit.FinalAttackDamage:0}  |  ASPD {unit.FinalAttackSpeed:0.00}\n" +
                $"Range {unit.FinalRange:0.0}  |  Move {unit.FinalMoveRange:0.0}  |  Anchor {unit.FinalAnchorRange:0.0}";
        }
    }
}
