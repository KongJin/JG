using Shared.EventBus;

namespace Features.Player
{
    public interface IBattleSceneEventBusConsumer
    {
        void Initialize(EventBus eventBus);
    }
}
