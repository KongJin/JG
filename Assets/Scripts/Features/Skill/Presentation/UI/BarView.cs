using Shared.Attributes;
using Features.Skill.Application.Events;
using Features.Skill.Application.Ports;
using Shared.EventBus;
using Shared.Kernel;
using Shared.Lifecycle;
using UnityEngine;

namespace Features.Skill.Presentation
{
    public sealed class BarView : MonoBehaviour
    {
        [Required, SerializeField]
        private SlotView[] slotViews;
        private static readonly string[] SlotLabels = { "RMB", "Q", "E" };

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

            _disposables.Add(EventBusSubscription.ForOwner(_eventBus, this));
            _eventBus.Subscribe(this, new System.Action<SkillEquippedEvent>(OnSkillEquipped));
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
    }
}
