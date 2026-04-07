namespace Features.Player.Domain
{
    public readonly struct PlayerSpec
    {
        public float MaxHp { get; }
        public float Defense { get; }
        public float MaxEnergy { get; }
        public float EnergyRegenPerSecond { get; }

        public PlayerSpec(
            float maxHp,
            float defense = 0f,
            float maxEnergy = 100f,
            float energyRegenPerSecond = 5f
        )
        {
            MaxHp = maxHp > 0f ? maxHp : 100f;
            Defense = defense < 0f ? 0f : defense;
            MaxEnergy = maxEnergy > 0f ? maxEnergy : 100f;
            EnergyRegenPerSecond = energyRegenPerSecond < 0f ? 0f : energyRegenPerSecond;
        }
    }
}
