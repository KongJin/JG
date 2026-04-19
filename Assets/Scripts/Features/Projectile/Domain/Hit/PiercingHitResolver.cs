namespace Features.Projectile.Domain.Hit
{
    public sealed class PiercingHitResolver : IHitResolver
    {
        public IHitResult Resolve(global::Features.Projectile.Domain.Projectile projectile)
        {
            return new ContinueHitResult();
        }
    }
}















