using Features.Garage.Domain;

namespace Features.Garage.Application
{
    public sealed class PublishGarageDraftStateUseCase
    {
        public GarageDraftStateSnapshot Build(GarageRoster committedRoster, bool hasUnsavedChanges)
        {
            if (hasUnsavedChanges)
                return new GarageDraftStateSnapshot(false, true, "Unsaved Garage changes");

            if (committedRoster == null || !committedRoster.IsValid)
            {
                int savedUnitCount = committedRoster?.Count ?? 0;
                int missingUnits = System.Math.Max(0, 3 - savedUnitCount);
                return new GarageDraftStateSnapshot(
                    false,
                    false,
                    missingUnits > 0
                        ? $"Need {missingUnits} more saved unit{(missingUnits == 1 ? string.Empty : "s")}"
                        : "Saved roster is not ready");
            }

            return new GarageDraftStateSnapshot(true, false, "Ready available");
        }
    }

    public readonly struct GarageDraftStateSnapshot
    {
        public bool ReadyEligible { get; }
        public bool HasUnsavedChanges { get; }
        public string BlockReason { get; }

        public GarageDraftStateSnapshot(bool readyEligible, bool hasUnsavedChanges, string blockReason)
        {
            ReadyEligible = readyEligible;
            HasUnsavedChanges = hasUnsavedChanges;
            BlockReason = blockReason;
        }
    }
}

