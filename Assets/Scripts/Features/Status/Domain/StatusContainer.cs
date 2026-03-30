using System.Collections.Generic;

namespace Features.Status.Domain
{
    public sealed class StatusContainer
    {
        private readonly List<StatusEffect> _effects = new List<StatusEffect>();

        public IReadOnlyList<StatusEffect> Effects => _effects;

        public void Apply(StatusEffect effect)
        {
            var policy = StatusRule.GetPolicy(effect.Type);

            if (policy == StackPolicy.Refresh)
            {
                for (var i = 0; i < _effects.Count; i++)
                {
                    if (_effects[i].Type == effect.Type)
                    {
                        _effects[i].Refresh(effect.Duration);
                        return;
                    }
                }
            }
            else if (policy == StackPolicy.Independent)
            {
                var maxStacks = StatusRule.GetMaxStacks(effect.Type);
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
