using Features.Wave.Application.Ports;
using Photon.Pun;
using UnityEngine;

namespace Features.Enemy.Infrastructure
{
    public sealed class EnemyAiAdapter : MonoBehaviour
    {
        private float _moveSpeed;
        private IPlayerPositionQuery _playerQuery;
        private bool _initialized;

        public void Initialize(float moveSpeed, IPlayerPositionQuery playerQuery)
        {
            _moveSpeed = moveSpeed;
            _playerQuery = playerQuery;
            _initialized = true;
        }

        private void Update()
        {
            if (!_initialized) return;
            if (!PhotonNetwork.IsMasterClient) return;

            var pos = transform.position;
            var (tx, ty, tz) = _playerQuery.GetNearestPlayerPosition(pos.x, pos.y, pos.z);
            var target = new Vector3(tx, ty, tz);
            var diff = target - pos;

            if (diff.sqrMagnitude < 0.01f) return;

            var dir = diff.normalized;
            transform.position += dir * _moveSpeed * Time.deltaTime;

            var lookDir = new Vector3(dir.x, 0f, dir.z);
            if (lookDir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(lookDir);
        }
    }
}
