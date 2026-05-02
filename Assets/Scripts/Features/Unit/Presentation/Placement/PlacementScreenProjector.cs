using UnityEngine;

namespace Features.Unit.Presentation
{
    internal static class PlacementScreenProjector
    {
        public static Vector3 ToWorld(Camera worldCamera, float planeY, Vector2 screenPosition)
        {
            if (worldCamera == null)
                return Vector3.zero;

            var ray = worldCamera.ScreenPointToRay(new Vector3(screenPosition.x, screenPosition.y, 0f));
            var plane = new Plane(Vector3.up, new Vector3(0f, planeY, 0f));
            return plane.Raycast(ray, out var enter) ? ray.GetPoint(enter) : Vector3.zero;
        }
    }
}
