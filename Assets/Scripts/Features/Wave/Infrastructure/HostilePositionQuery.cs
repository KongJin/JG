using Features.Enemy.Application.Ports;
using Features.Wave.Infrastructure;
using UnityEngine;

namespace Features.Wave.Infrastructure
{
    /// <summary>
    /// Combined hostile target position query that checks both Players and BattleEntities.
    /// Used by Enemy AI for targeting any hostile entity.
    /// </summary>
    public sealed class HostilePositionQuery : IPlayerPositionQuery
    {
        private readonly PlayerPositionQueryAdapter _playerQuery;
        private readonly UnitPositionQueryAdapter _unitQuery;

        public HostilePositionQuery(PlayerPositionQueryAdapter playerQuery, UnitPositionQueryAdapter unitQuery)
        {
            _playerQuery = playerQuery;
            _unitQuery = unitQuery;
        }

        public (float x, float y, float z) GetNearestPlayerPosition(float fromX, float fromY, float fromZ)
        {
            // Find nearest from both players and units, return the absolute nearest
            var nearestPlayer = _playerQuery.GetNearestPlayerPosition(fromX, fromY, fromZ);
            var nearestUnit = _unitQuery.GetNearestUnitPosition(fromX, fromY, fromZ);

            var from = new Vector3(fromX, fromY, fromZ);
            var distToPlayer = (new Vector3(nearestPlayer.x, nearestPlayer.y, nearestPlayer.z) - from).sqrMagnitude;
            var distToUnit = (new Vector3(nearestUnit.x, nearestUnit.y, nearestUnit.z) - from).sqrMagnitude;

            return distToPlayer <= distToUnit ? nearestPlayer : nearestUnit;
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
            // Check both players and units within radius, return the nearest one
            var foundPlayer = _playerQuery.TryGetNearestPlayerWithinHorizontalRadius(
                fromX, fromY, fromZ, radius, out var ptx, out var pty, out var ptz);
            var foundUnit = _unitQuery.TryGetNearestUnitWithinHorizontalRadius(
                fromX, fromY, fromZ, radius, out var utx, out var uty, out var utz);

            if (!foundPlayer && !foundUnit)
            {
                tx = ty = tz = 0f;
                return false;
            }

            if (foundPlayer && !foundUnit)
            {
                tx = ptx; ty = pty; tz = ptz;
                return true;
            }

            if (!foundPlayer && foundUnit)
            {
                tx = utx; ty = uty; tz = utz;
                return true;
            }

            // Both found — pick the nearest
            var from2 = new Vector3(fromX, fromY, fromZ);
            var distToPlayer = (new Vector3(ptx, pty, ptz) - from2).sqrMagnitude;
            var distToUnit = (new Vector3(utx, uty, utz) - from2).sqrMagnitude;

            if (distToPlayer <= distToUnit)
            {
                tx = ptx; ty = pty; tz = ptz;
            }
            else
            {
                tx = utx; ty = uty; tz = utz;
            }

            return true;
        }
    }
}
