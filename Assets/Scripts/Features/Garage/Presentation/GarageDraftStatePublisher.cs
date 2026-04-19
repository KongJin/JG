using UnityEngine;

namespace Features.Garage.Presentation
{
    internal sealed class GarageDraftStatePublisher
    {
        public GarageDraftState Build(GaragePageState state)
        {
            if (state.HasDraftChanges())
                return new GarageDraftState(false, true, "Unsaved Garage changes");

            if (!state.CommittedRoster.IsValid)
            {
                int missingUnits = Mathf.Max(0, 3 - state.CommittedRoster.Count);
                return new GarageDraftState(
                    false,
                    false,
                    missingUnits > 0
                        ? $"Need {missingUnits} more saved unit{(missingUnits == 1 ? string.Empty : "s")}"
                        : "Saved roster is not ready");
            }

            return new GarageDraftState(true, false, "Ready available");
        }
    }

    internal readonly struct GarageDraftState
    {
        public bool ReadyEligible { get; }
        public bool HasUnsavedChanges { get; }
        public string BlockReason { get; }

        public GarageDraftState(bool readyEligible, bool hasUnsavedChanges, string blockReason)
        {
            ReadyEligible = readyEligible;
            HasUnsavedChanges = hasUnsavedChanges;
            BlockReason = blockReason;
        }
    }
}
