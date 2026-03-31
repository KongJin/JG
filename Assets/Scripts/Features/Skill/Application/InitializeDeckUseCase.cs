using System.Collections.Generic;
using Features.Skill.Domain;
using Features.Wave.Application.Ports;
using Shared.Kernel;

namespace Features.Skill.Application
{
    public sealed class InitializeDeckUseCase
    {
        public readonly struct SkillEntry
        {
            public readonly string SkillId;
            public readonly string DisplayName;

            public SkillEntry(string skillId, string displayName)
            {
                SkillId = skillId;
                DisplayName = displayName;
            }
        }

        public readonly struct DeckSetupResult
        {
            public readonly Deck Deck;
            public readonly IReadOnlyList<SkillRewardCandidate> RewardPool;

            public DeckSetupResult(Deck deck, IReadOnlyList<SkillRewardCandidate> rewardPool)
            {
                Deck = deck;
                RewardPool = rewardPool;
            }
        }

        private readonly System.Random _rng = new();

        public DeckSetupResult Execute(IReadOnlyList<SkillEntry> uniqueSkills, int starterCount)
        {
            var shuffled = new List<SkillEntry>(uniqueSkills);
            for (var i = shuffled.Count - 1; i > 0; i--)
            {
                var j = _rng.Next(i + 1);
                (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            }

            var count = System.Math.Min(starterCount, shuffled.Count);
            var starterIds = new List<DomainEntityId>(count);
            var rewardPool = new List<SkillRewardCandidate>();

            for (var i = 0; i < shuffled.Count; i++)
            {
                var entry = shuffled[i];
                if (i < count)
                    starterIds.Add(new DomainEntityId(entry.SkillId));
                else
                    rewardPool.Add(new SkillRewardCandidate(entry.SkillId, entry.DisplayName));
            }

            var deck = new Deck(starterIds);
            return new DeckSetupResult(deck, rewardPool);
        }
    }
}
