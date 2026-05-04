using Features.Player.Application.Ports;
using Features.Player.Domain;
using Photon.Pun;
using Shared.Kernel;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace Features.Player.Infrastructure
{
    [RequireComponent(typeof(PhotonView))]
    public sealed class PlayerNetworkAdapter : MonoBehaviourPunCallbacks, IPunObservable,
        IPlayerNetworkPort
    {
        [SerializeField]
        private float _lerpSpeed = 15f;

        private Vector3 _networkPosition;
        private Quaternion _networkRotation;

        public bool IsMine => photonView.IsMine;
        public DomainEntityId StablePlayerId => new DomainEntityId(GetStablePlayerIdValue());

        public System.Action<DomainEntityId, float, DamageType, DomainEntityId> OnRemoteDamaged { set; private get; }
        public System.Action<DomainEntityId> OnRemoteRespawned { get; set; }
        public System.Action<DomainEntityId, float, float> OnHealthSynced { get; set; }
        public System.Action<DomainEntityId, float, float> OnEnergySynced { get; set; }
        public System.Action<DomainEntityId, LifeState> OnLifeStateSynced { get; set; }

        private void Update()
        {
            if (IsMine) return;

            transform.position = Vector3.Lerp(transform.position, _networkPosition, Time.deltaTime * _lerpSpeed);
            transform.rotation = Quaternion.Lerp(transform.rotation, _networkRotation, Time.deltaTime * _lerpSpeed);
        }

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                stream.SendNext(transform.position);
                stream.SendNext(transform.rotation);
            }
            else
            {
                _networkPosition = (Vector3)stream.ReceiveNext();
                _networkRotation = (Quaternion)stream.ReceiveNext();
            }
        }

        public void SendDamage(DomainEntityId targetId, float damage, DamageType damageType, DomainEntityId attackerId)
        {
            photonView.RPC(nameof(RPC_ApplyDamage), RpcTarget.Others,
                targetId.Value, damage, (int)damageType, attackerId.Value);
        }

        public void SendRespawn(DomainEntityId targetId)
        {
            photonView.RPC(nameof(RPC_PlayerRespawn), RpcTarget.Others, targetId.Value);
        }

        public void SyncHealth(DomainEntityId targetId, float currentHp, float maxHp)
        {
            if (photonView == null || !photonView.IsMine) return;

            PhotonNetwork.LocalPlayer.SetCustomProperties(
                PlayerNetworkPropertyReader.CreateHealthProperties(currentHp, maxHp));
        }

        public void SyncEnergy(DomainEntityId targetId, float currentEnergy, float maxEnergy)
        {
            if (photonView == null || !photonView.IsMine) return;

            PhotonNetwork.LocalPlayer.SetCustomProperties(
                PlayerNetworkPropertyReader.CreateEnergyProperties(currentEnergy, maxEnergy));
        }

        public void SyncLifeState(DomainEntityId playerId, LifeState state)
        {
            if (photonView == null || !photonView.IsMine) return;

            PhotonNetwork.LocalPlayer.SetCustomProperties(
                PlayerNetworkPropertyReader.CreateLifeStateProperties(state));
        }

        public override void OnPlayerPropertiesUpdate(Photon.Realtime.Player targetPlayer, Hashtable changedProps)
        {
            if (targetPlayer == null || targetPlayer.IsLocal)
                return;

            if (photonView.Owner != null && targetPlayer.ActorNumber != photonView.Owner.ActorNumber)
                return;

            var playerId = new DomainEntityId("player-" + targetPlayer.ActorNumber);

            if (PlayerNetworkPropertyReader.TryReadHealthChange(
                    changedProps,
                    targetPlayer.CustomProperties,
                    out var health))
                OnHealthSynced?.Invoke(playerId, health.Current, health.Max);

            if (PlayerNetworkPropertyReader.TryReadEnergyChange(
                    changedProps,
                    targetPlayer.CustomProperties,
                    out var energy))
                OnEnergySynced?.Invoke(playerId, energy.Current, energy.Max);

            if (PlayerNetworkPropertyReader.TryReadLifeState(changedProps, out var lifeState))
                OnLifeStateSynced?.Invoke(playerId, lifeState);
        }

        [PunRPC]
        private void RPC_ApplyDamage(string targetIdValue, float damage, int damageTypeInt, string attackerIdValue)
        {
            if (string.IsNullOrEmpty(targetIdValue))
            {
                Debug.LogWarning($"[{nameof(PlayerNetworkAdapter)}] RPC_ApplyDamage: targetIdValue is null or empty.");
                return;
            }
            if (float.IsNaN(damage) || float.IsInfinity(damage) || damage < 0f)
            {
                Debug.LogWarning($"[{nameof(PlayerNetworkAdapter)}] RPC_ApplyDamage: invalid damage value {damage}.");
                return;
            }
            if (!System.Enum.IsDefined(typeof(DamageType), damageTypeInt))
            {
                Debug.LogWarning($"[{nameof(PlayerNetworkAdapter)}] RPC_ApplyDamage: invalid damageType {damageTypeInt}.");
                return;
            }

            var targetId = new DomainEntityId(targetIdValue);
            var attackerId = string.IsNullOrEmpty(attackerIdValue) ? default : new DomainEntityId(attackerIdValue);
            var damageType = (DamageType)damageTypeInt;
            OnRemoteDamaged?.Invoke(targetId, damage, damageType, attackerId);
        }

        [PunRPC]
        private void RPC_PlayerRespawn(string targetIdValue)
        {
            if (string.IsNullOrEmpty(targetIdValue))
            {
                Debug.LogWarning($"[{nameof(PlayerNetworkAdapter)}] RPC_PlayerRespawn: targetIdValue is null or empty.");
                return;
            }

            var targetId = new DomainEntityId(targetIdValue);
            OnRemoteRespawned?.Invoke(targetId);
        }

        public void HydrateFromProperties()
        {
            if (photonView == null || photonView.Owner == null)
                return;

            var props = photonView.Owner.CustomProperties;
            var playerId = StablePlayerId;

            if (PlayerNetworkPropertyReader.TryReadHydratedHealth(props, out var health))
                OnHealthSynced?.Invoke(playerId, health.Current, health.Max);

            if (PlayerNetworkPropertyReader.TryReadHydratedEnergy(props, out var energy))
                OnEnergySynced?.Invoke(playerId, energy.Current, energy.Max);

            if (PlayerNetworkPropertyReader.TryReadLifeState(props, out var lifeState))
                OnLifeStateSynced?.Invoke(playerId, lifeState);
        }

        private string GetStablePlayerIdValue()
        {
            if (photonView != null)
            {
                if (photonView.Owner != null)
                    return "player-" + photonView.Owner.ActorNumber;

                if (photonView.OwnerActorNr > 0)
                    return "player-" + photonView.OwnerActorNr;

                if (photonView.ViewID > 0)
                    return "player-view-" + photonView.ViewID;
            }

            return "player-local-" + GetInstanceID();
        }
    }
}
