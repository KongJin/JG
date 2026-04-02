using System;
using Features.Skill.Application.Events;
using Shared.Attributes;
using Shared.EventBus;
using UnityEngine;
using UnityEngine.UI;

namespace Features.Skill.Presentation
{
    public sealed class StartSkillSelectionView : MonoBehaviour
    {
        [Required, SerializeField] private GameObject panel;
        [Required, SerializeField] private SkillSelectButton[] skillButtons;
        [Required, SerializeField] private Button confirmButton;
        [Required, SerializeField] private Text instructionLabel;

        private IEventPublisher _publisher;
        private IEventSubscriber _subscriber;
        private ISkillIconPort _iconPort;
        private StartSkillCandidate[] _candidates;
        private int _pickCount;
        private int _selectedCount;

        public void Initialize(IEventPublisher publisher, IEventSubscriber subscriber, ISkillIconPort iconPort)
        {
            _publisher = publisher;
            _subscriber = subscriber;
            _iconPort = iconPort;

            panel.SetActive(false);
            confirmButton.interactable = false;
            confirmButton.onClick.AddListener(OnConfirm);

            for (var i = 0; i < skillButtons.Length; i++)
            {
                var index = i;
                skillButtons[i].Button.onClick.AddListener(() => OnToggle(index));
            }

            _subscriber.Subscribe(this, new Action<StartSkillSelectionRequestedEvent>(OnSelectionRequested));
        }

        private void OnSelectionRequested(StartSkillSelectionRequestedEvent e)
        {
            _candidates = e.Candidates;
            _pickCount = e.PickCount;
            _selectedCount = 0;

            for (var i = 0; i < skillButtons.Length; i++)
            {
                if (i < _candidates.Length)
                {
                    var icon = _iconPort?.GetIcon(_candidates[i].SkillId);
                    skillButtons[i].Setup(_candidates[i].DisplayName, icon);
                }
                else
                {
                    skillButtons[i].Hide();
                }
            }

            confirmButton.interactable = false;
            instructionLabel.text = $"시작 스킬 {_pickCount}개를 선택하세요";
            panel.SetActive(true);
        }

        private void OnToggle(int index)
        {
            if (index >= _candidates.Length) return;

            var btn = skillButtons[index];
            if (btn.IsSelected)
            {
                btn.SetSelected(false);
                _selectedCount--;
            }
            else
            {
                if (_selectedCount >= _pickCount) return;
                btn.SetSelected(true);
                _selectedCount++;
            }

            confirmButton.interactable = _selectedCount == _pickCount;
        }

        private void OnConfirm()
        {
            var chosen = new string[_pickCount];
            var idx = 0;
            for (var i = 0; i < skillButtons.Length && idx < _pickCount; i++)
            {
                if (i < _candidates.Length && skillButtons[i].IsSelected)
                    chosen[idx++] = _candidates[i].SkillId;
            }

            panel.SetActive(false);
            _publisher.Publish(new StartSkillSelectedEvent(chosen));
        }

        private void OnDestroy()
        {
            _subscriber?.UnsubscribeAll(this);
            confirmButton.onClick.RemoveAllListeners();
            for (var i = 0; i < skillButtons.Length; i++)
                skillButtons[i].Button.onClick.RemoveAllListeners();
        }
    }
}
