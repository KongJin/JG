using System;
using Features.Skill.Domain;
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
        [SerializeField] private Text countdownText;

        private IEventPublisher _publisher;
        private IEventSubscriber _subscriber;
        private ISkillIconPort _iconPort;
        private DomainEntityId _localPlayerId;
        private RewardCandidate[] _candidates;
        private float _countdown;
        private bool _countdownActive;

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
            _subscriber.Subscribe(this, new Action<SkillSelectedEvent>(OnSkillSelected));
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
                    var c = _candidates[i];
                    if (c.Type == CandidateType.NewSkill)
                    {
                        labels[i].text = $"[NEW] {c.DisplayName}";
                    }
                    else
                    {
                        labels[i].text = $"[강화] {c.DisplayName}\n{c.EffectDescription} Lv.{c.CurrentLevel + 1}";
                    }
                    icons[i].sprite = _iconPort.GetIcon(c.SkillId);
                }
                else
                {
                    buttons[i].gameObject.SetActive(false);
                }
            }

            _countdown = e.SelectionDuration;
            _countdownActive = true;
            if (countdownText != null)
                countdownText.text = Mathf.CeilToInt(_countdown).ToString();

            panel.SetActive(true);
        }

        private void Update()
        {
            if (!_countdownActive) return;

            _countdown -= Time.deltaTime;
            if (countdownText != null)
                countdownText.text = Mathf.CeilToInt(Mathf.Max(0f, _countdown)).ToString();

            if (_countdown <= 0f)
                _countdownActive = false;
        }

        private void OnSkillSelected(SkillSelectedEvent e)
        {
            _countdownActive = false;
            _candidates = null;
            panel.SetActive(false);
        }

        private void SelectCandidate(int index)
        {
            if (_candidates == null || index >= _candidates.Length)
                return;

            _countdownActive = false;
            panel.SetActive(false);
            var c = _candidates[index];
            _publisher.Publish(new SkillSelectedEvent(_localPlayerId, c.SkillId, c.DisplayName, c.Type, c.Axis));
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
