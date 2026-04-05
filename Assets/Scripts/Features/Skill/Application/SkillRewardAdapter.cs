using System;
using System.Collections.Generic;
using Features.Skill.Application.Ports;
using Features.Skill.Domain;
using Shared.Kernel;

namespace Features.Skill.Application
{
    public sealed class SkillRewardAdapter
    {
        private readonly Deck _deck;
        private readonly SkillBar _bar;
        private readonly IReadOnlyList<SkillRewardCandidate> _rewardPool;
        private readonly System.Random _rng;
        private readonly ISkillUpgradeCommandPort _upgradeCommand;
        private readonly ISkillUpgradeQueryPort _upgradeQuery;
        private readonly Func<string, string> _getDisplayName;
        private readonly Func<string, IReadOnlyCollection<GrowthAxis>> _getEnabledAxes;

        public SkillRewardAdapter(
            Deck deck,
            SkillBar bar,
            IReadOnlyList<SkillRewardCandidate> rewardPool,
            ISkillUpgradeCommandPort upgradeCommand = null,
            ISkillUpgradeQueryPort upgradeQuery = null,
            Func<string, string> getDisplayName = null,
            Func<string, IReadOnlyCollection<GrowthAxis>> getEnabledAxes = null,
            System.Random rng = null)
        {
            _deck = deck;
            _bar = bar;
            _rewardPool = rewardPool;
            _upgradeCommand = upgradeCommand;
            _upgradeQuery = upgradeQuery;
            _getDisplayName = getDisplayName;
            _getEnabledAxes = getEnabledAxes;
            _rng = rng ?? new System.Random();
        }

        public SkillRewardCandidate[] DrawCandidates(int count)
        {
            var available = GetAvailableNewSkills();

            for (var i = available.Count - 1; i > 0; i--)
            {
                var j = _rng.Next(i + 1);
                (available[i], available[j]) = (available[j], available[i]);
            }

            var take = Math.Min(count, available.Count);
            var result = new SkillRewardCandidate[take];
            for (var i = 0; i < take; i++)
                result[i] = available[i];
            return result;
        }

        private List<SkillRewardCandidate> GetAvailableNewSkills()
        {
            var inDeck = CollectDeckSkillIds();

            var available = new List<SkillRewardCandidate>();
            foreach (var candidate in _rewardPool)
            {
                if (!inDeck.Contains(candidate.SkillId))
                    available.Add(candidate);
            }
            return available;
        }

        private HashSet<string> CollectDeckSkillIds()
        {
            var ids = new HashSet<string>();
            foreach (var id in _deck.DrawPileIds)
                ids.Add(id.Value);
            foreach (var id in _deck.DiscardPileIds)
                ids.Add(id.Value);
            for (var i = 0; i < SkillBar.SlotCount; i++)
            {
                var s = _bar.GetSkill(i);
                if (s != null)
                    ids.Add(s.Id.Value);
            }
            return ids;
        }

        private static string GetAxisDescription(GrowthAxis axis) => axis switch
        {
            GrowthAxis.Count => "발사 수 증가",
            GrowthAxis.Range => "범위 증가",
            GrowthAxis.Duration => "지속 시간 증가",
            GrowthAxis.Safety => "아군 피해 감소",
            _ => axis.ToString()
        };
    }
}
