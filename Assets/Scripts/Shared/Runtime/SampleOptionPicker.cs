using System;
using System.Collections.Generic;

namespace Shared.Runtime
{
    public static class SampleOptionPicker
    {
        public static bool TryPickFirst<T>(
            IReadOnlyList<T> options,
            Func<T, bool> predicate,
            out T selected)
            where T : class
        {
            selected = null;
            if (options == null || predicate == null)
                return false;

            for (int i = 0; i < options.Count; i++)
            {
                var option = options[i];
                if (option != null && predicate(option))
                {
                    selected = option;
                    return true;
                }
            }

            return false;
        }

        public static bool TryPickPreferredOrFirst<T>(
            IReadOnlyList<T> options,
            Func<T, bool> preferred,
            Func<T, bool> fallback,
            out T selected)
            where T : class
        {
            return TryPickFirst(options, preferred, out selected) ||
                   TryPickFirst(options, fallback, out selected);
        }
    }
}
