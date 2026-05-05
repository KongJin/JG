using Photon.Pun;
using Shared.Kernel;
using UnityEngine;

namespace Features.Enemy.Infrastructure
{
    [RequireComponent(typeof(PhotonView))]
    public sealed class EnemyNetworkAdapter : MonoBehaviourPun, IPunObservable
    {
        [SerializeField] private float lerpSpeed = 15f;

        private Vector3 _networkPosition;
        private Quaternion _networkRotation;

        public DomainEntityId StableEnemyId => new DomainEntityId("enemy-" + photonView.ViewID);

        private void Update()
        {
            if (photonView.IsMine) return;

            transform.position = Vector3.Lerp(transform.position, _networkPosition, Time.deltaTime * lerpSpeed);
            transform.rotation = Quaternion.Lerp(transform.rotation, _networkRotation, Time.deltaTime * lerpSpeed);
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
    }
}
