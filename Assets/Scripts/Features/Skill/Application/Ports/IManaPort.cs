using Shared.Kernel;

namespace Features.Skill.Application.Ports
{
    public interface IManaPort
    {
        bool TrySpendMana(DomainEntityId casterId, float cost);
        float GetCurrentMana(DomainEntityId casterId);
    }
}
