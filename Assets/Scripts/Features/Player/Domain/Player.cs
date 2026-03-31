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
            MaxMana = spec.MaxMana;
            CurrentMana = spec.MaxMana;
        }

        public PlayerSpec Spec { get; }
        public Float3 Position { get; private set; }
        public float VerticalVelocity { get; private set; }
        public bool IsGrounded { get; private set; }
        public bool IsSprinting { get; private set; }

        public float MaxHp { get; }
        public float CurrentHp { get; private set; }
        public LifeState LifeState { get; private set; } = LifeState.Alive;
        public bool IsAlive => LifeState == LifeState.Alive;
        public bool IsDowned => LifeState == LifeState.Downed;
        public bool IsDead => LifeState == LifeState.Dead;
        public bool IsInvulnerable { get; private set; }
        public float BleedoutElapsed { get; private set; }

        public float MaxMana { get; }
        public float CurrentMana { get; private set; }

        public float TakeDamage(float damage)
        {
            if (IsDead || IsDowned || IsInvulnerable)
                return CurrentHp;

            if (damage < 0f)
                damage = 0f;

            CurrentHp -= damage;
            if (CurrentHp < 0f)
                CurrentHp = 0f;

            if (CurrentHp <= 0f && IsAlive)
            {
                LifeState = LifeState.Downed;
                BleedoutElapsed = 0f;
            }

            return CurrentHp;
        }

        public void TickBleedout(float deltaTime)
        {
            if (!IsDowned)
                return;

            BleedoutElapsed += deltaTime;
            if (BleedoutRule.IsExpired(BleedoutElapsed))
            {
                LifeState = LifeState.Dead;
            }
        }

        public float InvulnerabilityRemaining { get; private set; }

        public void Rescue(float hp, float mana)
        {
            CurrentHp = hp;
            CurrentMana = mana;
            LifeState = LifeState.Alive;
            IsInvulnerable = true;
            InvulnerabilityRemaining = RescueRule.InvulnerabilityDuration;
            BleedoutElapsed = 0f;
        }

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

        public void ForceDowned()
        {
            if (IsDowned || IsDead)
                return;

            CurrentHp = 0f;
            LifeState = LifeState.Downed;
            BleedoutElapsed = 0f;
        }

        public void Die()
        {
            LifeState = LifeState.Dead;
            CurrentHp = 0f;
        }

        public void Respawn()
        {
            CurrentHp = MaxHp;
            CurrentMana = MaxMana;
            IsInvulnerable = false;
            LifeState = LifeState.Alive;
            BleedoutElapsed = 0f;
        }

        public void Hydrate(float hp, float mana)
        {
            CurrentHp = System.Math.Max(0f, System.Math.Min(hp, MaxHp));
            CurrentMana = System.Math.Max(0f, System.Math.Min(mana, MaxMana));
        }

        public bool SpendMana(float cost)
        {
            if (cost <= 0f)
                return true;
            if (CurrentMana < cost)
                return false;
            CurrentMana -= cost;
            return true;
        }

        public void RegenMana(float deltaTime)
        {
            if (CurrentMana >= MaxMana)
                return;
            CurrentMana = System.Math.Min(MaxMana, CurrentMana + Spec.ManaRegenPerSecond * deltaTime);
        }

        public void SetInvulnerable(bool value)
        {
            IsInvulnerable = value;
        }

        public Float3 CalculateMovement(Float2 input, float deltaTime, float speedOverride = -1f)
        {
            var baseSpeed = speedOverride >= 0f
                ? speedOverride
                : Spec.WalkSpeed;
            var speed = MovementRule.SelectSpeed(baseSpeed, IsSprinting, Spec.SprintMultiplier);
            var horizontal = MovementRule.CalculateDelta(input, speed, deltaTime);

            VerticalVelocity = MovementRule.ApplyGravity(VerticalVelocity, Spec.Gravity, deltaTime, IsGrounded);

            return new Float3(horizontal.X, VerticalVelocity * deltaTime, horizontal.Z);
        }

        public bool TryJump()
        {
            if (!IsGrounded || !IsAlive)
                return false;

            VerticalVelocity = Spec.JumpForce;
            return true;
        }

        public void SetSprinting(bool value)
        {
            IsSprinting = value;
        }

        public void ApplyMovement(Float3 position, bool isGrounded)
        {
            Position = position;
            IsGrounded = isGrounded;
            if (isGrounded) VerticalVelocity = 0f;
        }
    }
}
