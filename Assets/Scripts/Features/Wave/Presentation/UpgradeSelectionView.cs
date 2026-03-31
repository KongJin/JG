using System;
using Features.Skill.Presentation;
using Features.Wave.Application.Events;
using Features.Wave.Application.Ports;
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
        [Required, SerializeField] private Text countLabel;
        [Required, SerializeField] private Text rangeLabel;
        [Required, SerializeField] private Text durationLabel;
        [Required, SerializeField] private Image countIcon;
        [Required, SerializeField] private Image rangeIcon;
        [Required, SerializeField] private Image durationIcon;

        private IEventPublisher _publisher;
        private IEventSubscriber _subscriber;
        private ISkillIconPort _iconPort;
        private DomainEntityId _localPlayerId;
        private SkillRewardCandidate[] _candidates;

        public void Initialize(IEventPublisher publisher, IEventSubscriber subscriber, DomainEntityId localPlayerId, ISkillIconPort iconPort)
        {
            _publisher = publisher;
            _subscriber = subscriber;
            _localPlayerId = localPlayerId;
            _iconPort = iconPort;

            panel.SetActive(false);

            countButton.onClick.AddListener(() => SelectCandidate(0));
            rangeButton.onClick.AddListener(() => SelectCandidate(1));
            durationButton.onClick.AddListener(() => SelectCandidate(2));

            _subscriber.Subscribe(this, new Action<SkillSelectionRequestedEvent>(OnSelectionRequested));
        }

        private void OnSelectionRequested(SkillSelectionRequestedEvent e)
        {
            _candidates = e.Candidates;

            var buttons = new[] { countButton, rangeButton, durationButton };
            var labels = new[] { countLabel, rangeLabel, durationLabel };
            var icons = new[] { countIcon, rangeIcon, durationIcon };

            for (var i = 0; i < buttons.Length; i++)
            {
                if (i < _candidates.Length)
                {
                    buttons[i].gameObject.SetActive(true);
                    labels[i].text = _candidates[i].DisplayName;
                    icons[i].sprite = _iconPort.GetIcon(_candidates[i].SkillId);
                }
                else
                {
                    buttons[i].gameObject.SetActive(false);
                }
            }

            panel.SetActive(true);
        }

        private void SelectCandidate(int index)
        {
            if (_candidates == null || index >= _candidates.Length)
                return;

            panel.SetActive(false);
            var candidate = _candidates[index];
            _publisher.Publish(new SkillSelectedEvent(_localPlayerId, candidate.SkillId, candidate.DisplayName));
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
