namespace Features.Projectile.Domain.Hit
{
    public sealed class ContinueHitResult : IHitResult
    {
        public void Apply(global::Features.Projectile.Domain.Projectile projectile)
        {
            projectile.RegisterHit();
        }
    }
}















