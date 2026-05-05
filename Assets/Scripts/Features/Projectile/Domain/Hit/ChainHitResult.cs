namespace Features.Projectile.Domain.Hit
{
    public sealed class ChainHitResult : IHitResult
    {
        public void Apply(global::Features.Projectile.Domain.Projectile projectile)
        {
            projectile.RegisterHit();
        }
    }
}















