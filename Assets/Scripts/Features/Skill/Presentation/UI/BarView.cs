using Shared.Attributes;
using Features.Skill.Application.Events;
using Features.Skill.Application.Ports;
using Shared.EventBus;
using Shared.Kernel;
using Shared.Lifecycle;
using UnityEngine;
using UnityEngine.UI;

namespace Features.Skill.Presentation
{
    public sealed class BarView : MonoBehaviour
    {
        [Required, SerializeField]
        private SlotView[] slotViews;

        [Tooltip("선택. 뽑기 더미에 다음 스킬이 있을 때 표시 (MVP ① 덱 순환 직관). 비우면 이벤트만 구독하고 UI는 건너뜀.")]
        [SerializeField]
        private Image nextDrawPreviewIcon;

        [Tooltip("선택. 예: \"다음\"")]
        [SerializeField]
        private Text nextDrawHintLabel;

        private static readonly string[] SlotLabels = { "RMB", "Q" };

        private IEventSubscriber _eventBus;
        private ISkillIconPort _iconPort;
        private DomainEntityId _localCasterId;
        private DisposableScope _disposables = new DisposableScope();

        public void Initialize(IEventSubscriber eventBus, ISkillIconPort iconPort, DomainEntityId localCasterId)
        {
            _eventBus = eventBus;
            _iconPort = iconPort;
            _localCasterId = localCasterId;
            _disposables.Dispose();
            _disposables = new DisposableScope();

            for (var i = 0; i < slotViews.Length; i++)
            {
                if (slotViews[i] == null)
                    continue;
                slotViews[i].SetKeyLabel(i < SlotLabels.Length ? SlotLabels[i] : string.Empty);
                slotViews[i].ClearSkill();
            }

            ApplyNextDrawPreview(null);

            _disposables.Add(EventBusSubscription.ForOwner(_eventBus, this));
            _eventBus.Subscribe(this, new System.Action<SkillEquippedEvent>(OnSkillEquipped));
            _eventBus.Subscribe(this, new System.Action<DeckNextDrawPreviewEvent>(OnDeckNextDrawPreview));
        }

        private void OnDestroy()
        {
            _disposables.Dispose();
        }

        private void OnSkillEquipped(SkillEquippedEvent e)
        {
            if (e.SlotIndex < 0 || e.SlotIndex >= slotViews.Length)
                return;
            var icon = _iconPort?.GetIcon(e.SkillId.Value);
            slotViews[e.SlotIndex].SetSkill(icon);
        }

        private void OnDeckNextDrawPreview(DeckNextDrawPreviewEvent e)
        {
            ApplyNextDrawPreview(e.NextSkillId);
        }

        private void ApplyNextDrawPreview(string nextSkillIdOrNull)
        {
            if (nextDrawPreviewIcon == null && nextDrawHintLabel == null)
                return;

            if (string.IsNullOrEmpty(nextSkillIdOrNull))
            {
                if (nextDrawPreviewIcon != null)
                {
                    nextDrawPreviewIcon.sprite = null;
                    nextDrawPreviewIcon.enabled = false;
                }

                if (nextDrawHintLabel != null)
                    nextDrawHintLabel.gameObject.SetActive(false);
                return;
            }

            var sprite = _iconPort?.GetIcon(nextSkillIdOrNull);
            if (nextDrawPreviewIcon != null)
            {
                nextDrawPreviewIcon.sprite = sprite;
                nextDrawPreviewIcon.enabled = sprite != null;
            }

            if (nextDrawHintLabel != null)
            {
                nextDrawHintLabel.gameObject.SetActive(true);
                if (string.IsNullOrEmpty(nextDrawHintLabel.text))
                    nextDrawHintLabel.text = "다음";
            }
        }
    }
}
