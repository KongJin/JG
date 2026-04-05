using Features.Enemy.Application;
using Features.Enemy.Application.Ports;
using Features.Enemy.Domain;
using Photon.Pun;
using UnityEngine;

namespace Features.Enemy.Infrastructure
{
    public sealed class EnemyAiAdapter : MonoBehaviour
    {
        private float _moveSpeed;
        private EnemySpec _spec;
        private IPlayerPositionQuery _playerQuery;
        private ICoreObjectiveQuery _coreQuery;
        private bool _initialized;

        public void Initialize(EnemySpec spec, IPlayerPositionQuery playerQuery, ICoreObjectiveQuery coreQuery)
        {
            _spec = spec;
            _moveSpeed = spec.MoveSpeed;
            _playerQuery = playerQuery;
            _coreQuery = coreQuery;
            _initialized = true;
        }

        private void Update()
        {
            if (!_initialized) return;
            if (!PhotonNetwork.IsMasterClient) return;

            var pos = transform.position;
            if (!EnemyMoveTargetResolver.TryGetMoveDestination(
                    _spec,
                    pos.x,
                    pos.y,
                    pos.z,
                    _playerQuery,
                    _coreQuery,
                    out var isCoreTarget,
                    out var tx,
                    out var ty,
                    out var tz))
            {
                Debug.LogWarning("[EnemyAiAdapter] Move destination could not be resolved.", this);
                return;
            }

            var target = new Vector3(tx, ty, tz);
            var diff = target - pos;
            var horizontal = new Vector3(diff.x, 0f, diff.z);

            if (isCoreTarget && _spec.StopDistance > 0f)
            {
                var stopSq = _spec.StopDistance * _spec.StopDistance;
                if (horizontal.sqrMagnitude <= stopSq)
                    return;
            }
            else if (diff.sqrMagnitude < 0.01f)
            {
                return;
            }

            var dir = diff.normalized;
            transform.position += dir * _moveSpeed * Time.deltaTime;

            var lookDir = new Vector3(dir.x, 0f, dir.z);
            if (lookDir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(lookDir);
        }
    }
}
