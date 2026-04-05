namespace Features.Player.Domain
{
    public readonly struct PlayerSpec
    {
        public float MaxHp { get; }
        public float Defense { get; }
        public float MaxMana { get; }
        public float ManaRegenPerSecond { get; }

        public PlayerSpec(
            float maxHp,
            float defense = 0f,
            float maxMana = 100f,
            float manaRegenPerSecond = 5f
        )
        {
            MaxHp = maxHp > 0f ? maxHp : 100f;
            Defense = defense < 0f ? 0f : defense;
            MaxMana = maxMana > 0f ? maxMana : 100f;
            ManaRegenPerSecond = manaRegenPerSecond < 0f ? 0f : manaRegenPerSecond;
        }
    }
}
