namespace Features.Player.Domain
{
    public readonly struct PlayerSpec
    {
        public float WalkSpeed { get; }
        public float SprintMultiplier { get; }
        public float JumpForce { get; }
        public float Gravity { get; }
        public float MaxHp { get; }
        public float Defense { get; }
        public float RotationSpeed { get; }
        public float MaxMana { get; }
        public float ManaRegenPerSecond { get; }

        public PlayerSpec(
            float walkSpeed,
            float sprintMultiplier,
            float jumpForce,
            float gravity,
            float maxHp,
            float defense = 0f,
            float rotationSpeed = 720f,
            float maxMana = 100f,
            float manaRegenPerSecond = 5f
        )
        {
            WalkSpeed = walkSpeed;
            SprintMultiplier = sprintMultiplier;
            JumpForce = jumpForce;
            Gravity = gravity;
            MaxHp = maxHp > 0f ? maxHp : 100f;
            Defense = defense < 0f ? 0f : defense;
            RotationSpeed = rotationSpeed;
            MaxMana = maxMana > 0f ? maxMana : 100f;
            ManaRegenPerSecond = manaRegenPerSecond < 0f ? 0f : manaRegenPerSecond;
        }
    }
}
