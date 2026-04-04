using System;
using Features.Skill.Domain;
using Features.Skill.Presentation;
using Features.Wave.Application;
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
        [Required, SerializeField] private Button skipButton;

        private IEventPublisher _publisher;
        private IEventSubscriber _subscriber;
        private ISkillIconPort _iconPort;
        private DomainEntityId _localPlayerId;
        private RewardCandidate[] _candidates;
        private SelectionTimer _selectionTimer;
        private int _rewardContextWaveIndex;
        private float _selectionDurationSeconds;

        private void Awake()
        {
            // Canvas sortingOrder가 시작 스킬 선택보다 높을 때, Initialize 전 한 프레임이라도 Panel이 켜 있으면 전체를 가린다.
            panel.SetActive(false);
        }

        public void Initialize(IEventPublisher publisher, IEventSubscriber subscriber, DomainEntityId localPlayerId, ISkillIconPort iconPort, SelectionTimer selectionTimer)
        {
            _publisher = publisher;
            _subscriber = subscriber;
            _localPlayerId = localPlayerId;
            _iconPort = iconPort;
            _selectionTimer = selectionTimer;

            panel.SetActive(false);

            countButton.onClick.AddListener(() => SelectCandidate(0));
            rangeButton.onClick.AddListener(() => SelectCandidate(1));
            durationButton.onClick.AddListener(() => SelectCandidate(2));
            skipButton.onClick.AddListener(OnSkipClicked);

            _subscriber.Subscribe(this, new Action<SkillSelectionRequestedEvent>(OnSelectionRequested));
            _subscriber.Subscribe(this, new Action<SkillSelectedEvent>(OnSkillSelected));
            _subscriber.Subscribe(this, new Action<SkillSelectionSkippedEvent>(OnSelectionSkipped));
        }

        private void OnSelectionRequested(SkillSelectionRequestedEvent e)
        {
            _candidates = e.Candidates;
            _rewardContextWaveIndex = e.WaveIndex;
            _selectionDurationSeconds = e.SelectionDuration;

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

            if (countdownText != null)
                countdownText.text = Mathf.CeilToInt(e.SelectionDuration).ToString();

            skipButton.gameObject.SetActive(true);

            panel.SetActive(true);
        }

        private void Update()
        {
            if (_selectionTimer == null || !_selectionTimer.IsRunning) return;

            if (countdownText != null)
                countdownText.text = Mathf.CeilToInt(Mathf.Max(0f, _selectionTimer.Remaining)).ToString();
        }

        private void OnSkillSelected(SkillSelectedEvent e)
        {
            _candidates = null;
            panel.SetActive(false);
        }

        private void OnSelectionSkipped(SkillSelectionSkippedEvent e)
        {
            if (!e.PlayerId.Equals(_localPlayerId))
                return;
            _candidates = null;
            panel.SetActive(false);
        }

        private void OnSkipClicked()
        {
            if (_candidates == null)
                return;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var elapsed = _selectionTimer != null
                ? _selectionDurationSeconds - _selectionTimer.Remaining
                : 0f;
            Debug.Log(
                $"[MvpReward] manual_skip contextWaveIndex={_rewardContextWaveIndex} elapsedSec={elapsed:F2}");
#endif
            _candidates = null;
            panel.SetActive(false);
            _publisher.Publish(new SkillSelectionSkippedEvent(_localPlayerId, _rewardContextWaveIndex));
        }

        private void SelectCandidate(int index)
        {
            if (_candidates == null || index >= _candidates.Length)
                return;

            panel.SetActive(false);
            var c = _candidates[index];
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var elapsed = _selectionTimer != null
                ? _selectionDurationSeconds - _selectionTimer.Remaining
                : 0f;
            Debug.Log(
                $"[MvpReward] manual_select contextWaveIndex={_rewardContextWaveIndex} " +
                $"index={index} type={c.Type} skillId={c.SkillId} axis={c.Axis} elapsedSec={elapsed:F2}");
#endif
            _publisher.Publish(new SkillSelectedEvent(_localPlayerId, c.SkillId, c.DisplayName, c.Type, c.Axis));
        }

        private void OnDestroy()
        {
            _subscriber?.UnsubscribeAll(this);
            countButton.onClick.RemoveAllListeners();
            rangeButton.onClick.RemoveAllListeners();
            durationButton.onClick.RemoveAllListeners();
            skipButton.onClick.RemoveAllListeners();
        }
    }
}
