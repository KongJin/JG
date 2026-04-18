using UnityEngine;

namespace Features.Unit.Presentation
{
    /// <summary>
    /// 배치 영역용 반투명 Material 생성 헬퍼.
    /// </summary>
    public static class PlacementAreaMaterialFactory
    {
        private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");
        private static readonly int BaseColorPropertyId = Shader.PropertyToID("_BaseColor");

        /// <summary>
        /// 유효한 배치 영역용 반투명 녹색 Material을 생성한다.
        /// </summary>
        public static Material CreateValidMaterial()
        {
            return CreateTransparentMaterial(new Color(0f, 1f, 0f, 0.25f));
        }

        /// <summary>
        /// 무효한 배치 영역용 반투명 빨간색 Material을 생성한다.
        /// </summary>
        public static Material CreateInvalidMaterial()
        {
            return CreateTransparentMaterial(new Color(1f, 0f, 0f, 0.25f));
        }

        /// <summary>
        /// 하이라이트용 반투명 노란색 Material을 생성한다.
        /// </summary>
        public static Material CreateHighlightMaterial()
        {
            return CreateTransparentMaterial(new Color(1f, 1f, 0f, 0.35f));
        }

        private static Material CreateTransparentMaterial(Color color)
        {
            var shader = ResolveShader();
            var material = new Material(shader);
            ApplyColor(material, color);

            if (shader != null && shader.name == "Universal Render Pipeline/Unlit")
            {
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_Blend", 0f);
                material.SetFloat("_ZWrite", 0f);
                material.SetFloat("_AlphaClip", 0f);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                return material;
            }

            material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            return material;
        }

        private static Shader ResolveShader()
        {
            return Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Unlit/Color")
                ?? Shader.Find("Sprites/Default")
                ?? Shader.Find("Standard");
        }

        private static void ApplyColor(Material material, Color color)
        {
            if (material.HasProperty(BaseColorPropertyId))
            {
                material.SetColor(BaseColorPropertyId, color);
            }

            if (material.HasProperty(ColorPropertyId))
            {
                material.SetColor(ColorPropertyId, color);
            }
        }
    }
}
