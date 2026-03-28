using UnityEngine;

namespace Shared.Analytics
{
    public static class RoundCounter
    {
        static int _count;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Init() => _count = 0;

        public static int Current => _count;
        public static void Increment() => _count++;
        public static void Reset() => _count = 0;
    }
}
