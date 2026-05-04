using System.Collections.Generic;

namespace Features.Status.Domain
{
    public sealed class StatusContainer
    {
        private readonly List<StatusEffect> _effects = new List<StatusEffect>();
        private readonly IStatusRuleSet _rules;

        public IReadOnlyList<StatusEffect> Effects => _effects;

        public StatusContainer(IStatusRuleSet rules = null)
        {
            _rules = rules ?? StatusRule.Default;
        }

        public void Apply(StatusEffect effect)
        {
            if (effect == null)
                return;

            var policy = _rules.GetPolicy(effect.Type);

            if (policy == StackPolicy.Refresh)
            {
                for (var i = 0; i < _effects.Count; i++)
                {
                    if (_effects[i].Type == effect.Type)
                    {
                        _effects[i] = _effects[i].Refresh(effect.Duration);
                        return;
                    }
                }
            }
            else if (policy == StackPolicy.Independent)
            {
                var maxStacks = _rules.GetMaxStacks(effect.Type);
                var count = 0;
                for (var i = 0; i < _effects.Count; i++)
                {
                    if (_effects[i].Type == effect.Type)
                        count++;
                }

                if (count >= maxStacks)
                    return;
            }

            _effects.Add(effect);
        }

        public StatusEffect AdvanceEffect(int index, float deltaTime, out bool tickConsumed)
        {
            tickConsumed = false;
            if (index < 0 || index >= _effects.Count)
                return null;

            var advanced = _effects[index].Advance(deltaTime);
            var updated = advanced.ConsumeTickIfReady(out tickConsumed);
            _effects[index] = updated;
            return updated;
        }

        public float GetCombinedMagnitude(StatusType type)
        {
            var total = 0f;
            for (var i = 0; i < _effects.Count; i++)
            {
                if (_effects[i].Type == type && !_effects[i].IsExpired)
                    total += _effects[i].Magnitude;
            }
            return total;
        }

        public int GetStackCount(StatusType type)
        {
            var count = 0;
            for (var i = 0; i < _effects.Count; i++)
            {
                if (_effects[i].Type == type && !_effects[i].IsExpired)
                    count++;
            }
            return count;
        }

        public bool Has(StatusType type)
        {
            for (var i = 0; i < _effects.Count; i++)
            {
                if (_effects[i].Type == type && !_effects[i].IsExpired)
                    return true;
            }
            return false;
        }

        public int RemoveExpired()
        {
            var removed = 0;
            for (var i = _effects.Count - 1; i >= 0; i--)
            {
                if (_effects[i].IsExpired)
                {
                    _effects.RemoveAt(i);
                    removed++;
                }
            }
            return removed;
        }

        public void Clear()
        {
            _effects.Clear();
        }
    }
}
