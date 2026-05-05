using Features.Unit.Application.Ports;
using Photon.Pun;
using Shared.Kernel;

namespace Features.Unit.Infrastructure
{
    /// <summary>
    /// BattleEntity ID를 Photon 오브젝트 파괴로 연결한다.
    /// </summary>
    public sealed class BattleEntityDespawnAdapter : IBattleEntityDespawnPort
    {
        public void Despawn(DomainEntityId entityId)
        {
            if (!EntityIdHolder.TryGet(entityId, out var holder))
            {
                return;
            }

            var gameObject = holder.gameObject;
            // csharp-guardrails: allow-null-defense
            if (gameObject == null)
            {
                return;
            }

            PhotonNetwork.Destroy(gameObject);
        }
    }
}
