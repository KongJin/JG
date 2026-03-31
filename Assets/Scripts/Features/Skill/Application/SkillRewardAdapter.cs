using System.Collections.Generic;
using Features.Skill.Domain;
using Features.Wave.Application.Ports;
using Shared.Kernel;

namespace Features.Skill.Application
{
    public sealed class SkillRewardAdapter : ISkillRewardPort
    {
        private readonly Deck _deck;
        private readonly SkillBar _bar;
        private readonly IReadOnlyList<SkillRewardCandidate> _rewardPool;
        private readonly System.Random _rng = new();

        public SkillRewardAdapter(Deck deck, SkillBar bar, IReadOnlyList<SkillRewardCandidate> rewardPool)
        {
            _deck = deck;
            _bar = bar;
            _rewardPool = rewardPool;
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

            var available = new List<SkillRewardCandidate>();
            foreach (var candidate in _rewardPool)
            {
                if (!inDeck.Contains(candidate.SkillId))
                    available.Add(candidate);
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
                result[i] = available[i];
            }
            return result;
        }

        public void AddToDeck(string skillId)
        {
            _deck.AddToDiscardPile(new DomainEntityId(skillId));
        }
    }
}
