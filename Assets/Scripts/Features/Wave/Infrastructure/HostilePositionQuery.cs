using Features.Enemy.Application.Ports;

namespace Features.Wave.Infrastructure
{
    /// <summary>
    /// Combined hostile target position query that checks BattleEntities before Players.
    /// Used as the unit -> player fallback after core targeting.
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
            if (_unitQuery.TryGetNearestUnitPosition(fromX, fromY, fromZ, out var ux, out var uy, out var uz))
                return (ux, uy, uz);

            return _playerQuery.GetNearestPlayerPosition(fromX, fromY, fromZ);
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
            if (_unitQuery.TryGetNearestUnitWithinHorizontalRadius(
                    fromX, fromY, fromZ, radius, out tx, out ty, out tz))
            {
                return true;
            }

            return _playerQuery.TryGetNearestPlayerWithinHorizontalRadius(
                fromX, fromY, fromZ, radius, out tx, out ty, out tz);
        }
    }
}
