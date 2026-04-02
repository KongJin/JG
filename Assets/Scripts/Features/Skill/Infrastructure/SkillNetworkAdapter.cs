using System;
using System.IO;
using ExitGames.Client.Photon;
using Features.Skill.Application.Ports;
using Features.Projectile.Domain.Hit;
using Features.Projectile.Domain.Trajectory;
using Features.Skill.Domain.Delivery;
using Features.Status.Domain;
using Photon.Pun;
using Shared.Kernel;
using Shared.Math;
using UnityEngine;

namespace Features.Skill.Infrastructure
{
    public sealed class SkillNetworkAdapter : MonoBehaviourPun,
        ISkillNetworkCommandPort, ISkillNetworkCallbackPort
    {
        private const string Tag = nameof(SkillNetworkAdapter);

        private const string SkillsReadyKey = "skillsReady";

        public Action<SkillCastNetworkData> OnSkillCasted { get; set; }

        public void SyncSkillsReady()
        {
            PhotonNetwork.LocalPlayer.SetCustomProperties(
                new Hashtable { { SkillsReadyKey, true } });
        }

        public static bool IsPlayerSkillsReady(Photon.Realtime.Player player)
        {
            return player.CustomProperties.TryGetValue(SkillsReadyKey, out var val) && val is true;
        }

        public void SendSkillCasted(SkillCastNetworkData data)
        {
            photonView.RPC(nameof(RPC_SkillCasted), RpcTarget.All, Serialize(data));
        }

        [PunRPC]
        private void RPC_SkillCasted(byte[] payload)
        {
            if (!TryDeserialize(payload, out var data))
                return;

            OnSkillCasted?.Invoke(data);
        }

        private static byte[] Serialize(SkillCastNetworkData data)
        {
            using (var ms = new MemoryStream(128))
            using (var w = new BinaryWriter(ms))
            {
                w.Write(data.SkillId.Value);
                w.Write(data.CasterId.Value);
                w.Write(data.SlotIndex);
                w.Write(data.Damage);
                w.Write(data.Duration);
                w.Write(data.Range);
                w.Write((int)data.DeliveryType);
                w.Write(data.TrajectoryType);
                w.Write(data.HitType);
                w.Write(data.Speed);
                w.Write(data.Radius);
                w.Write(data.Position.X);
                w.Write(data.Position.Y);
                w.Write(data.Position.Z);
                w.Write(data.Direction.X);
                w.Write(data.Direction.Y);
                w.Write(data.Direction.Z);
                w.Write(data.TargetPosition.X);
                w.Write(data.TargetPosition.Y);
                w.Write(data.TargetPosition.Z);
                w.Write(data.StatusPayload.HasEffect);
                w.Write((int)data.StatusPayload.Type);
                w.Write(data.StatusPayload.Magnitude);
                w.Write(data.StatusPayload.Duration);
                w.Write(data.StatusPayload.TickInterval);
                w.Write(data.ProjectileCount);
                w.Write(data.AllyDamageScale);
                return ms.ToArray();
            }
        }

        private static bool TryDeserialize(byte[] payload, out SkillCastNetworkData data)
        {
            data = default;

            if (payload == null || payload.Length == 0)
            {
                Debug.LogWarning($"[{Tag}] Received null or empty payload.");
                return false;
            }

            try
            {
                using (var ms = new MemoryStream(payload))
                using (var r = new BinaryReader(ms))
                {
                    var skillIdValue = r.ReadString();
                    if (string.IsNullOrEmpty(skillIdValue))
                    {
                        Debug.LogWarning($"[{Tag}] SkillId is null or empty.");
                        return false;
                    }

                    var casterIdValue = r.ReadString();
                    if (string.IsNullOrEmpty(casterIdValue))
                    {
                        Debug.LogWarning($"[{Tag}] CasterId is null or empty.");
                        return false;
                    }

                    var slotIndex = r.ReadInt32();
                    if (slotIndex < 0 || slotIndex >= Domain.SkillBar.SlotCount)
                    {
                        Debug.LogWarning($"[{Tag}] Invalid slotIndex: {slotIndex}");
                        return false;
                    }

                    var damage = r.ReadSingle();
                    var duration = r.ReadSingle();
                    var range = r.ReadSingle();

                    if (!IsFinite(damage) || !IsFinite(duration) || !IsFinite(range))
                    {
                        Debug.LogWarning($"[{Tag}] Non-finite numeric field (damage={damage}, duration={duration}, range={range}).");
                        return false;
                    }

                    if (damage < 0 || duration < 0 || range < 0)
                    {
                        Debug.LogWarning($"[{Tag}] Negative numeric field (damage={damage}, duration={duration}, range={range}).");
                        return false;
                    }

                    var deliveryTypeInt = r.ReadInt32();
                    if (!Enum.IsDefined(typeof(DeliveryType), deliveryTypeInt))
                    {
                        Debug.LogWarning($"[{Tag}] Invalid DeliveryType: {deliveryTypeInt}");
                        return false;
                    }
                    var deliveryType = (DeliveryType)deliveryTypeInt;

                    var trajectoryType = r.ReadInt32();
                    if (!Enum.IsDefined(typeof(TrajectoryType), trajectoryType))
                    {
                        Debug.LogWarning($"[{Tag}] Invalid TrajectoryType: {trajectoryType}");
                        return false;
                    }

                    var hitType = r.ReadInt32();
                    if (!Enum.IsDefined(typeof(HitType), hitType))
                    {
                        Debug.LogWarning($"[{Tag}] Invalid HitType: {hitType}");
                        return false;
                    }

                    var speed = r.ReadSingle();
                    var radius = r.ReadSingle();

                    if (!IsFinite(speed) || !IsFinite(radius))
                    {
                        Debug.LogWarning($"[{Tag}] Non-finite numeric field (speed={speed}, radius={radius}).");
                        return false;
                    }

                    if (speed < 0 || radius < 0)
                    {
                        Debug.LogWarning($"[{Tag}] Negative numeric field (speed={speed}, radius={radius}).");
                        return false;
                    }

                    var position = new Float3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
                    var direction = new Float3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
                    var targetPosition = new Float3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());

                    if (!IsFiniteFloat3(position) || !IsFiniteFloat3(direction) || !IsFiniteFloat3(targetPosition))
                    {
                        Debug.LogWarning($"[{Tag}] Non-finite vector field.");
                        return false;
                    }

                    var hasEffect = r.ReadBoolean();
                    var statusTypeInt = r.ReadInt32();
                    var statusMagnitude = r.ReadSingle();
                    var statusDuration = r.ReadSingle();
                    var statusTickInterval = r.ReadSingle();

                    if (!IsFinite(statusMagnitude) || !IsFinite(statusDuration) || !IsFinite(statusTickInterval))
                    {
                        Debug.LogWarning($"[{Tag}] Non-finite status field (magnitude={statusMagnitude}, duration={statusDuration}, tickInterval={statusTickInterval}).");
                        return false;
                    }

                    StatusPayload statusPayload;
                    if (hasEffect)
                    {
                        if (!Enum.IsDefined(typeof(StatusType), statusTypeInt))
                        {
                            Debug.LogWarning($"[{Tag}] Invalid StatusType: {statusTypeInt}");
                            return false;
                        }
                        statusPayload = StatusPayload.Create(
                            (StatusType)statusTypeInt, statusMagnitude, statusDuration, statusTickInterval);
                    }
                    else
                    {
                        statusPayload = StatusPayload.None;
                    }

                    var projectileCount = r.ReadInt32();
                    if (projectileCount < 1)
                    {
                        Debug.LogWarning($"[{Tag}] Invalid projectileCount: {projectileCount}");
                        return false;
                    }

                    var allyDamageScale = r.ReadSingle();
                    if (!IsFinite(allyDamageScale))
                    {
                        Debug.LogWarning($"[{Tag}] Non-finite allyDamageScale: {allyDamageScale}");
                        return false;
                    }

                    data = new SkillCastNetworkData(
                        new DomainEntityId(skillIdValue),
                        new DomainEntityId(casterIdValue),
                        slotIndex,
                        damage, duration, range, deliveryType,
                        trajectoryType, hitType, speed, radius,
                        position, direction, targetPosition,
                        statusPayload, projectileCount, allyDamageScale);
                    return true;
                }
            }
            catch (EndOfStreamException)
            {
                Debug.LogWarning($"[{Tag}] Payload too short — unexpected end of stream.");
                return false;
            }
            catch (IOException ex)
            {
                Debug.LogWarning($"[{Tag}] IO error during deserialization: {ex.Message}");
                return false;
            }
        }

        private static bool IsFinite(float v)
        {
            return !float.IsNaN(v) && !float.IsInfinity(v);
        }

        private static bool IsFiniteFloat3(Float3 v)
        {
            return IsFinite(v.X) && IsFinite(v.Y) && IsFinite(v.Z);
        }
    }
}
