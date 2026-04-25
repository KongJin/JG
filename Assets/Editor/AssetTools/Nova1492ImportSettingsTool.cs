using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace ProjectSD.EditorTools
{
    public static class Nova1492ImportSettingsTool
    {
        private const string MenuPath = "Tools/Nova1492/Apply Staging Import Settings";
        private const string RootPath = "Assets/Art/Nova1492";
        private const string ReportPath = "artifacts/nova1492/unity_import_settings_report.md";

        [MenuItem(MenuPath)]
        public static void ApplyStagingImportSettings()
        {
            var texturePaths = AssetDatabase.FindAssets("t:Texture2D", new[] { RootPath });
            var changed = new List<string>();
            var alreadyOk = new List<string>();

            foreach (var guid in texturePaths)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }

                if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                {
                    continue;
                }

                var desiredMaxSize = path.Contains("/UI/") ? 2048 : 1024;
                var dirty = false;

                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    dirty = true;
                }

                if (importer.spriteImportMode != SpriteImportMode.Single)
                {
                    importer.spriteImportMode = SpriteImportMode.Single;
                    dirty = true;
                }

                if (!importer.alphaIsTransparency)
                {
                    importer.alphaIsTransparency = true;
                    dirty = true;
                }

                if (importer.mipmapEnabled)
                {
                    importer.mipmapEnabled = false;
                    dirty = true;
                }

                if (importer.maxTextureSize != desiredMaxSize)
                {
                    importer.maxTextureSize = desiredMaxSize;
                    dirty = true;
                }

                if (importer.textureCompression != TextureImporterCompression.Compressed)
                {
                    importer.textureCompression = TextureImporterCompression.Compressed;
                    dirty = true;
                }

                if (dirty)
                {
                    importer.SaveAndReimport();
                    changed.Add(path);
                }
                else
                {
                    alreadyOk.Add(path);
                }
            }

            WriteReport(changed, alreadyOk);
            Debug.Log($"[Nova1492] Applied import settings. changed={changed.Count}, alreadyOk={alreadyOk.Count}, report={ReportPath}");
        }

        private static void WriteReport(IReadOnlyCollection<string> changed, IReadOnlyCollection<string> alreadyOk)
        {
            var builder = new StringBuilder();
            builder.AppendLine("# Nova1492 Unity Import Settings Report");
            builder.AppendLine();
            builder.AppendLine($"> generated: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            builder.AppendLine();
            builder.AppendLine($"- root: `{RootPath}`");
            builder.AppendLine($"- changed: {changed.Count}");
            builder.AppendLine($"- already ok: {alreadyOk.Count}");
            builder.AppendLine();
            builder.AppendLine("## Applied Defaults");
            builder.AppendLine();
            builder.AppendLine("- texture type: `Sprite (2D and UI)`");
            builder.AppendLine("- sprite mode: `Single`");
            builder.AppendLine("- alpha is transparency: `true`");
            builder.AppendLine("- mip maps: `false`");
            builder.AppendLine("- UI max texture size: `2048`");
            builder.AppendLine("- unit/effect max texture size: `1024`");
            builder.AppendLine("- texture compression: `Compressed`");
            builder.AppendLine();
            builder.AppendLine("## Changed Assets");
            builder.AppendLine();

            foreach (var path in changed)
            {
                builder.AppendLine($"- `{path}`");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
            File.WriteAllText(ReportPath, builder.ToString(), Encoding.UTF8);
            AssetDatabase.Refresh();
        }
    }
}
