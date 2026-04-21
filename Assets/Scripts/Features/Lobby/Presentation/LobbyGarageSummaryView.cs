using Features.Garage.Application;
using Features.Garage.Domain;
using Shared.Attributes;
using Shared.EventBus;
using Shared.Lifecycle;
using TMPro;
using UnityEngine;

namespace Features.Lobby.Presentation
{
    public sealed class LobbyGarageSummaryView : MonoBehaviour
    {
        [Header("Copy")]
        [Required, SerializeField]
        private TMP_Text _statusPillText;

        [Required, SerializeField]
        private TMP_Text _headlineText;

        [Required, SerializeField]
        private TMP_Text _bodyText;

        [Header("Status Colors")]
        [SerializeField]
        private Color _syncedColor = new(0.42f, 0.80f, 0.62f, 1f);

        [SerializeField]
        private Color _draftColor = new(1f, 0.70f, 0.32f, 1f);

        [SerializeField]
        private Color _lockedColor = new(0.60f, 0.67f, 0.80f, 1f);

        private DisposableScope _disposables = new();
        private int _savedUnitCount;
        private bool _hasUnsavedChanges;
        private bool _readyEligible;
        private string _blockReason;

        public void Initialize(IEventSubscriber eventSubscriber)
        {
            _disposables.Dispose();
            _disposables = new DisposableScope();
            _savedUnitCount = 0;
            _hasUnsavedChanges = false;
            _readyEligible = false;
            _blockReason = "Save at least 3 units to unlock Ready.";
            Render();

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
            _savedUnitCount = roster?.Count ?? 0;

            if (_hasUnsavedChanges)
            {
                Render();
                return;
            }

            _readyEligible = roster != null && roster.IsValid;
            _blockReason = _readyEligible
                ? "Roster synced. Room panel can Ready now."
                : BuildLockedBody(_savedUnitCount);
            Render();
        }

        private void HandleDraftStateChanged(GarageDraftStateChangedEvent e)
        {
            _savedUnitCount = e.SavedUnitCount;
            _hasUnsavedChanges = e.HasUnsavedChanges;
            _readyEligible = e.ReadyEligible;
            _blockReason = string.IsNullOrWhiteSpace(e.BlockReason)
                ? BuildLockedBody(_savedUnitCount)
                : e.BlockReason;
            Render();
        }

        private void Render()
        {
            if (_statusPillText == null || _headlineText == null || _bodyText == null)
                return;

            if (_hasUnsavedChanges)
            {
                _statusPillText.text = "DRAFT";
                _statusPillText.color = _draftColor;
                _headlineText.text = "Draft changes pending";
                _bodyText.text = "Switch to Garage and save before entering a room.";
                return;
            }

            if (_readyEligible)
            {
                _statusPillText.text = "SYNCED";
                _statusPillText.color = _syncedColor;
                _headlineText.text = $"{_savedUnitCount}/6 units synced";
                _bodyText.text = "Ready is unlocked in the room panel.";
                return;
            }

            _statusPillText.text = "LOCKED";
            _statusPillText.color = _lockedColor;
            _headlineText.text = _savedUnitCount <= 0
                ? "No saved roster yet"
                : $"{_savedUnitCount}/6 units saved";
            _bodyText.text = BuildLockedBody(_savedUnitCount, _blockReason);
        }

        private static string BuildLockedBody(int savedUnitCount, string blockReason = null)
        {
            if (!string.IsNullOrWhiteSpace(blockReason) && blockReason != "Ready available")
                return blockReason;

            int missingUnits = Mathf.Max(0, 3 - savedUnitCount);
            return missingUnits <= 0
                ? "Switch to Garage to review the synced roster."
                : $"Save {missingUnits} more unit{(missingUnits == 1 ? string.Empty : "s")} to unlock Ready.";
        }
    }
}
