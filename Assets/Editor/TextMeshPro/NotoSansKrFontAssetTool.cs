#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace ProjectSD.EditorTools.TextMeshPro
{
    internal static class NotoSansKrFontAssetTool
    {
        private const string MenuPath = "Tools/TextMeshPro/Refresh Noto Sans KR Fallback";
        private const string SourceFontPath = "Assets/Fonts/NotoSansKR-VF.ttf";
        private const string OutputAssetPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/NotoSansKR Dynamic.asset";
        private const int SamplingPointSize = 90;
        private const int AtlasPadding = 9;
        private const int AtlasWidth = 2048;
        private const int AtlasHeight = 2048;

        [MenuItem(MenuPath)]
        private static void RefreshFallback()
        {
            if (TMP_Settings.instance == null)
            {
                Debug.LogError("[TMP] TMP Essential Resources are missing. Import them before creating the fallback.");
                return;
            }

            EnsureParentFolderExists(OutputAssetPath);

            var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
            if (sourceFont == null)
            {
                Debug.LogError($"[TMP] Source font not found at '{SourceFontPath}'.");
                return;
            }

            RemoveExistingAsset(OutputAssetPath);

            var fontAsset = TMP_FontAsset.CreateFontAsset(
                sourceFont,
                SamplingPointSize,
                AtlasPadding,
                GlyphRenderMode.SDFAA,
                AtlasWidth,
                AtlasHeight);

            if (fontAsset == null)
            {
                Debug.LogError("[TMP] Failed to create Noto Sans KR font asset.");
                return;
            }

            fontAsset.name = Path.GetFileNameWithoutExtension(OutputAssetPath);
            AssetDatabase.CreateAsset(fontAsset, OutputAssetPath);

            if (fontAsset.atlasTexture != null)
            {
                fontAsset.atlasTexture.name = "NotoSansKR Dynamic Atlas";
                AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);
            }

            if (fontAsset.material != null)
            {
                fontAsset.material.name = "NotoSansKR Dynamic Material";
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }

            WarmUpCommonGlyphs(fontAsset);
            AttachToTmpFallbacks(fontAsset);

            EditorUtility.SetDirty(fontAsset);
            EditorUtility.SetDirty(TMP_Settings.instance);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[TMP] Refreshed Korean fallback font asset at '{OutputAssetPath}'.");
        }

        [MenuItem(MenuPath, true)]
        private static bool ValidateRefreshFallback()
        {
            return File.Exists(Path.GetFullPath(SourceFontPath));
        }

        private static void WarmUpCommonGlyphs(TMP_FontAsset fontAsset)
        {
            var warmupSegments = new List<string>
            {
                "차고 편성 저장 출격 준비 슬롯 대기 조립 프레임 무장 기동 임시안",
                "저장 완료 미저장 비어 있음 작성 중 동기화 로스터 결과 미리보기",
                "프레임 선택 무장 선택 기동 선택 저장 가능 저장됨 출격 가능 출격 잠김",
                "선택한 빌드 미리보기가 여기에 표시됩니다.",
                "빌드 동기화 중... 임시안 저장 가능 로스터 동기화 완료. 출격 가능.",
                "HP ASPD DMG RNG MOV ANC Cost Trait"
            };

            foreach (var segment in warmupSegments)
            {
                fontAsset.TryAddCharacters(segment, out _);
            }
        }

        private static void AttachToTmpFallbacks(TMP_FontAsset fontAsset)
        {
            TMP_Settings.fallbackFontAssets ??= new List<TMP_FontAsset>();
            TMP_Settings.fallbackFontAssets.RemoveAll(asset =>
                asset == null ||
                AssetDatabase.GetAssetPath(asset) == OutputAssetPath);
            TMP_Settings.fallbackFontAssets.Insert(0, fontAsset);
        }

        private static void EnsureParentFolderExists(string assetPath)
        {
            var parent = Path.GetDirectoryName(assetPath);
            if (string.IsNullOrWhiteSpace(parent))
            {
                return;
            }

            var absoluteParent = Path.GetFullPath(parent);
            if (!Directory.Exists(absoluteParent))
            {
                Directory.CreateDirectory(absoluteParent);
                AssetDatabase.Refresh();
            }
        }

        private static void RemoveExistingAsset(string assetPath)
        {
            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath) != null)
            {
                AssetDatabase.DeleteAsset(assetPath);
            }
        }
    }
}
#endif
