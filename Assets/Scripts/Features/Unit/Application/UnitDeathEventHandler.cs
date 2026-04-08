using Features.Unit.Application.Events;
using Photon.Pun;
using Shared.EventBus;
using Shared.Kernel;

namespace Features.Unit.Application
{
    /// <summary>
    /// BattleEntity 사망 처리 핸들러.
    /// UnitDiedEvent를 받아 Photon 오브젝트를 파괴한다.
    /// </summary>
    public sealed class UnitDeathEventHandler
    {
        private readonly IEventSubscriber _subscriber;

        public UnitDeathEventHandler(IEventSubscriber subscriber)
        {
            _subscriber = subscriber;
            _subscriber.Subscribe(this, new System.Action<UnitDiedEvent>(OnUnitDied));
        }

        private void OnUnitDied(UnitDiedEvent e)
        {
            // EntityIdHolder에서 Transform을 찾아 PhotonNetwork.Destroy
            if (EntityIdHolder.TryGet(e.EntityId, out var holder))
            {
                var go = holder.gameObject;
                if (go != null)
                {
                    PhotonNetwork.Destroy(go);
                }
            }
        }
    }
}
