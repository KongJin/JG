using System.Collections.Generic;
using Features.Wave.Application.Ports;
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
            var from = new Vector3(fromX, fromY, fromZ);
            var nearest = from;
            var minDist = float.MaxValue;

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
                    nearest = t.position;
                }
            }

            return (nearest.x, nearest.y, nearest.z);
        }
    }
}
