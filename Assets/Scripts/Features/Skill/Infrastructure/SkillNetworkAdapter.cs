using Features.Skill.Application.Ports;
using Features.Skill.Domain.Delivery;
using Features.Status.Domain;
using Photon.Pun;
using Shared.Kernel;
using Shared.Math;

namespace Features.Skill.Infrastructure
{
    public sealed class SkillNetworkAdapter : MonoBehaviourPun,
        ISkillNetworkCommandPort, ISkillNetworkCallbackPort
    {
        public System.Action<SkillCastNetworkData> OnSkillCasted { get; set; }

        public void SendSkillCasted(SkillCastNetworkData data)
        {
            var sp = data.StatusPayload;
            photonView.RPC(nameof(RPC_SkillCasted), RpcTarget.All,
                data.SkillId.Value,
                data.CasterId.Value,
                data.SlotIndex,
                (int)data.DeliveryType,
                data.Damage, data.Duration, data.Range,
                data.TrajectoryType, data.HitType,
                data.Speed, data.Radius,
                data.Position.X, data.Position.Y, data.Position.Z,
                data.Direction.X, data.Direction.Y, data.Direction.Z,
                data.TargetPosition.X, data.TargetPosition.Y, data.TargetPosition.Z,
                sp.HasEffect, (int)sp.Type, sp.Magnitude, sp.Duration, sp.TickInterval,
                data.ProjectileCount);
        }

        [PunRPC]
        private void RPC_SkillCasted(
            string skillId, string casterId,
            int slotIndex, int deliveryType,
            float damage, float duration, float range,
            int trajectoryType, int hitType,
            float speed, float radius,
            float posX, float posY, float posZ,
            float dirX, float dirY, float dirZ,
            float targetPosX, float targetPosY, float targetPosZ,
            bool hasStatus, int statusType, float statusMag, float statusDur, float statusTick,
            int projectileCount)
        {
            var statusPayload = hasStatus
                ? StatusPayload.Create((StatusType)statusType, statusMag, statusDur, statusTick)
                : StatusPayload.None;

            var data = new SkillCastNetworkData(
                new DomainEntityId(skillId),
                new DomainEntityId(casterId),
                slotIndex,
                damage, duration, range,
                (DeliveryType)deliveryType,
                trajectoryType, hitType, speed, radius,
                new Float3(posX, posY, posZ),
                new Float3(dirX, dirY, dirZ),
                new Float3(targetPosX, targetPosY, targetPosZ),
                statusPayload,
                projectileCount);

            OnSkillCasted?.Invoke(data);
        }
    }
}
