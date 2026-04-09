using UnityEngine;

namespace Features.Unit.Presentation
{
    /// <summary>
    /// 배치 영역용 반투명 Material 생성 헬퍼.
    /// </summary>
    public static class PlacementAreaMaterialFactory
    {
        /// <summary>
        /// 유효한 배치 영역용 반투명 녹색 Material을 생성한다.
        /// </summary>
        public static Material CreateValidMaterial()
        {
            var mat = new Material(Shader.Find("Standard"))
            {
                color = new Color(0f, 1f, 0f, 0.25f), // 반투명 녹색
            };
            mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = 3000;
            return mat;
        }

        /// <summary>
        /// 무효한 배치 영역용 반투명 빨간색 Material을 생성한다.
        /// </summary>
        public static Material CreateInvalidMaterial()
        {
            var mat = new Material(Shader.Find("Standard"))
            {
                color = new Color(1f, 0f, 0f, 0.25f), // 반투명 빨간색
            };
            mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = 3000;
            return mat;
        }

        /// <summary>
        /// 하이라이트용 반투명 노란색 Material을 생성한다.
        /// </summary>
        public static Material CreateHighlightMaterial()
        {
            var mat = new Material(Shader.Find("Standard"))
            {
                color = new Color(1f, 1f, 0f, 0.35f), // 반투명 노란색 (하이라이트)
            };
            mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = 3000;
            return mat;
        }
    }
}
