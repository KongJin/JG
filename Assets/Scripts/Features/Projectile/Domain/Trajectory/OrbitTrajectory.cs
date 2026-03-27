using Shared.Math;

namespace Features.Projectile.Domain.Trajectory
{
    public sealed class OrbitTrajectory : ITrajectory
    {
        public const float DefaultOrbitRadius = 3f;

        public Float3 Calculate(in TrajectoryInput input)
        {
            var startOffset = input.Origin - input.TargetPosition;
            var orbitRadius = startOffset.Magnitude;
            var startAngle = orbitRadius > 0.001f
                ? (float)System.Math.Atan2(startOffset.Z, startOffset.X)
                : 0f;

            if (orbitRadius <= 0.001f)
                orbitRadius = DefaultOrbitRadius;

            var angle = startAngle + input.Speed * input.Elapsed;
            var cos = (float)System.Math.Cos(angle);
            var sin = (float)System.Math.Sin(angle);
            return new Float3(
                input.TargetPosition.X + orbitRadius * cos,
                input.TargetPosition.Y,
                input.TargetPosition.Z + orbitRadius * sin);
        }
    }
}
