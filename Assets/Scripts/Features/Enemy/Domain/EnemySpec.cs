namespace Features.Enemy.Domain
{
    public readonly struct EnemySpec
    {
        public float MaxHp { get; }
        public float Defense { get; }
        public float MoveSpeed { get; }
        public float ContactDamage { get; }
        public float ContactCooldown { get; }

        public EnemySpec(float maxHp, float defense, float moveSpeed, float contactDamage, float contactCooldown)
        {
            MaxHp = maxHp;
            Defense = defense;
            MoveSpeed = moveSpeed;
            ContactDamage = contactDamage;
            ContactCooldown = contactCooldown;
        }
    }
}
