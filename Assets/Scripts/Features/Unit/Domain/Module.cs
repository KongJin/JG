namespace Features.Unit.Domain
{
    /// <summary>
    /// 모듈 타입 구분. SO 데이터 조회용 키.
    /// </summary>
    public enum ModuleType
    {
        Firepower,   // 상단: 화력
        Mobility,    // 하단: 기동
        Passive      // 중단: 고유 특성
    }

    /// <summary>
    /// 모듈 식별자. 타입 + ID 조합.
    /// </summary>
    public readonly struct ModuleId
    {
        public ModuleType Type { get; }
        public string Id { get; }

        public ModuleId(ModuleType type, string id)
        {
            Type = type;
            Id = id;
        }

        public override string ToString() => $"{Type}:{Id}";
    }

    /// <summary>
    /// 모듈 스탯 정의. 모든 모듈이 공통으로 가지는 수치.
    /// </summary>
    public readonly struct ModuleStats
    {
        public float HpBonus { get; }
        public float Defense { get; }
        public float AttackDamage { get; }
        public float AttackSpeed { get; }
        public float Range { get; }
        public float MoveSpeed { get; }
        public float MoveRange { get; }
        public int CostBonus { get; }

        public ModuleStats(
            float hpBonus = 0f,
            float defense = 0f,
            float attackDamage = 0f,
            float attackSpeed = 0f,
            float range = 0f,
            float moveSpeed = 0f,
            float moveRange = 0f,
            int costBonus = 0)
        {
            HpBonus = hpBonus;
            Defense = defense;
            AttackDamage = attackDamage;
            AttackSpeed = attackSpeed;
            Range = range;
            MoveSpeed = moveSpeed;
            MoveRange = moveRange;
            CostBonus = costBonus;
        }
    }
}
