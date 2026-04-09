using Features.Unit.Domain;
using Shared.Kernel;
using Shared.Math;

namespace Features.Unit.Domain
{
    /// <summary>
    /// 전장에 소환된 유닛 인스턴스 (가변 상태).
    /// Unit 스펙을 참조하며, HP, 위치, 상태 등을 관리한다.
    /// 재소환 시 새 BattleEntity가 생성된다 (같은 UnitSpec 참조).
    /// </summary>
    public sealed class BattleEntity : Entity
    {
        public Unit UnitSpec { get; }
        public DomainEntityId OwnerId { get; }

        public float MaxHp { get; }
        public float CurrentHp { get; private set; }
        public Float3 Position { get; private set; }
        public bool IsAlive => CurrentHp > 0f;
        public bool IsDead => CurrentHp <= 0f;

        public BattleEntity(
            DomainEntityId id,
            Unit unitSpec,
            DomainEntityId ownerId,
            Float3 spawnPosition,
            float? initialHp = null) : base(id)
        {
            UnitSpec = unitSpec;
            OwnerId = ownerId;
            MaxHp = unitSpec.FinalHp;
            CurrentHp = initialHp != null ? Clamp(initialHp.Value, 0f, unitSpec.FinalHp) : unitSpec.FinalHp;
            Position = spawnPosition;
        }

        private static float Clamp(float value, float min, float max) => value < min ? min : value > max ? max : value;

        public float TakeDamage(float damage)
        {
            if (IsDead)
                return CurrentHp;

            if (damage < 0f)
                damage = 0f;

            CurrentHp -= damage;
            if (CurrentHp < 0f)
                CurrentHp = 0f;

            return CurrentHp;
        }

        public void MoveTo(Float3 newPosition)
        {
            // 앵커 반경 제한 확인
            var distance = Float3.Distance(Position, newPosition);
            if (distance > UnitSpec.FinalAnchorRange)
            {
                // 앵커 반경 밖으로 이동 불가
                return;
            }

            Position = newPosition;
        }

        public void Die()
        {
            CurrentHp = 0f;
        }

        /// <summary>
        /// 네트워크 동기화용 HP 직접 설정.
        /// 원격 클라이언트에서 Owner의 상태를 로컬 도메인에 반영할 때 사용.
        /// </summary>
        public void SetHpFromNetwork(float hp)
        {
            if (hp < 0f) hp = 0f;
            if (hp > MaxHp) hp = MaxHp;
            CurrentHp = hp;
        }
    }
}
