using Features.Garage.Domain;
using UnityEngine;

namespace Features.Garage.Presentation
{
    public sealed class GaragePageState
    {
        public GarageRoster CommittedRoster { get; private set; } = new GarageRoster();
        public GarageRoster DraftRoster { get; private set; } = new GarageRoster();

        public int SelectedSlotIndex { get; private set; }
        public string ValidationOverride { get; private set; }

        public void Initialize(GarageRoster roster)
        {
            CommittedRoster = roster ?? new GarageRoster();
            CommittedRoster.Normalize();
            DraftRoster = CommittedRoster.Clone();
            DraftRoster.Normalize();
            SelectSlot(FindInitialSelectedSlot());
        }

        public void SelectSlot(int slotIndex)
        {
            SelectedSlotIndex = Mathf.Clamp(slotIndex, 0, GarageRoster.MaxSlots - 1);
            ValidationOverride = null;
        }

        public GarageRoster.UnitLoadout GetSelectedCommittedSlot()
        {
            return CommittedRoster.GetSlot(SelectedSlotIndex);
        }

        public GarageRoster.UnitLoadout GetSelectedDraftSlot()
        {
            return DraftRoster.GetSlot(SelectedSlotIndex);
        }

        public string EditingFrameId => GetSelectedDraftSlot().frameId;
        public string EditingFirepowerId => GetSelectedDraftSlot().firepowerModuleId;
        public string EditingMobilityId => GetSelectedDraftSlot().mobilityModuleId;

        public void SetEditingFrameId(string frameId)
        {
            var slot = GetSelectedDraftSlot();
            SetSelectedDraftSlot(
                frameId,
                slot.firepowerModuleId,
                slot.mobilityModuleId);
        }

        public void SetEditingFirepowerId(string firepowerId)
        {
            var slot = GetSelectedDraftSlot();
            SetSelectedDraftSlot(
                slot.frameId,
                firepowerId,
                slot.mobilityModuleId);
        }

        public void SetEditingMobilityId(string mobilityId)
        {
            var slot = GetSelectedDraftSlot();
            SetSelectedDraftSlot(
                slot.frameId,
                slot.firepowerModuleId,
                mobilityId);
        }

        public void SetCommittedRoster(GarageRoster roster)
        {
            CommittedRoster = roster ?? new GarageRoster();
            CommittedRoster.Normalize();
            DraftRoster = CommittedRoster.Clone();
            DraftRoster.Normalize();
            SelectSlot(FindInitialSelectedSlot());
        }

        public void ClearSelectedSlotDraft()
        {
            DraftRoster.ClearSlot(SelectedSlotIndex);
            ValidationOverride = null;
        }

        public void CommitDraft()
        {
            CommittedRoster = DraftRoster.Clone();
            CommittedRoster.Normalize();
            DraftRoster = CommittedRoster.Clone();
            DraftRoster.Normalize();
            ValidationOverride = null;
        }

        public void SetValidationOverride(string message)
        {
            ValidationOverride = message;
        }

        public void ClearValidationOverride()
        {
            ValidationOverride = null;
        }

        public bool SelectedSlotHasCommittedLoadout()
        {
            return CommittedRoster.GetSlot(SelectedSlotIndex).IsComplete;
        }

        public bool SelectedSlotHasDraftLoadout()
        {
            return DraftRoster.GetSlot(SelectedSlotIndex).IsComplete;
        }

        public bool HasAnyDraftSelection()
        {
            return GetSelectedDraftSlot().HasAnySelection;
        }

        public bool HasCompleteDraft()
        {
            return GetSelectedDraftSlot().IsComplete;
        }

        public bool HasDraftChanges()
        {
            for (int i = 0; i < GarageRoster.MaxSlots; i++)
            {
                if (!SlotsEqual(CommittedRoster.GetSlot(i), DraftRoster.GetSlot(i)))
                    return true;
            }

            return false;
        }

        public bool SelectedSlotHasDraftChanges()
        {
            return !SlotsEqual(GetSelectedCommittedSlot(), GetSelectedDraftSlot());
        }

        public bool DraftMatchesCommittedSelection()
        {
            var slot = GetSelectedCommittedSlot();
            var draft = GetSelectedDraftSlot();
            return slot.IsComplete &&
                   slot.frameId == draft.frameId &&
                   slot.firepowerModuleId == draft.firepowerModuleId &&
                slot.mobilityModuleId == draft.mobilityModuleId;
        }

        public GarageRoster BuildSelectedSlotCommitRoster()
        {
            var commitRoster = CommittedRoster.Clone();
            commitRoster.SetSlot(SelectedSlotIndex, GetSelectedDraftSlot());
            return commitRoster;
        }

        public void CommitSelectedSlotDraft()
        {
            var selectedDraft = GetSelectedDraftSlot();
            CommittedRoster = CommittedRoster.Clone();
            CommittedRoster.SetSlot(SelectedSlotIndex, selectedDraft);
            CommittedRoster.Normalize();
            DraftRoster = CommittedRoster.Clone();
            DraftRoster.Normalize();
            ValidationOverride = null;
        }

        public int DraftUnitCount => DraftRoster.Count;

        private void SetSelectedDraftSlot(string frameId, string firepowerModuleId, string mobilityModuleId)
        {
            DraftRoster.SetSlot(SelectedSlotIndex, new GarageRoster.UnitLoadout(
                frameId,
                firepowerModuleId,
                mobilityModuleId));
        }

        private static bool SlotsEqual(GarageRoster.UnitLoadout left, GarageRoster.UnitLoadout right)
        {
            left ??= new GarageRoster.UnitLoadout();
            right ??= new GarageRoster.UnitLoadout();

            return left.frameId == right.frameId &&
                   left.firepowerModuleId == right.firepowerModuleId &&
                   left.mobilityModuleId == right.mobilityModuleId;
        }

        private int FindInitialSelectedSlot()
        {
            for (int i = 0; i < GarageRoster.MaxSlots; i++)
            {
                if (CommittedRoster.GetSlot(i).IsComplete)
                    return i;
            }

            for (int i = 0; i < GarageRoster.MaxSlots; i++)
            {
                if (DraftRoster.GetSlot(i).HasAnySelection)
                    return i;
            }

            return 0;
        }
    }
}
