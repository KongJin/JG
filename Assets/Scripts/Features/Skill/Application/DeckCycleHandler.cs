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

        public DeckCycleHandler(
            Deck deck,
            SkillBar bar,
            EquipSkillUseCase equip,
            Func<string, SkillEntity> skillLookup,
            DomainEntityId localCasterId,
            IEventSubscriber subscriber)
        {
            _deck = deck;
            _bar = bar;
            _equip = equip;
            _skillLookup = skillLookup;
            _localCasterId = localCasterId;

            subscriber.Subscribe(this, new Action<SkillCastedEvent>(OnSkillCasted));
        }

        private void OnSkillCasted(SkillCastedEvent e)
        {
            if (!e.CasterId.Equals(_localCasterId)) return;

            _deck.Discard(e.SkillId);

            var nextId = _deck.Draw();
            if (string.IsNullOrEmpty(nextId.Value)) return;

            var nextSkill = _skillLookup(nextId.Value);
            if (nextSkill == null) return;

            _equip.Execute(_bar, e.SlotIndex, nextSkill);
        }
    }
}
