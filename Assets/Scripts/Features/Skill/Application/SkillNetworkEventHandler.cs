using Features.Projectile.Application.Events;
using Features.Projectile.Domain;
using Features.Projectile.Domain.Hit;
using Features.Projectile.Domain.Trajectory;
using Features.Skill.Application.Events;
using Features.Skill.Application.Ports;
using Features.Skill.Domain;
using Features.Skill.Domain.Delivery;
using Features.Status.Application.Events;
using Shared.EventBus;
using Shared.Kernel;
using Shared.Math;

namespace Features.Skill.Application
{
    public sealed class SkillNetworkEventHandler
    {
        private readonly IEventPublisher _publisher;

        public SkillNetworkEventHandler(
            IEventPublisher publisher,
            ISkillNetworkCallbackPort networkCallbacks
        )
        {
            _publisher = publisher;
            networkCallbacks.OnSkillCasted = HandleSkillCasted;
        }

        private void HandleSkillCasted(SkillCastNetworkData data)
        {
            var spec = new SkillSpec(
                data.Damage, 0f, data.Range, data.Duration, data.ProjectileCount, data.StatusPayload, data.GameplayTags);

            switch (data.DeliveryType)
            {
                case DeliveryType.Projectile:
                    var projectileSpec = new ProjectileSpec(
                        (TrajectoryType)data.TrajectoryType,
                        (HitType)data.HitType,
                        data.Speed, data.Radius);
                    PublishProjectiles(data, projectileSpec, data.AllyDamageScale);
                    break;
                case DeliveryType.Zone:
                    PublishZones(data, spec, data.AllyDamageScale);
                    break;
                case DeliveryType.Targeted:
                    _publisher.Publish(
                        new TargetedRequestedEvent(
                            data.SkillId,
                            data.CasterId,
                            spec,
                            data.Position,
                            data.Direction,
                            data.TargetPosition
                        )
                    );
                    break;
                case DeliveryType.Self:
                    PublishSelf(data, spec);
                    break;
            }

            _publisher.Publish(new SkillCastedEvent(data.SkillId, data.CasterId, data.SlotIndex, spec));
        }

        private void PublishZones(SkillCastNetworkData data, SkillSpec spec, float allyDamageScale)
        {
            var count = data.ProjectileCount;
            if (count <= 1)
            {
                _publisher.Publish(new ZoneRequestedEvent(data.SkillId, data.CasterId, spec, data.Position, data.Direction, allyDamageScale));
                return;
            }

            var ringRadius = data.Range * 0.3f;
            var angleStep = 2.0 * System.Math.PI / count;
            for (var i = 0; i < count; i++)
            {
                var angle = angleStep * i;
                var offsetX = (float)(ringRadius * System.Math.Cos(angle));
                var offsetZ = (float)(ringRadius * System.Math.Sin(angle));
                var pos = new Float3(data.Position.X + offsetX, data.Position.Y, data.Position.Z + offsetZ);
                _publisher.Publish(new ZoneRequestedEvent(data.SkillId, data.CasterId, spec, pos, data.Direction, allyDamageScale));
            }
        }

        private void PublishSelf(SkillCastNetworkData data, SkillSpec spec)
        {
            _publisher.Publish(new SelfRequestedEvent(data.SkillId, data.CasterId, spec, data.Position));

            if (!spec.StatusPayload.HasEffect)
                return;

            _publisher.Publish(
                new StatusApplyRequestedEvent(
                    data.CasterId,
                    spec.StatusPayload.Type,
                    spec.StatusPayload.Magnitude,
                    spec.StatusPayload.Duration,
                    data.CasterId,
                    spec.StatusPayload.TickInterval
                )
            );
        }

        private void PublishProjectiles(SkillCastNetworkData data, ProjectileSpec projectileSpec, float allyDamageScale)
        {
            var count = data.ProjectileCount;
            if (count <= 1)
            {
                _publisher.Publish(new ProjectileRequestedEvent(
                    data.CasterId, projectileSpec, data.Damage, DamageType.Magical,
                    data.Position, data.Direction, data.StatusPayload, allyDamageScale));
                return;
            }

            const float spreadAngleDeg = 10f;
            var totalSpread = spreadAngleDeg * (count - 1);
            var startAngle = -totalSpread * 0.5f;
            var dx = data.Direction.X;
            var dz = data.Direction.Z;

            for (var i = 0; i < count; i++)
            {
                var angleDeg = startAngle + spreadAngleDeg * i;
                var rad = angleDeg * (System.Math.PI / 180.0);
                var cos = (float)System.Math.Cos(rad);
                var sin = (float)System.Math.Sin(rad);
                var rotatedDir = new Float3(dx * cos - dz * sin, data.Direction.Y, dx * sin + dz * cos);

                _publisher.Publish(new ProjectileRequestedEvent(
                    data.CasterId, projectileSpec, data.Damage, DamageType.Magical,
                    data.Position, rotatedDir, data.StatusPayload, allyDamageScale));
            }
        }
    }
}
