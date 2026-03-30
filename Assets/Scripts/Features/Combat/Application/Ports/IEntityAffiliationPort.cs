using Features.Combat.Domain;
using Shared.Kernel;

namespace Features.Combat.Application.Ports
{
    public interface IEntityAffiliationPort
    {
        RelationshipType GetRelationship(DomainEntityId attackerId, DomainEntityId targetId);
    }
}
