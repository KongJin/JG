using Features.Skill.Domain;
using Shared.Kernel;
using Shared.Math;

namespace Features.Skill.Application.Events
{
    public readonly struct ZoneRequestedEvent
    {
        public ZoneRequestedEvent(DomainEntityId skillId, DomainEntityId casterId, SkillSpec spec, Float3 position, Float3 direction, float allyDamageScale = 1f)
        {
            SkillId = skillId;
            CasterId = casterId;
            Spec = spec;
            Position = position;
            Direction = direction;
            AllyDamageScale = allyDamageScale;
        }

        public DomainEntityId SkillId { get; }
        public DomainEntityId CasterId { get; }
        public SkillSpec Spec { get; }
        public Float3 Position { get; }
        public Float3 Direction { get; }
        public float AllyDamageScale { get; }
    }
}
