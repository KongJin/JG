using Features.Garage.Application;
using Features.Garage.Domain;
using Shared.EventBus;
using Shared.Lifecycle;
using UnityEngine;

namespace Features.Lobby.Presentation
{
    public sealed class LobbyGarageSummaryView : MonoBehaviour
    {
        private DisposableScope _disposables = new();

        public string StatusPillText { get; private set; } = "LOCKED";
        public string HeadlineText { get; private set; } = "No saved roster yet";
        public string BodyText { get; private set; } = "Save at least 3 units to unlock Ready.";

        public void Initialize(IEventSubscriber eventSubscriber)
        {
            _disposables.Dispose();
            _disposables = new DisposableScope();
            RenderLocked(0);

            if (eventSubscriber == null)
                return;

            _disposables.Add(EventBusSubscription.ForOwner(eventSubscriber, this));
            eventSubscriber.Subscribe<GarageInitializedEvent>(this, e => HandleRosterChanged(e.Roster));
            eventSubscriber.Subscribe<RosterSavedEvent>(this, e => HandleRosterChanged(e.Roster));
            eventSubscriber.Subscribe<GarageDraftStateChangedEvent>(this, HandleDraftStateChanged);
        }

        private void OnDestroy()
        {
            _disposables.Dispose();
        }

        private void HandleRosterChanged(GarageRoster roster)
        {
            if (roster != null && roster.IsValid)
            {
                StatusPillText = "SYNCED";
                HeadlineText = $"{roster.Count}/6 units synced";
                BodyText = "Ready is unlocked in the room panel.";
                return;
            }

            RenderLocked(roster?.Count ?? 0);
        }

        private void HandleDraftStateChanged(GarageDraftStateChangedEvent e)
        {
            if (e.HasUnsavedChanges)
            {
                StatusPillText = "DRAFT";
                HeadlineText = "Draft changes pending";
                BodyText = "Switch to Garage and save before entering a room.";
                return;
            }

            if (e.ReadyEligible)
            {
                StatusPillText = "SYNCED";
                HeadlineText = $"{e.SavedUnitCount}/6 units synced";
                BodyText = "Ready is unlocked in the room panel.";
                return;
            }

            RenderLocked(e.SavedUnitCount, e.BlockReason);
        }

        private void RenderLocked(int savedUnitCount, string blockReason = null)
        {
            StatusPillText = "LOCKED";
            HeadlineText = savedUnitCount <= 0 ? "No saved roster yet" : $"{savedUnitCount}/6 units saved";
            BodyText = string.IsNullOrWhiteSpace(blockReason)
                ? "Save at least 3 units to unlock Ready."
                : blockReason;
        }
    }
}
