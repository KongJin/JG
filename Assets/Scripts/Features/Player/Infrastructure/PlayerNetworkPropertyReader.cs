using ExitGames.Client.Photon;
using Features.Player.Domain;

namespace Features.Player.Infrastructure
{
    internal static class PlayerNetworkPropertyKeys
    {
        public const string Health = "hp";
        public const string MaxHealth = "maxHp";
        public const string Energy = "energy";
        public const string MaxEnergy = "maxEnergy";
        public const string LifeState = "lifeState";
    }

    internal readonly struct PlayerResourceSnapshot
    {
        public PlayerResourceSnapshot(float current, float max)
        {
            Current = current;
            Max = max;
        }

        public float Current { get; }
        public float Max { get; }
    }

    internal static class PlayerNetworkPropertyReader
    {
        private const float DefaultMaxHealth = 100f;
        private const float DefaultMaxEnergy = 100f;

        public static Hashtable CreateHealthProperties(float currentHp, float maxHp)
        {
            return new Hashtable
            {
                { PlayerNetworkPropertyKeys.Health, currentHp },
                { PlayerNetworkPropertyKeys.MaxHealth, maxHp }
            };
        }

        public static Hashtable CreateEnergyProperties(float currentEnergy, float maxEnergy)
        {
            return new Hashtable
            {
                { PlayerNetworkPropertyKeys.Energy, currentEnergy },
                { PlayerNetworkPropertyKeys.MaxEnergy, maxEnergy }
            };
        }

        public static Hashtable CreateLifeStateProperties(LifeState state)
        {
            return new Hashtable
            {
                { PlayerNetworkPropertyKeys.LifeState, (int)state }
            };
        }

        public static bool TryReadHealthChange(
            Hashtable changedProps,
            Hashtable currentProps,
            out PlayerResourceSnapshot snapshot)
        {
            snapshot = default;
            if (!TryReadFloat(changedProps, PlayerNetworkPropertyKeys.Health, out var current))
                return false;

            if (!TryReadFloat(changedProps, PlayerNetworkPropertyKeys.MaxHealth, out var max) &&
                !TryReadFloat(currentProps, PlayerNetworkPropertyKeys.MaxHealth, out max))
                max = DefaultMaxHealth;

            snapshot = new PlayerResourceSnapshot(current, max);
            return true;
        }

        public static bool TryReadEnergyChange(
            Hashtable changedProps,
            Hashtable currentProps,
            out PlayerResourceSnapshot snapshot)
        {
            snapshot = default;
            if (!TryReadFloat(changedProps, PlayerNetworkPropertyKeys.Energy, out var current))
                return false;

            if (!TryReadFloat(changedProps, PlayerNetworkPropertyKeys.MaxEnergy, out var max) &&
                !TryReadFloat(currentProps, PlayerNetworkPropertyKeys.MaxEnergy, out max))
                max = DefaultMaxEnergy;

            snapshot = new PlayerResourceSnapshot(current, max);
            return true;
        }

        public static bool TryReadHydratedHealth(Hashtable props, out PlayerResourceSnapshot snapshot)
        {
            snapshot = default;
            if (!TryReadFloat(props, PlayerNetworkPropertyKeys.Health, out var current) ||
                !TryReadFloat(props, PlayerNetworkPropertyKeys.MaxHealth, out var max))
                return false;

            snapshot = new PlayerResourceSnapshot(current, max);
            return true;
        }

        public static bool TryReadHydratedEnergy(Hashtable props, out PlayerResourceSnapshot snapshot)
        {
            snapshot = default;
            if (!TryReadFloat(props, PlayerNetworkPropertyKeys.Energy, out var current))
                return false;

            if (!TryReadFloat(props, PlayerNetworkPropertyKeys.MaxEnergy, out var max))
                max = DefaultMaxEnergy;

            snapshot = new PlayerResourceSnapshot(current, max);
            return true;
        }

        public static bool TryReadLifeState(Hashtable props, out LifeState state)
        {
            state = default;
            if (props == null ||
                !props.TryGetValue(PlayerNetworkPropertyKeys.LifeState, out var raw) ||
                raw is not int value)
                return false;

            state = (LifeState)value;
            return true;
        }

        private static bool TryReadFloat(Hashtable props, string key, out float value)
        {
            value = default;
            if (props == null || !props.TryGetValue(key, out var raw) || raw is not float parsed)
                return false;

            value = parsed;
            return true;
        }
    }
}
