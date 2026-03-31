using System;
using Features.Player.Application;
using Features.Player.Application.Events;
using Features.Player.Domain;
using Shared.Attributes;
using Shared.EventBus;
using Shared.Kernel;
using UnityEngine;
using UnityEngine.UI;

namespace Features.Player.Presentation
{
    public sealed class DownedOverlayView : MonoBehaviour
    {
        [Required, SerializeField] private GameObject _overlayRoot;
        [Required, SerializeField] private Image _bleedoutBar;
        [Required, SerializeField] private Image _rescueChannelBar;
        [Required, SerializeField] private GameObject _rescueChannelGroup;

        private IEventSubscriber _eventBus;
        private DomainEntityId _localPlayerId;
        private BleedoutTracker _bleedoutTracker;
        private RescueChannelTracker _rescueChannelTracker;
        private bool _isDowned;
        private bool _rescueChannelActive;

        public void Initialize(
            IEventSubscriber eventBus,
            DomainEntityId localPlayerId,
            BleedoutTracker bleedoutTracker,
            RescueChannelTracker rescueChannelTracker)
        {
            _eventBus = eventBus;
            _localPlayerId = localPlayerId;
            _bleedoutTracker = bleedoutTracker;
            _rescueChannelTracker = rescueChannelTracker;

            _overlayRoot.SetActive(false);
            _rescueChannelGroup.SetActive(false);

            eventBus.Subscribe(this, new Action<PlayerDownedEvent>(OnDowned));
            eventBus.Subscribe(this, new Action<PlayerRescuedEvent>(OnRescued));
            eventBus.Subscribe(this, new Action<PlayerRespawnedEvent>(OnRespawned));
            eventBus.Subscribe(this, new Action<RescueChannelStartedEvent>(OnRescueChannelStarted));
            eventBus.Subscribe(this, new Action<RescueChannelCancelledEvent>(OnRescueChannelCancelled));
        }

        private void Update()
        {
            if (!_isDowned)
                return;

            _bleedoutBar.fillAmount = 1f - Mathf.Clamp01(_bleedoutTracker.Elapsed / BleedoutRule.Duration);

            if (_rescueChannelActive)
                _rescueChannelBar.fillAmount = Mathf.Clamp01(_rescueChannelTracker.Elapsed / RescueRule.ChannelDuration);
        }

        private void OnDowned(PlayerDownedEvent e)
        {
            if (e.PlayerId != _localPlayerId)
                return;

            _isDowned = true;
            _overlayRoot.SetActive(true);
            _rescueChannelGroup.SetActive(false);
            _rescueChannelActive = false;
        }

        private void OnRescued(PlayerRescuedEvent e)
        {
            if (e.RescuedId != _localPlayerId)
                return;

            Hide();
        }

        private void OnRespawned(PlayerRespawnedEvent e)
        {
            if (e.PlayerId != _localPlayerId)
                return;

            Hide();
        }

        private void OnRescueChannelStarted(RescueChannelStartedEvent e)
        {
            if (e.TargetId != _localPlayerId)
                return;

            _rescueChannelActive = true;
            _rescueChannelGroup.SetActive(true);
        }

        private void OnRescueChannelCancelled(RescueChannelCancelledEvent e)
        {
            if (e.TargetId != _localPlayerId)
                return;

            _rescueChannelActive = false;
            _rescueChannelGroup.SetActive(false);
        }

        private void Hide()
        {
            _isDowned = false;
            _rescueChannelActive = false;
            _overlayRoot.SetActive(false);
            _rescueChannelGroup.SetActive(false);
        }

        private void OnDestroy()
        {
            _eventBus?.UnsubscribeAll(this);
        }
    }
}
