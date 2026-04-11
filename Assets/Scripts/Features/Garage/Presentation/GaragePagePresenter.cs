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
                string summary = "Select frame and modules";

                if (hasCommittedLoadout)
                {
                    title = Catalog?.FindFrame(loadout.frameId)?.DisplayName ?? loadout.frameId;
                    var firepowerName = Catalog?.FindFirepower(loadout.firepowerModuleId)?.DisplayName ?? loadout.firepowerModuleId;
                    var mobilityName = Catalog?.FindMobility(loadout.mobilityModuleId)?.DisplayName ?? loadout.mobilityModuleId;
                    summary = $"{firepowerName} | {mobilityName}";
                }

                slotViewModels.Add(new GarageSlotViewModel(
                    $"SLOT {i + 1}",
                    title,
                    summary,
                    hasCommittedLoadout,
                    i == state.SelectedSlotIndex));
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
                subtitle = "Build a loadout. Valid combinations save immediately.";
            }
            else if (hasCommittedUnit && !hasDraftChanges)
            {
                title = $"Slot {state.SelectedSlotIndex + 1} Loadout";
                subtitle = "Saved loadout. Adjust selectors to overwrite this slot automatically.";
            }
            else if (hasCommittedUnit)
            {
                title = $"Slot {state.SelectedSlotIndex + 1} Draft";
                subtitle = "Draft edits stay in the center/right panels until a valid combination replaces the saved slot.";
            }
            else
            {
                title = $"Slot {state.SelectedSlotIndex + 1} Draft";
                subtitle = "This slot is not saved yet. Complete a valid draft to commit it to the roster.";
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
