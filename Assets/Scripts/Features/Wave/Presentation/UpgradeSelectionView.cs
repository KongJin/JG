using System;
using Features.Status.Domain;
using Features.Wave.Application.Events;
using Shared.Attributes;
using Shared.EventBus;
using Shared.Kernel;
using UnityEngine;
using UnityEngine.UI;

namespace Features.Wave.Presentation
{
    public sealed class UpgradeSelectionView : MonoBehaviour
    {
        [Required, SerializeField] private GameObject panel;
        [Required, SerializeField] private Button countButton;
        [Required, SerializeField] private Button rangeButton;
        [Required, SerializeField] private Button durationButton;

        private IEventPublisher _publisher;
        private IEventSubscriber _subscriber;
        private DomainEntityId _localPlayerId;

        public void Initialize(IEventPublisher publisher, IEventSubscriber subscriber, DomainEntityId localPlayerId)
        {
            _publisher = publisher;
            _subscriber = subscriber;
            _localPlayerId = localPlayerId;

            panel.SetActive(false);

            countButton.onClick.AddListener(() => Select(StatusType.Count));
            rangeButton.onClick.AddListener(() => Select(StatusType.Expand));
            durationButton.onClick.AddListener(() => Select(StatusType.Extend));

            _subscriber.Subscribe(this, new Action<UpgradeSelectionRequestedEvent>(OnSelectionRequested));
        }

        private void OnSelectionRequested(UpgradeSelectionRequestedEvent e)
        {
            panel.SetActive(true);
        }

        private void Select(StatusType type)
        {
            panel.SetActive(false);
            _publisher.Publish(new UpgradeSelectedEvent(_localPlayerId, type));
        }

        private void OnDestroy()
        {
            _subscriber?.UnsubscribeAll(this);
            countButton.onClick.RemoveAllListeners();
            rangeButton.onClick.RemoveAllListeners();
            durationButton.onClick.RemoveAllListeners();
        }
    }
}
