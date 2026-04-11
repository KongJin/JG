using Features.Garage.Domain;
using UnityEngine;

namespace Features.Garage.Presentation
{
    public sealed class GaragePageState
    {
        public GarageRoster CommittedRoster { get; private set; } = new GarageRoster();

        public int SelectedSlotIndex { get; private set; }
        public string EditingFrameId { get; private set; }
        public string EditingFirepowerId { get; private set; }
        public string EditingMobilityId { get; private set; }
        public string ValidationOverride { get; private set; }

        public void Initialize(GarageRoster roster)
        {
            CommittedRoster = roster ?? new GarageRoster();
            CommittedRoster.Normalize();
            SelectSlot(0);
        }

        public void SelectSlot(int slotIndex)
        {
            SelectedSlotIndex = Mathf.Clamp(slotIndex, 0, GarageRoster.MaxSlots - 1);

            var slot = CommittedRoster.GetSlot(SelectedSlotIndex);
            EditingFrameId = slot.frameId;
            EditingFirepowerId = slot.firepowerModuleId;
            EditingMobilityId = slot.mobilityModuleId;
            ValidationOverride = null;
        }

        public void SetEditingFrameId(string frameId)
        {
            EditingFrameId = frameId;
        }

        public void SetEditingFirepowerId(string firepowerId)
        {
            EditingFirepowerId = firepowerId;
        }

        public void SetEditingMobilityId(string mobilityId)
        {
            EditingMobilityId = mobilityId;
        }

        public void SetCommittedRoster(GarageRoster roster)
        {
            CommittedRoster = roster ?? new GarageRoster();
            CommittedRoster.Normalize();
        }

        public void ClearSelectedSlotDraft()
        {
            EditingFrameId = null;
            EditingFirepowerId = null;
            EditingMobilityId = null;
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

        public bool HasAnyDraftSelection()
        {
            return !string.IsNullOrWhiteSpace(EditingFrameId) ||
                   !string.IsNullOrWhiteSpace(EditingFirepowerId) ||
                   !string.IsNullOrWhiteSpace(EditingMobilityId);
        }

        public bool HasCompleteDraft()
        {
            return !string.IsNullOrWhiteSpace(EditingFrameId) &&
                   !string.IsNullOrWhiteSpace(EditingFirepowerId) &&
                   !string.IsNullOrWhiteSpace(EditingMobilityId);
        }

        public bool HasDraftChanges()
        {
            var slot = CommittedRoster.GetSlot(SelectedSlotIndex);
            return slot.frameId != EditingFrameId ||
                   slot.firepowerModuleId != EditingFirepowerId ||
                   slot.mobilityModuleId != EditingMobilityId;
        }

        public bool DraftMatchesCommittedSelection()
        {
            var slot = CommittedRoster.GetSlot(SelectedSlotIndex);
            return slot.IsComplete &&
                   slot.frameId == EditingFrameId &&
                   slot.firepowerModuleId == EditingFirepowerId &&
                   slot.mobilityModuleId == EditingMobilityId;
        }
    }
}
