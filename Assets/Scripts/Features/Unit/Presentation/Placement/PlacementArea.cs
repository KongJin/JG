using UnityEngine;

namespace Features.Unit.Presentation
{
    /// <summary>
    /// 배치 영역 정의 및 판정.
    /// Core 위치를 기준으로 아군 진영 내 고정 영역을 정의한다.
    /// </summary>
    public sealed class PlacementArea
    {
        /// <summary>배치 영역 중심 (월드 공간).</summary>
        public Vector3 Center { get; private set; }

        /// <summary>너비 (X 방향, 미터).</summary>
        public float Width { get; }

        /// <summary>깊이 (Z 방향, 미터).</summary>
        public float Depth { get; }

        /// <summary>Core로부터 Z축 오프셋 (양수 = 적 진영 방향).</summary>
        public float ForwardOffset { get; }

        public PlacementArea(float width = 8f, float depth = 5f, float forwardOffset = 0f)
        {
            Width = width;
            Depth = depth;
            ForwardOffset = forwardOffset;
            Center = Vector3.zero;
        }

        /// <summary>
        /// Core 위치를 기반으로 배치 영역 중심을 계산한다.
        /// </summary>
        public void SetCorePosition(Vector3 coreWorldPos)
        {
            Center = new Vector3(
                coreWorldPos.x,
                coreWorldPos.y,
                coreWorldPos.z + ForwardOffset
            );
        }

        /// <summary>
        /// 월드 좌표가 배치 영역 내에 있는지 판정한다.
        /// </summary>
        public bool Contains(Vector3 worldPosition)
        {
            var dx = Mathf.Abs(worldPosition.x - Center.x);
            var dz = Mathf.Abs(worldPosition.z - Center.z);

            return dx <= Width * 0.5f && dz <= Depth * 0.5f;
        }

        /// <summary>
        /// 월드 좌표를 배치 영역 경계로 보정한다.
        /// </summary>
        public Vector3 ClampToBounds(Vector3 worldPosition)
        {
            var halfWidth = Width * 0.5f;
            var halfDepth = Depth * 0.5f;

            return new Vector3(
                Mathf.Clamp(worldPosition.x, Center.x - halfWidth, Center.x + halfWidth),
                worldPosition.y,
                Mathf.Clamp(worldPosition.z, Center.z - halfDepth, Center.z + halfDepth)
            );
        }

        /// <summary>
        /// 배치 영역의 4귀퉁이를 반환한다 (시각화용).
        /// 순서: 좌하단, 우하단, 우상단, 좌상단 (XZ 평면 기준).
        /// </summary>
        public Vector3[] GetCorners()
        {
            var halfWidth = Width * 0.5f;
            var halfDepth = Depth * 0.5f;

            return new[]
            {
                new Vector3(Center.x - halfWidth, Center.y, Center.z - halfDepth), // 좌하단
                new Vector3(Center.x + halfWidth, Center.y, Center.z - halfDepth), // 우하단
                new Vector3(Center.x + halfWidth, Center.y, Center.z + halfDepth), // 우상단
                new Vector3(Center.x - halfWidth, Center.y, Center.z + halfDepth), // 좌상단
            };
        }

        /// <summary>
        /// 배치 영역의 Z축 범위 (min, max).
        /// </summary>
        public (float min, float max) GetZRange()
        {
            var halfDepth = Depth * 0.5f;
            return (Center.z - halfDepth, Center.z + halfDepth);
        }

        /// <summary>
        /// 배치 영역의 X축 범위 (min, max).
        /// </summary>
        public (float min, float max) GetXRange()
        {
            var halfWidth = Width * 0.5f;
            return (Center.x - halfWidth, Center.x + halfWidth);
        }
    }
}
