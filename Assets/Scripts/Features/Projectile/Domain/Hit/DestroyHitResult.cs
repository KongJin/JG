namespace Features.Projectile.Domain.Hit
{
    public sealed class DestroyHitResult : IHitResult
    {
        public void Apply(global::Features.Projectile.Domain.Projectile projectile)
        {
            projectile.RegisterHit();
            projectile.Destroy();
        }
    }
}















