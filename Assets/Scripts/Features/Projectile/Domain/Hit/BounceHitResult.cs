namespace Features.Projectile.Domain.Hit
{
    public sealed class BounceHitResult : IHitResult
    {
        public void Apply(global::Features.Projectile.Domain.Projectile projectile)
        {
            projectile.RegisterHit();
        }
    }
}

