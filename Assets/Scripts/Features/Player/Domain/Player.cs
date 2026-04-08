using Shared.Kernel;
using Shared.Math;

namespace Features.Player.Domain
{
    public sealed class Player : Entity
    {
        public Player(DomainEntityId id, PlayerSpec spec) : base(id)
        {
            Spec = spec;
            MaxHp = spec.MaxHp;
            CurrentHp = spec.MaxHp;
            MaxEnergy = spec.MaxEnergy;
            CurrentEnergy = spec.MaxEnergy;
        }

        public PlayerSpec Spec { get; }
        public Float3 Position { get; private set; }

        public float MaxHp { get; }
        public float CurrentHp { get; private set; }
        public LifeState LifeState { get; private set; } = LifeState.Alive;
        public bool IsAlive => LifeState == LifeState.Alive;
        public bool IsDead => LifeState == LifeState.Dead;

        public float MaxEnergy { get; }
        public float CurrentEnergy { get; private set; }

        // TODO: Remove Mana fields - Skill system is being integrated into Unit attacks
        // See: docs/design/game_design.md - "Skill 시스템: 유닛의 공격 형태로 통합"
        internal float MaxMana => MaxEnergy;
        internal float CurrentMana => CurrentEnergy;

        public float TakeDamage(float damage)
        {
            if (IsDead || IsInvulnerable)
                return CurrentHp;

            if (damage < 0f)
                damage = 0f;

            CurrentHp -= damage;
            if (CurrentHp < 0f)
                CurrentHp = 0f;

            return CurrentHp;
        }

        public bool IsInvulnerable { get; private set; }
        public float InvulnerabilityRemaining { get; private set; }

        public void TickInvulnerability(float deltaTime)
        {
            if (!IsInvulnerable || InvulnerabilityRemaining <= 0f)
                return;

            InvulnerabilityRemaining -= deltaTime;
            if (InvulnerabilityRemaining <= 0f)
            {
                InvulnerabilityRemaining = 0f;
                IsInvulnerable = false;
            }
        }

        public void Die()
        {
            LifeState = LifeState.Dead;
            CurrentHp = 0f;
        }

        public void Respawn()
        {
            CurrentHp = MaxHp;
            CurrentEnergy = MaxEnergy;
            IsInvulnerable = false;
            LifeState = LifeState.Alive;
        }

        public void Hydrate(float hp, float energy)
        {
            CurrentHp = System.Math.Max(0f, System.Math.Min(hp, MaxHp));
            CurrentEnergy = System.Math.Max(0f, System.Math.Min(energy, MaxEnergy));
        }

        public bool SpendEnergy(float cost)
        {
            if (cost <= 0f)
                return true;
            if (CurrentEnergy < cost)
                return false;
            CurrentEnergy -= cost;
            return true;
        }

        public void RegenEnergy(float deltaTime, float regenPerSecond)
        {
            if (CurrentEnergy >= MaxEnergy)
                return;
            CurrentEnergy = System.Math.Min(MaxEnergy, CurrentEnergy + regenPerSecond * deltaTime);
        }

        public void SetInvulnerable(bool value)
        {
            IsInvulnerable = value;
        }

        public void ApplyMovement(Float3 position, bool isGrounded)
        {
            Position = position;
        }

        // TODO: Remove Mana methods - Skill system is being integrated into Unit attacks
        // See: docs/design/game_design.md - "Skill 시스템: 유닛의 공격 형태로 통합"
        // These methods are temporary compatibility wrappers around Energy
        internal bool SpendMana(float cost)
        {
            return SpendEnergy(cost);
        }

        internal void RegenMana(float deltaTime)
        {
            // Skill Mana regen uses constant rate from PlayerSpec
            // This is a simplified wrapper - actual regen logic should use spec.ManaRegenPerSecond
            RegenEnergy(deltaTime, 5f); // Default fallback rate
        }
    }
}
