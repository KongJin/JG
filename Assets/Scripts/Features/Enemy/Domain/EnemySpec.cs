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
        /// <summary>XZ 평면에서 목표까지 이 거리 이하이면 이동을 멈춘다. 0이면 레거시: 3D 거리 제곱이 0.01 미만일 때만 정지.</summary>
        public float StopDistance { get; }

        public EnemySpec(
            float maxHp,
            float defense,
            float moveSpeed,
            float contactDamage,
            float contactCooldown,
            EnemyTargetMode targetMode = EnemyTargetMode.ChaseNearestPlayer,
            float aggroRadius = 0f,
            float stopDistance = 0f)
        {
            MaxHp = maxHp;
            Defense = defense;
            MoveSpeed = moveSpeed;
            ContactDamage = contactDamage;
            ContactCooldown = contactCooldown;
            TargetMode = targetMode;
            AggroRadius = aggroRadius;
            StopDistance = stopDistance;
        }
    }
}
