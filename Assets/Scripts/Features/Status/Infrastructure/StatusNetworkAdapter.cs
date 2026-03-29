using System;
using Features.Status.Application.Ports;
using Features.Status.Domain;
using Photon.Pun;
using Shared.Kernel;

namespace Features.Status.Infrastructure
{
    public sealed class StatusNetworkAdapter : MonoBehaviourPun,
        IStatusNetworkCommandPort, IStatusNetworkCallbackPort
    {
        // IStatusNetworkCallbackPort
        public Action<DomainEntityId, StatusType, float, float, DomainEntityId, float> OnRemoteStatusApplied
        {
            get; set;
        }

        public Action<DomainEntityId, float, DomainEntityId> OnRemoteTickDamage { get; set; }

        // IStatusNetworkCommandPort
        public void SendApplyStatus(
            DomainEntityId targetId,
            StatusType type,
            float magnitude,
            float duration,
            DomainEntityId sourceId,
            float tickInterval)
        {
            photonView.RPC(
                nameof(RPC_ApplyStatus),
                RpcTarget.Others,
                targetId.Value,
                (int)type,
                magnitude,
                duration,
                sourceId.Value,
                tickInterval);
        }

        public void SendTickDamage(DomainEntityId targetId, float damage, DomainEntityId sourceId)
        {
            photonView.RPC(
                nameof(RPC_TickDamage),
                RpcTarget.Others,
                targetId.Value,
                damage,
                sourceId.Value);
        }

        [PunRPC]
        private void RPC_ApplyStatus(
            string targetIdValue,
            int typeInt,
            float magnitude,
            float duration,
            string sourceIdValue,
            float tickInterval)
        {
            var targetId = new DomainEntityId(targetIdValue);
            var sourceId = new DomainEntityId(sourceIdValue);
            var type = (StatusType)typeInt;
            OnRemoteStatusApplied?.Invoke(targetId, type, magnitude, duration, sourceId, tickInterval);
        }

        [PunRPC]
        private void RPC_TickDamage(string targetIdValue, float damage, string sourceIdValue)
        {
            var targetId = new DomainEntityId(targetIdValue);
            var sourceId = new DomainEntityId(sourceIdValue);
            OnRemoteTickDamage?.Invoke(targetId, damage, sourceId);
        }
    }
}
