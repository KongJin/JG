using Features.Garage.Domain;

namespace Features.Lobby.Application
{
    public sealed class LobbyReadyPolicy
    {
        public bool ComputeReadyEligible(GarageRoster roster, bool hasUnsavedChanges)
        {
            return roster != null && roster.IsValid && !hasUnsavedChanges;
        }

        public bool CanToggleReady(bool readyEligible, bool localIsReady)
        {
            return readyEligible || localIsReady;
        }

        public bool ShouldForceRelock(bool readyEligible, bool localIsReady, bool hasRoomMemberContext)
        {
            return !readyEligible && localIsReady && hasRoomMemberContext;
        }

        public string BuildRosterBlockReason(GarageRoster roster, bool hasUnsavedChanges, bool readyEligible)
        {
            if (hasUnsavedChanges)
                return "Unsaved Garage changes";

            if (readyEligible)
                return "Ready available";

            return roster != null && roster.Count > 0
                ? "Need at least 3 saved units"
                : "Need at least 3 saved units";
        }

        public string BuildDraftBlockReason(string eventBlockReason, bool hasUnsavedChanges, bool readyEligible)
        {
            if (hasUnsavedChanges)
                return "Unsaved Garage changes";

            if (readyEligible)
                return "Ready available";

            return string.IsNullOrWhiteSpace(eventBlockReason)
                ? "Need at least 3 saved units"
                : eventBlockReason;
        }

        public string BuildBlockReason(string currentReason)
        {
            return string.IsNullOrWhiteSpace(currentReason)
                ? "Ready requires a saved Garage roster."
                : currentReason;
        }

        public string BuildReadyButtonLabel(bool hasUnsavedChanges, string currentReason)
        {
            if (hasUnsavedChanges)
                return "Save Garage Draft";

            return string.IsNullOrWhiteSpace(currentReason)
                ? "Need 3 Saved Units"
                : currentReason;
        }
    }
}
