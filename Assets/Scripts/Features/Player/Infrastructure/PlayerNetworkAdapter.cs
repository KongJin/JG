using Features.Combat.Domain;
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
        IPlayerNetworkCommandPort, IPlayerNetworkCallbackPort
    {
        [SerializeField]
        private float _lerpSpeed = 15f;

        private Vector3 _networkPosition;
        private Quaternion _networkRotation;

        private const string HealthKey = "hp";
        private const string MaxHealthKey = "maxHp";
        private const string EnergyKey = "energy";
        private const string MaxEnergyKey = "maxEnergy";
        private const string LifeStateKey = "lifeState";

        public bool IsMine => photonView.IsMine;
        public DomainEntityId StablePlayerId => new DomainEntityId(GetStablePlayerIdValue());

        // IPlayerNetworkCallbackPort
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

            var props = new Hashtable
            {
                { HealthKey, currentHp },
                { MaxHealthKey, maxHp }
            };
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }

        public void SyncEnergy(DomainEntityId targetId, float currentEnergy, float maxEnergy)
        {
            if (photonView == null || !photonView.IsMine) return;

            var props = new Hashtable
            {
                { EnergyKey, currentEnergy },
                { MaxEnergyKey, maxEnergy }
            };
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }

        public void SyncLifeState(DomainEntityId playerId, LifeState state)
        {
            if (photonView == null || !photonView.IsMine) return;

            var props = new Hashtable
            {
                { LifeStateKey, (int)state }
            };
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }

        // TODO: Remove - Skill system compatibility
        public void SyncMana(DomainEntityId targetId, float currentMana, float maxMana)
        {
            // Mana is now Energy - no-op for compatibility
            // Energy is already synced via SyncEnergy
        }

        public override void OnPlayerPropertiesUpdate(Photon.Realtime.Player targetPlayer, Hashtable changedProps)
        {
            if (targetPlayer == null || targetPlayer.IsLocal)
                return;

            if (photonView.Owner != null && targetPlayer.ActorNumber != photonView.Owner.ActorNumber)
                return;

            var playerId = new DomainEntityId("player-" + targetPlayer.ActorNumber);

            if (changedProps.TryGetValue(HealthKey, out var hpRaw) && hpRaw is float hp
                && changedProps.TryGetValue(MaxHealthKey, out var maxHpRaw) && maxHpRaw is float maxHp)
            {
                OnHealthSynced?.Invoke(playerId, hp, maxHp);
            }
            else if (targetPlayer.CustomProperties.TryGetValue(HealthKey, out var hpFallback) && hpFallback is float hpF
                     && changedProps.ContainsKey(HealthKey))
            {
                var mhp = targetPlayer.CustomProperties.TryGetValue(MaxHealthKey, out var mhpRaw) && mhpRaw is float mhpF ? mhpF : 100f;
                OnHealthSynced?.Invoke(playerId, hpF, mhp);
            }

            if (changedProps.TryGetValue(EnergyKey, out var energyRaw) && energyRaw is float energy)
            {
                var maxEnergy = changedProps.TryGetValue(MaxEnergyKey, out var maxEnergyRaw) && maxEnergyRaw is float me
                    ? me
                    : (targetPlayer.CustomProperties.TryGetValue(MaxEnergyKey, out var meFallback) && meFallback is float meF ? meF : 100f);
                OnEnergySynced?.Invoke(playerId, energy, maxEnergy);
            }

            if (changedProps.TryGetValue(LifeStateKey, out var lsRaw) && lsRaw is int lsInt)
            {
                OnLifeStateSynced?.Invoke(playerId, (LifeState)lsInt);
            }
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

            if (props.TryGetValue(HealthKey, out var hpRaw) && hpRaw is float hp
                && props.TryGetValue(MaxHealthKey, out var maxHpRaw) && maxHpRaw is float maxHp)
            {
                OnHealthSynced?.Invoke(playerId, hp, maxHp);
            }

            if (props.TryGetValue(EnergyKey, out var energyRaw) && energyRaw is float energy)
            {
                var maxEnergy = props.TryGetValue(MaxEnergyKey, out var meRaw) && meRaw is float me ? me : 100f;
                OnEnergySynced?.Invoke(playerId, energy, maxEnergy);
            }

            if (props.TryGetValue(LifeStateKey, out var lsRaw) && lsRaw is int lsInt)
            {
                OnLifeStateSynced?.Invoke(playerId, (LifeState)lsInt);
            }
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
