using Shared.Attributes;
using Features.Skill.Application.Events;
using Features.Skill.Application.Ports;
using Shared.EventBus;
using Shared.Kernel;
using Shared.Lifecycle;
using System;
using UnityEngine;

namespace Features.Skill.Presentation
{
    public sealed class BarView : MonoBehaviour
    {
        [Required, SerializeField]
        private SlotView[] slotViews;
        private static readonly string[] SlotLabels = { "RMB", "Q", "E", "R" };

        private IEventSubscriber _eventBus;
        private ISkillIconPort _iconPort;
        private Action<int> _onSlotClicked;
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

            _disposables.Add(EventBusSubscription.ForOwner(_eventBus, this));
            _eventBus.Subscribe(this, new System.Action<SkillEquippedEvent>(OnSkillEquipped));
            _eventBus.Subscribe(this, new System.Action<SkillCastedEvent>(OnSkillCasted));
        }

        public void SetSlotClickHandler(Action<int> onSlotClicked)
        {
            _onSlotClicked = onSlotClicked;

            for (var i = 0; i < slotViews.Length; i++)
            {
                var slotIndex = i;
                var slotView = slotViews[i];
                if (slotView == null)
                    continue;

                slotView.SetClickHandler(() => _onSlotClicked?.Invoke(slotIndex));
            }
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
            Debug.Log($"[BarView] Slot {e.SlotIndex} equipped: {e.SkillId}");
        }

        private void OnSkillCasted(SkillCastedEvent e)
        {
            if (e.CasterId != _localCasterId)
                return;

            if (e.SlotIndex < 0 || e.SlotIndex >= slotViews.Length)
                return;
            slotViews[e.SlotIndex].StartCooldown(e.Spec.Cooldown);
        }
    }
}
