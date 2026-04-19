namespace Features.Projectile.Domain.Hit
{
    public sealed class SingleHitResolver : IHitResolver
    {
        public IHitResult Resolve(global::Features.Projectile.Domain.Projectile projectile)
        {
            return new DestroyHitResult();
        }
    }
}















