#if UNITY_EDITOR
using System.IO;
using Features.Wave.Presentation;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ProjectSD.EditorTools.SceneTools
{
    internal static class SetEResultPrefabPackTool
    {
        private const string MenuPath = "Tools/Scene/Generate Set E Result Prefabs";
        private const string TempScenePath = "Assets/Scenes/TempScene.unity";
        private const string RootFolder = "Assets/Prefabs/Features/BattleResult";
        private const string RootPrefabPath = RootFolder + "/Root/BattleResultOverlayRoot.prefab";
        private const string StatCardPrefabPath = RootFolder + "/Repeat/BattleResultStatCard.prefab";
        private const string VictoryCardPrefabPath = RootFolder + "/Independent/BattleVictoryResultCard.prefab";
        private const string DefeatCardPrefabPath = RootFolder + "/Independent/BattleDefeatResultCard.prefab";

        [MenuItem(MenuPath)]
        private static void GeneratePrefabPack()
        {
            EnsureMainStageAndTempScene();
            EnsureFolders();

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            SavePrefab(BuildOverlayRoot(font), RootPrefabPath);
            SavePrefab(BuildStatCard(font), StatCardPrefabPath);
            SavePrefab(BuildResultCard(font, true), VictoryCardPrefabPath);
            SavePrefab(BuildResultCard(font, false), DefeatCardPrefabPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[SetEResultPrefabPackTool] Generated Set E prefab pack under Assets/Prefabs/Features/BattleResult.");
        }

        private static void EnsureMainStageAndTempScene()
        {
            if (PrefabStageUtility.GetCurrentPrefabStage() != null)
            {
                StageUtility.GoToMainStage();
            }

            if (!File.Exists(TempScenePath))
            {
                var createdScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(createdScene, TempScenePath, true);
            }

            if (SceneManager.GetActiveScene().path != TempScenePath)
            {
                EditorSceneManager.OpenScene(TempScenePath, OpenSceneMode.Single);
            }
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/Prefabs");
            EnsureFolder("Assets/Prefabs/Features");
            EnsureFolder(RootFolder);
            EnsureFolder(RootFolder + "/Root");
            EnsureFolder(RootFolder + "/Repeat");
            EnsureFolder(RootFolder + "/Independent");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
            var folderName = Path.GetFileName(path);
            if (!string.IsNullOrWhiteSpace(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent ?? "Assets", folderName);
        }

        private static void SavePrefab(GameObject instanceRoot, string assetPath)
        {
            PrefabUtility.SaveAsPrefabAsset(instanceRoot, assetPath);
            Object.DestroyImmediate(instanceRoot);
        }

        private static GameObject BuildOverlayRoot(Font font)
        {
            var root = CreateUiRoot("BattleResultOverlayRoot", new Vector2(390f, 844f), new Color(0f, 0f, 0f, 0.78f));
            root.AddComponent<CanvasGroup>();
            var waveEndView = root.AddComponent<WaveEndView>();

            var panel = CreateUiChild(root.transform, "Panel", new Vector2(312f, 436f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -6f), new Color(0.08f, 0.09f, 0.12f, 0.97f));

            var resultText = CreateText(panel.transform, "ResultText", font, "Victory!", 34, FontStyle.Bold, TextAnchor.UpperCenter, new Color(1f, 0.72f, 0.14f, 1f), new Vector2(260f, 42f), new Vector2(0.5f, 1f), new Vector2(0f, -34f));
            CreateText(panel.transform, "SummaryText", font, "작전 성공", 16, FontStyle.Normal, TextAnchor.UpperCenter, new Color(0.83f, 0.85f, 0.9f, 0.9f), new Vector2(180f, 22f), new Vector2(0.5f, 1f), new Vector2(0f, -68f));

            var statsText = CreateText(panel.transform, "StatsText", font, "결과: 승리", 16, FontStyle.Normal, TextAnchor.UpperLeft, new Color(0.94f, 0.96f, 1f, 1f), new Vector2(248f, 146f), new Vector2(0.5f, 1f), new Vector2(0f, -114f));
            statsText.horizontalOverflow = HorizontalWrapMode.Wrap;
            statsText.verticalOverflow = VerticalWrapMode.Overflow;

            var statCardA = BuildStatCard(font);
            statCardA.name = "WaveStatCard";
            statCardA.transform.SetParent(panel.transform, false);
            ConfigureChildRect((RectTransform)statCardA.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(248f, 58f));
            SetTextIfExists(statCardA.transform, "LabelText", "WAVE_CLEARED");
            SetTextIfExists(statCardA.transform, "ValueText", "15 / 15");
            SetTextIfExists(statCardA.transform, "MetaText", "최종 웨이브");

            var statCardB = BuildStatCard(font);
            statCardB.name = "ScoreStatCard";
            statCardB.transform.SetParent(panel.transform, false);
            ConfigureChildRect((RectTransform)statCardB.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -220f), new Vector2(248f, 58f));
            SetTextIfExists(statCardB.transform, "LabelText", "COMMAND_SCORE");
            SetTextIfExists(statCardB.transform, "ValueText", "8,450");
            SetTextIfExists(statCardB.transform, "MetaText", "지휘 점수");

            var returnButton = CreateButton(panel.transform, font, "ReturnToLobbyButton", "RETURN_TO_LOBBY", new Vector2(280f, 52f), new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Color(0.97f, 0.67f, 0.06f, 1f), new Color(0.08f, 0.07f, 0.04f, 1f));
            CreateText(returnButton.transform, "HintText", font, "(전투로 귀환)", 10, FontStyle.Normal, TextAnchor.UpperCenter, new Color(0.19f, 0.16f, 0.09f, 0.88f), new Vector2(120f, 12f), new Vector2(0.5f, 0f), new Vector2(0f, 4f));

            SetObjectReference(waveEndView, "panel", panel);
            SetObjectReference(waveEndView, "resultText", resultText);
            SetObjectReference(waveEndView, "statsText", statsText);
            SetObjectReference(waveEndView, "returnToLobbyButton", returnButton.GetComponent<Button>());

            return root;
        }

        private static GameObject BuildStatCard(Font font)
        {
            var root = CreateUiRoot("BattleResultStatCard", new Vector2(248f, 58f), new Color(0.10f, 0.10f, 0.13f, 0.97f));
            CreateText(root.transform, "LabelText", font, "WAVE_CLEARED", 10, FontStyle.Bold, TextAnchor.UpperLeft, new Color(0.52f, 0.73f, 1f, 1f), new Vector2(150f, 12f), new Vector2(0f, 1f), new Vector2(12f, -10f));
            CreateText(root.transform, "ValueText", font, "15 / 15", 26, FontStyle.Bold, TextAnchor.UpperLeft, Color.white, new Vector2(110f, 24f), new Vector2(0f, 1f), new Vector2(12f, -24f));
            CreateText(root.transform, "MetaText", font, "최종 웨이브", 10, FontStyle.Normal, TextAnchor.UpperLeft, new Color(0.66f, 0.68f, 0.75f, 1f), new Vector2(88f, 12f), new Vector2(0f, 0f), new Vector2(12f, 10f));
            return root;
        }

        private static GameObject BuildResultCard(Font font, bool isVictory)
        {
            var accent = isVictory ? new Color(1f, 0.70f, 0.12f, 1f) : new Color(1f, 0.28f, 0.28f, 1f);
            var title = isVictory ? "MISSION_SUCCESS" : "MISSION_FAILED";
            var subtitle = isVictory ? "미션 성공" : "작전 실패";
            var waveValue = isVictory ? "15 / 15" : "09 / 15";
            var coreValue = isVictory ? "72%" : "0%";
            var deployValue = isVictory ? "48" : "32";
            var scoreValue = isVictory ? "8,450" : "2,150";

            var root = CreateUiRoot(isVictory ? "BattleVictoryResultCard" : "BattleDefeatResultCard", new Vector2(312f, 436f), new Color(0.08f, 0.09f, 0.12f, 0.97f));
            CreateText(root.transform, "ResultText", font, title, 34, FontStyle.Bold, TextAnchor.UpperCenter, accent, new Vector2(260f, 42f), new Vector2(0.5f, 1f), new Vector2(0f, -34f));
            CreateText(root.transform, "SummaryText", font, subtitle, 16, FontStyle.Normal, TextAnchor.UpperCenter, new Color(0.83f, 0.85f, 0.9f, 0.9f), new Vector2(180f, 22f), new Vector2(0.5f, 1f), new Vector2(0f, -68f));

            var statWave = BuildStatCard(font);
            statWave.name = "WaveStatCard";
            statWave.transform.SetParent(root.transform, false);
            ConfigureChildRect((RectTransform)statWave.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -126f), new Vector2(248f, 58f));
            SetTextIfExists(statWave.transform, "ValueText", waveValue);

            var statCore = BuildStatCard(font);
            statCore.name = "CoreStatCard";
            statCore.transform.SetParent(root.transform, false);
            ConfigureChildRect((RectTransform)statCore.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(32f, -196f), new Vector2(118f, 58f));
            SetTextIfExists(statCore.transform, "LabelText", "CORE_INTEGRITY");
            SetTextIfExists(statCore.transform, "ValueText", coreValue);
            SetTextIfExists(statCore.transform, "MetaText", "코어 무결성");
            TrySetImageColor(statCore, isVictory ? new Color(0.15f, 0.14f, 0.16f, 0.97f) : new Color(0.16f, 0.09f, 0.10f, 0.97f));

            var statDeploy = BuildStatCard(font);
            statDeploy.name = "DeployStatCard";
            statDeploy.transform.SetParent(root.transform, false);
            ConfigureChildRect((RectTransform)statDeploy.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-32f, -196f), new Vector2(118f, 58f));
            SetTextIfExists(statDeploy.transform, "LabelText", "UNITS_DEPLOYED");
            SetTextIfExists(statDeploy.transform, "ValueText", deployValue);
            SetTextIfExists(statDeploy.transform, "MetaText", "전투 배치");

            var statScore = BuildStatCard(font);
            statScore.name = "ScoreStatCard";
            statScore.transform.SetParent(root.transform, false);
            ConfigureChildRect((RectTransform)statScore.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -266f), new Vector2(248f, 58f));
            SetTextIfExists(statScore.transform, "LabelText", "COMMAND_SCORE");
            SetTextIfExists(statScore.transform, "ValueText", scoreValue);
            SetTextIfExists(statScore.transform, "MetaText", "지휘 점수");

            var button = CreateButton(root.transform, font, "ReturnToLobbyButton", "RETURN_TO_LOBBY", new Vector2(280f, 52f), new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Color(0.97f, 0.67f, 0.06f, 1f), new Color(0.08f, 0.07f, 0.04f, 1f));
            CreateText(button.transform, "HintText", font, "(로비로 귀환)", 10, FontStyle.Normal, TextAnchor.UpperCenter, new Color(0.19f, 0.16f, 0.09f, 0.88f), new Vector2(120f, 12f), new Vector2(0.5f, 0f), new Vector2(0f, 4f));
            return root;
        }

        private static GameObject CreateUiRoot(string name, Vector2 size, Color backgroundColor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;

            var image = go.GetComponent<Image>();
            image.color = backgroundColor;
            image.raycastTarget = false;
            return go;
        }

        private static GameObject CreateUiChild(Transform parent, string name, Vector2 size, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Color backgroundColor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            var image = go.GetComponent<Image>();
            image.color = backgroundColor;
            image.raycastTarget = false;
            return go;
        }

        private static Text CreateText(Transform parent, string name, Font font, string content, int fontSize, FontStyle fontStyle, TextAnchor alignment, Color color, Vector2 size, Vector2 anchor, Vector2 anchoredPosition)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(anchor.x, anchor.y);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;

            var text = go.GetComponent<Text>();
            text.font = font;
            text.text = content;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static GameObject CreateButton(Transform parent, Font font, string name, string label, Vector2 size, Vector2 anchor, Vector2 anchoredPosition, Color backgroundColor, Color textColor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;

            var image = go.GetComponent<Image>();
            image.color = backgroundColor;
            image.raycastTarget = true;

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;

            CreateText(go.transform, "Label", font, label, 16, FontStyle.Bold, TextAnchor.MiddleCenter, textColor, new Vector2(size.x - 24f, 22f), new Vector2(0.5f, 0.5f), new Vector2(0f, 8f));
            return go;
        }

        private static void ConfigureChildRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        private static void SetTextIfExists(Transform parent, string childName, string value)
        {
            var text = parent.Find(childName)?.GetComponent<Text>();
            if (text != null)
            {
                text.text = value;
            }
        }

        private static void TrySetImageColor(GameObject go, Color color)
        {
            var image = go.GetComponent<Image>();
            if (image != null)
            {
                image.color = color;
            }
        }

        private static void SetObjectReference(Object target, string fieldName, Object value)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(fieldName);
            if (property == null)
            {
                throw new UnityException($"Missing serialized field '{fieldName}' on {target.GetType().Name}.");
            }

            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }
    }
}
#endif
