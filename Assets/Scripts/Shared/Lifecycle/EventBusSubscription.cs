using Shared.EventBus;

namespace Shared.Lifecycle
{
    public static class EventBusSubscription
    {
        public static DelegateDisposable ForOwner(IEventSubscriber eventBus, object owner)
        {
            return new DelegateDisposable(() => eventBus?.UnsubscribeAll(owner));
        }
    }
}
