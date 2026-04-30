using Shared.EventBus;

namespace Features.Player
{
    public interface IGameSceneEventBusConsumer
    {
        void Initialize(EventBus eventBus);
    }
}
