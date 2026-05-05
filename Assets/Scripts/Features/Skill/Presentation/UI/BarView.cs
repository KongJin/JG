using Features.Skill.Application.Events;
using Features.Skill.Application.Ports;
using Shared.EventBus;
using Shared.Attributes;
using Shared.Kernel;
using Shared.Lifecycle;
using UnityEngine;

namespace Features.Skill.Presentation
{
    public sealed class BarView : MonoBehaviour
    {
        [Required, SerializeField] private SlotView[] slotViews;

        private static readonly string[] SlotLabels = { "RMB", "Q" };

        private IEventSubscriber _eventBus;
        private ISkillPresentationAssetPort _assetPort;
        private DisposableScope _disposables = new();

        public Sprite NextDrawPreviewIcon { get; private set; }
        public string NextDrawHintLabel { get; private set; } = string.Empty;

        public void Initialize(IEventSubscriber eventBus, ISkillPresentationAssetPort assetPort, DomainEntityId localCasterId)
        {
            _eventBus = eventBus;
            _assetPort = assetPort;
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

// csharp-guardrails: allow-null-defense
            var icon = _assetPort?.GetIcon(e.SkillId.Value);
// csharp-guardrails: allow-null-defense
            slotViews[e.SlotIndex]?.SetSkill(icon);
        }

        private void OnDeckNextDrawPreview(DeckNextDrawPreviewEvent e)
        {
            ApplyNextDrawPreview(e.NextSkillId);
        }

        private void ApplyNextDrawPreview(string nextSkillIdOrNull)
        {
            NextDrawPreviewIcon = string.IsNullOrEmpty(nextSkillIdOrNull)
                ? null
// csharp-guardrails: allow-null-defense
                : _assetPort?.GetIcon(nextSkillIdOrNull);
            NextDrawHintLabel = string.IsNullOrEmpty(nextSkillIdOrNull) ? string.Empty : "다음";
        }
    }
}
