namespace Features.Enemy.Domain
{
    public enum EnemyTargetMode
    {
        ChaseNearestPlayer = 0,
        ChaseCore = 1,
        ChaseCoreAggroPlayerInRadius = 2
    }

    public readonly struct EnemySpec
    {
        public float MaxHp { get; }
        public float Defense { get; }
        public float MoveSpeed { get; }
        public float ContactDamage { get; }
        public float ContactCooldown { get; }
        public EnemyTargetMode TargetMode { get; }
        public float AggroRadius { get; }

        public EnemySpec(
            float maxHp,
            float defense,
            float moveSpeed,
            float contactDamage,
            float contactCooldown,
            EnemyTargetMode targetMode = EnemyTargetMode.ChaseNearestPlayer,
            float aggroRadius = 0f)
        {
            MaxHp = maxHp;
            Defense = defense;
            MoveSpeed = moveSpeed;
            ContactDamage = contactDamage;
            ContactCooldown = contactCooldown;
            TargetMode = targetMode;
            AggroRadius = aggroRadius;
        }
    }
}
