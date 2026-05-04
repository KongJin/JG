using System;
using Features.Skill.Application.Events;
using Shared.EventBus;
using UnityEngine;

namespace Features.Skill.Presentation
{
    public sealed class StartSkillSelectionView : MonoBehaviour
    {
        [SerializeField] private SkillSelectButton[] skillButtons;

        private IEventPublisher _publisher;
        private IEventSubscriber _subscriber;
        private ISkillPresentationAssetPort _assetPort;
        private StartSkillCandidate[] _candidates;
        private int _pickCount;
        private int _selectedCount;

        public bool IsPanelVisible { get; private set; }
        public bool CanConfirm => _selectedCount == _pickCount;
        public string InstructionText { get; private set; } = string.Empty;

        public void Initialize(IEventPublisher publisher, IEventSubscriber subscriber, ISkillPresentationAssetPort assetPort)
        {
            _publisher = publisher;
            _subscriber = subscriber;
            _assetPort = assetPort;
            IsPanelVisible = false;
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
                    var icon = _assetPort?.GetIcon(_candidates[i].SkillId);
                    skillButtons[i].Setup(_candidates[i].DisplayName, icon);
                }
                else
                {
                    skillButtons[i].Hide();
                }
            }

            InstructionText = $"시작 스킬 {_pickCount}개를 선택하세요";
            IsPanelVisible = true;
        }

        public void Toggle(int index)
        {
            if (_candidates == null || index < 0 || index >= _candidates.Length)
                return;

            var button = skillButtons[index];
            if (button.IsSelected)
            {
                button.SetSelected(false);
                _selectedCount--;
                return;
            }

            if (_selectedCount >= _pickCount)
                return;

            button.SetSelected(true);
            _selectedCount++;
        }

        public void Confirm()
        {
            if (!CanConfirm)
                return;

            var chosen = new string[_pickCount];
            var idx = 0;
            for (var i = 0; i < skillButtons.Length && idx < _pickCount; i++)
            {
                if (i < _candidates.Length && skillButtons[i].IsSelected)
                    chosen[idx++] = _candidates[i].SkillId;
            }

            IsPanelVisible = false;
            _publisher.Publish(new StartSkillSelectedEvent(chosen));
        }

        private void OnDestroy()
        {
            _subscriber?.UnsubscribeAll(this);
        }
    }
}
