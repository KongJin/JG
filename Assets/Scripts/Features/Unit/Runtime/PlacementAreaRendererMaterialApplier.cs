using UnityEngine;

namespace Features.Unit.Runtime
{
    internal static class PlacementAreaRendererMaterialApplier
    {
        public static void Apply(MeshRenderer renderer, Material material)
        {
            if (renderer == null || material == null)
                return;

            renderer.sharedMaterial = material;
        }
    }
}
