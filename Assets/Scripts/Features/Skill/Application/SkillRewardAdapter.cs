using System.Collections.Generic;
using Features.Skill.Domain;
using Features.Skill.Infrastructure;
using Features.Wave.Application.Ports;
using Shared.Kernel;

namespace Features.Skill.Application
{
    public sealed class SkillRewardAdapter : ISkillRewardPort
    {
        private readonly Deck _deck;
        private readonly SkillBar _bar;
        private readonly SkillCatalog _catalog;
        private readonly System.Random _rng = new();

        public SkillRewardAdapter(Deck deck, SkillBar bar, SkillCatalog catalog)
        {
            _deck = deck;
            _bar = bar;
            _catalog = catalog;
        }

        public SkillRewardCandidate[] DrawCandidates(int count)
        {
            var inDeck = new HashSet<string>();
            foreach (var id in _deck.DrawPileIds)
                inDeck.Add(id.Value);
            foreach (var id in _deck.DiscardPileIds)
                inDeck.Add(id.Value);
            for (var i = 0; i < SkillBar.SlotCount; i++)
            {
                var s = _bar.GetSkill(i);
                if (s != null)
                    inDeck.Add(s.Id.Value);
            }

            var available = new List<SkillData>();
            foreach (var data in _catalog.AllSkills)
            {
                if (data == null || string.IsNullOrWhiteSpace(data.SkillId))
                    continue;
                if (inDeck.Contains(data.SkillId))
                    continue;
                available.Add(data);
            }

            for (var i = available.Count - 1; i > 0; i--)
            {
                var j = _rng.Next(i + 1);
                (available[i], available[j]) = (available[j], available[i]);
            }

            var take = System.Math.Min(count, available.Count);
            var result = new SkillRewardCandidate[take];
            for (var i = 0; i < take; i++)
            {
                var pres = available[i].Presentation;
                result[i] = new SkillRewardCandidate(
                    available[i].SkillId,
                    pres != null ? pres.DisplayName : available[i].SkillId);
            }
            return result;
        }

        public void AddToDeck(string skillId)
        {
            _deck.AddToDiscardPile(new DomainEntityId(skillId));
        }
    }
}
