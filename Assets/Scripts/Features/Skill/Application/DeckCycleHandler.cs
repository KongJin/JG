using System;
using Features.Skill.Application.Events;
using Features.Skill.Domain;
using Shared.EventBus;
using Shared.Kernel;
using SkillEntity = Features.Skill.Domain.Skill;

namespace Features.Skill.Application
{
    /// <summary>
    /// 스킬 시전 시 덱에서 다음 스킬을 자동으로 뽑아 슬롯에 장착한다.
    /// </summary>
    public sealed class DeckCycleHandler
    {
        private readonly Deck _deck;
        private readonly SkillBar _bar;
        private readonly EquipSkillUseCase _equip;
        private readonly Func<string, SkillEntity> _skillLookup;
        private readonly DomainEntityId _localCasterId;
        private readonly IEventPublisher _publisher;

        public DeckCycleHandler(
            Deck deck,
            SkillBar bar,
            EquipSkillUseCase equip,
            Func<string, SkillEntity> skillLookup,
            DomainEntityId localCasterId,
            IEventSubscriber subscriber,
            IEventPublisher publisher)
        {
            _deck = deck;
            _bar = bar;
            _equip = equip;
            _skillLookup = skillLookup;
            _localCasterId = localCasterId;
            _publisher = publisher;

            subscriber.Subscribe(this, new Action<SkillCastedEvent>(OnSkillCasted));
        }

        /// <summary>초기 장착 직후 등, 덱 미리보기 HUD를 한 번 갱신한다.</summary>
        public void PublishDeckPreview()
        {
            _publisher.Publish(new DeckNextDrawPreviewEvent(_deck.PeekNextDrawSkillId()));
        }

        private void OnSkillCasted(SkillCastedEvent e)
        {
            if (!e.CasterId.Equals(_localCasterId)) return;

            _deck.Discard(e.SkillId);

            var nextId = _deck.Draw();
            if (string.IsNullOrEmpty(nextId.Value))
            {
                _publisher.Publish(new DeckNextDrawPreviewEvent(null));
                return;
            }

            var nextSkill = _skillLookup(nextId.Value);
// csharp-guardrails: allow-null-defense
            if (nextSkill == null)
            {
                _publisher.Publish(new DeckNextDrawPreviewEvent(null));
                return;
            }

            _equip.Execute(_bar, e.SlotIndex, nextSkill);
            _publisher.Publish(new DeckNextDrawPreviewEvent(_deck.PeekNextDrawSkillId()));
        }
    }
}
