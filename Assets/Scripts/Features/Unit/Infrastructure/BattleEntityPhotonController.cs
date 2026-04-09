using Features.Combat.Application.Events;
using Features.Unit.Application.Events;
using Features.Unit.Domain;
using Photon.Pun;
using Shared.Attributes;
using Shared.EventBus;
using Shared.Kernel;
using Shared.Math;
using UnityEngine;

namespace Features.Unit.Infrastructure
{
    /// <summary>
    /// BattleEntity의 Owner 기반 RPC 동기화.
    /// HP, 위치, 사망 상태를 Owner 클라이언트에서 관리하고 다른 클라이언트에 동기화.
    /// </summary>
    public sealed class BattleEntityPhotonController : MonoBehaviourPunCallbacks, IPunObservable
    {
        [Header("References")]
        [SerializeField] private float _positionSyncInterval = 0.1f;
        [SerializeField] private float _lerpSpeed = 15f;

        private BattleEntity _battleEntity;
        private IEventSubscriber _eventBus;
        private IEventPublisher _publisher;
        private DomainEntityId _ownerId;

        // Network sync state
        private Vector3 _networkPosition;
        private float _lastPositionSync;
        private float _networkCurrentHp;
        private bool _isDead;

        /// <summary>
        /// BattleEntityPrefabSetup에서 BattleEntity를 설정한다.
        /// </summary>
        public void SetBattleEntity(BattleEntity entity, IEventSubscriber subscriber, IEventPublisher publisher, DomainEntityId ownerId)
        {
            _battleEntity = entity;
            _eventBus = subscriber;
            _publisher = publisher;
            _ownerId = ownerId;
            _networkCurrentHp = entity.CurrentHp;

            if (photonView.IsMine)
            {
                InitializeAsOwner();
            }
            else
            {
                InitializeAsRemote();
            }
        }

        private void InitializeAsOwner()
        {
            // Owner: BattleEntityPrefabSetup에서 이미 초기화됨
            // 추가 Owner 전용 로직이 필요하면 여기에 추가
        }

        private void InitializeAsRemote()
        {
            // 원격은 네트워크 상태만 구독
            _eventBus.Subscribe(this, new System.Action<DamageAppliedEvent>(OnDamageApplied));
        }

        private void OnDamageApplied(DamageAppliedEvent e)
        {
            // Owner만 상태 동기화
            if (!photonView.IsMine) return;
            if (!e.TargetId.Equals(_battleEntity.Id)) return;

            _networkCurrentHp = e.RemainingHealth;
            _isDead = e.IsDead;

            // 상태 동기화 (CustomProperties 사용)
            SyncState();

            // 사망 시 UnitDiedEvent 발행 (Owner만)
            if (_isDead)
            {
                _publisher.Publish(new Application.Events.UnitDiedEvent(_battleEntity.Id, _ownerId));
            }
        }

        private void SyncState()
        {
            if (PhotonNetwork.LocalPlayer == null) return;

            var props = new ExitGames.Client.Photon.Hashtable
            {
                { "battle_hp", _networkCurrentHp },
                { "battle_dead", _isDead }
            };

            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }

        private bool _hasReceivedSync;

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                // Owner → 다른 클라이언트로 상태 전송
                stream.SendNext(transform.position);
                stream.SendNext(_networkCurrentHp);
                stream.SendNext(_isDead);
            }
            else
            {
                // 다른 클라이언트 → 원격 상태 수신
                _networkPosition = (Vector3)stream.ReceiveNext();
                _networkCurrentHp = (float)stream.ReceiveNext();
                _isDead = (bool)stream.ReceiveNext();

                // 첫 수신 시 로컬 BattleEntity 도메인에 반영
                if (!_hasReceivedSync && _battleEntity != null)
                {
                    _battleEntity.SetHpFromNetwork(_networkCurrentHp);
                    _hasReceivedSync = true;
                }
            }
        }

        private void Update()
        {
            if (photonView.IsMine)
            {
                // Owner: 위치 동기화
                if (Time.time - _lastPositionSync >= _positionSyncInterval)
                {
                    _lastPositionSync = Time.time;
                    // 위치는 자동으로 PhotonNetwork.Instantiate 시 설정됨
                }
            }
            else
            {
                // Remote: 네트워크 위치로 Lerping
                transform.position = Vector3.Lerp(transform.position, _networkPosition, Time.deltaTime * _lerpSpeed);
            }
        }

        private void OnDestroy()
        {
            _eventBus?.UnsubscribeAll(this);
        }
    }
}
