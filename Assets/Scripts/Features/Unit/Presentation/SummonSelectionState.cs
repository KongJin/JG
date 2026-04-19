using Features.Unit.Domain;
using Shared.Kernel;
using UnitSpec = Features.Unit.Domain.Unit;

namespace Features.Unit.Presentation
{
    public sealed class SummonSelectionState
    {
        public bool IsActive { get; private set; }
        public UnitSpec SelectedUnit { get; private set; }
        public DomainEntityId OwnerId { get; private set; }
        public int SelectedSlotIndex { get; private set; } = -1;

        public void SetOwner(DomainEntityId ownerId)
        {
            OwnerId = ownerId;
        }

        public void Activate(DomainEntityId ownerId, UnitSpec unitSpec, int slotIndex)
        {
            OwnerId = ownerId;
            SelectedUnit = unitSpec;
            SelectedSlotIndex = slotIndex;
            IsActive = unitSpec != null;
        }

        public void Clear()
        {
            SelectedUnit = null;
            SelectedSlotIndex = -1;
            IsActive = false;
        }
    }
}
