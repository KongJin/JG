using System.Collections.Generic;
using UnityEngine;

namespace Features.Wave.Infrastructure
{
    /// <summary>
    /// Tracks BattleEntity transforms for enemy AI targeting queries.
    /// Registered by BattleSceneRoot when BattleEntities are spawned.
    /// </summary>
    public sealed class UnitPositionQueryAdapter : MonoBehaviour
    {
        private readonly List<Transform> _unitTransforms = new List<Transform>();

        public void RegisterUnit(Transform unitTransform)
        {
            if (!_unitTransforms.Contains(unitTransform))
                _unitTransforms.Add(unitTransform);
        }

        public void UnregisterUnit(Transform unitTransform)
        {
            _unitTransforms.Remove(unitTransform);
        }

        public (float x, float y, float z) GetNearestUnitPosition(float fromX, float fromY, float fromZ)
        {
            if (TryGetNearestUnitPosition(fromX, fromY, fromZ, out var x, out var y, out var z))
                return (x, y, z);

            return (fromX, fromY, fromZ);
        }

        public bool TryGetNearestUnitPosition(
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

            for (var i = _unitTransforms.Count - 1; i >= 0; i--)
            {
                var t = _unitTransforms[i];
// csharp-guardrails: allow-null-defense
                if (t == null)
                {
                    _unitTransforms.RemoveAt(i);
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

        public bool TryGetNearestUnitWithinHorizontalRadius(
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

            for (var i = _unitTransforms.Count - 1; i >= 0; i--)
            {
                var t = _unitTransforms[i];
// csharp-guardrails: allow-null-defense
                if (t == null)
                {
                    _unitTransforms.RemoveAt(i);
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
}
