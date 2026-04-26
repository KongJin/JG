using System.Collections.Generic;
using Features.Combat.Application.Ports;
using Features.Enemy.Application.Ports;
using Features.Wave.Domain;
using UnityEngine;

namespace Features.Wave.Infrastructure
{
    public sealed class PlayerPositionQueryAdapter : MonoBehaviour, IPlayerPositionQuery
    {
        private readonly List<Transform> _playerTransforms = new List<Transform>();

        public void RegisterPlayer(Transform playerTransform)
        {
            if (!_playerTransforms.Contains(playerTransform))
                _playerTransforms.Add(playerTransform);
        }

        public (float x, float y, float z) GetNearestPlayerPosition(float fromX, float fromY, float fromZ)
        {
            if (TryGetNearestPlayerPosition(fromX, fromY, fromZ, out var x, out var y, out var z))
                return (x, y, z);

            return (fromX, fromY, fromZ);
        }

        public bool TryGetNearestPlayerPosition(
            float fromX,
            float fromY,
            float fromZ,
            out float tx,
            out float ty,
            out float tz)
        {
            var from = new Vector3(fromX, fromY, fromZ);
            var minDist = float.MaxValue;
            tx = ty = tz = 0f;
            var found = false;

            for (var i = _playerTransforms.Count - 1; i >= 0; i--)
            {
                var t = _playerTransforms[i];
                if (t == null)
                {
                    _playerTransforms.RemoveAt(i);
                    continue;
                }

                var dist = (t.position - from).sqrMagnitude;
                if (dist < minDist)
                {
                    minDist = dist;
                    found = true;
                    tx = t.position.x;
                    ty = t.position.y;
                    tz = t.position.z;
                }
            }

            return found;
        }

        public bool TryGetNearestPlayerWithinHorizontalRadius(
            float fromX,
            float fromY,
            float fromZ,
            float radius,
            out float tx,
            out float ty,
            out float tz)
        {
            var r2 = radius * radius;
            var minXZ = float.MaxValue;
            tx = ty = tz = 0f;
            var found = false;

            for (var i = _playerTransforms.Count - 1; i >= 0; i--)
            {
                var t = _playerTransforms[i];
                if (t == null)
                {
                    _playerTransforms.RemoveAt(i);
                    continue;
                }

                var dx = t.position.x - fromX;
                var dz = t.position.z - fromZ;
                var xz2 = dx * dx + dz * dz;
                if (xz2 > r2)
                    continue;

                if (xz2 < minXZ)
                {
                    minXZ = xz2;
                    found = true;
                    tx = t.position.x;
                    ty = t.position.y;
                    tz = t.position.z;
                }
            }

            return found;
        }
    }

    public sealed class ObjectiveCoreCombatTargetProvider : ICombatTargetProvider
    {
        private readonly ObjectiveCore _core;

        public ObjectiveCoreCombatTargetProvider(ObjectiveCore core)
        {
            _core = core;
        }

        public float GetDefense() => _core.Defense;

        public float GetCurrentHealth() => _core.CurrentHp;

        public CombatTargetDamageResult ApplyDamage(float damage)
        {
            if (_core.IsDestroyed)
                return new CombatTargetDamageResult(_core.CurrentHp, true, false);

            var remaining = _core.TakeDamage(damage);
            return new CombatTargetDamageResult(remaining, _core.IsDestroyed, false);
        }
    }
}
