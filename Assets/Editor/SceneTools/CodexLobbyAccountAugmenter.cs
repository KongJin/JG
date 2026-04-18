#if UNITY_EDITOR
using System;
using Features.Account;
using Features.Account.Infrastructure;
using Features.Account.Presentation;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ProjectSD.EditorTools.SceneTools
{
    internal static class CodexLobbyAccountAugmenter
    {
        private const string ScenePath = "Assets/Scenes/CodexLobbyScene.unity";
        private const string AccountConfigPath = "Assets/Settings/AccountConfig.asset";

        [MenuItem("Tools/Codex/Augment Current Codex Lobby With Account")]
        private static void AugmentCurrentScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !string.Equals(scene.path, ScenePath, StringComparison.Ordinal))
                throw new InvalidOperationException($"Open {ScenePath} before running the augmenter.");

            var canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
            var lobbySetup = UnityEngine.Object.FindFirstObjectByType<LobbySetup>();
            if (canvas == null || lobbySetup == null)
                throw new InvalidOperationException("Canvas and LobbySetup must exist before Account augmentation.");

            Augment(canvas, lobbySetup);
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("[CodexLobbyAccountAugmenter] Account wiring applied to CodexLobbyScene.");
        }

        internal static void Augment(Canvas canvas, LobbySetup lobbySetup)
        {
            if (canvas == null)
                throw new ArgumentNullException(nameof(canvas));

            if (lobbySetup == null)
                throw new ArgumentNullException(nameof(lobbySetup));

            DestroyIfExists("AccountSetup");
            DestroyIfExists("LoginLoadingOverlay");

            var accountSetupGo = CreateGameObject("AccountSetup");
            var accountSetup = accountSetupGo.AddComponent<AccountSetup>();
            var accountConfig = LoadAsset<AccountConfig>(AccountConfigPath);
            CodexLobbyGarageDataBuilder.SetObject(accountSetup, "_config", accountConfig);

            var loginLoadingView = BuildLoginLoadingOverlay(canvas.transform);
            var accountSettingsView = UnityEngine.Object.FindFirstObjectByType<AccountSettingsView>();

            CodexLobbyGarageDataBuilder.SetObject(lobbySetup, "_accountSetup", accountSetup);
            CodexLobbyGarageDataBuilder.SetObject(lobbySetup, "_loginLoadingView", loginLoadingView);
            if (accountSettingsView != null)
                CodexLobbyGarageDataBuilder.SetObject(lobbySetup, "_accountSettingsView", accountSettingsView);
        }

        private static LoginLoadingView BuildLoginLoadingOverlay(Transform parent)
        {
            var root = CreateGameObject("LoginLoadingOverlay", parent, typeof(RectTransform));
            Stretch(root.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var loadingPanel = CreatePanel("LoadingPanel", root.transform, new Color32(4, 8, 15, 214), true);
            Stretch(loadingPanel.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var loadingCard = CreatePanel("LoadingCard", loadingPanel.transform, new Color32(24, 31, 50, 250), true);
            Stretch(loadingCard.rectTransform, new Vector2(0.37f, 0.41f), new Vector2(0.63f, 0.59f), Vector2.zero, Vector2.zero);

            var statusText = CreateText(
                "StatusText",
                loadingCard.transform,
                "Signing in...",
                23,
                FontStyles.Bold,
                TextAlignmentOptions.Center,
                new Color32(244, 247, 255, 255));
            Stretch(statusText.rectTransform, new Vector2(0.08f, 0.22f), new Vector2(0.92f, 0.78f), Vector2.zero, Vector2.zero);

            var errorPanel = CreatePanel("ErrorPanel", root.transform, new Color32(4, 8, 15, 224), true);
            Stretch(errorPanel.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var errorCard = CreatePanel("ErrorCard", errorPanel.transform, new Color32(28, 35, 58, 252), true);
            Stretch(errorCard.rectTransform, new Vector2(0.31f, 0.31f), new Vector2(0.69f, 0.63f), Vector2.zero, Vector2.zero);

            var errorTitle = CreateText(
                "ErrorTitle",
                errorCard.transform,
                "Sign-in failed",
                24,
                FontStyles.Bold,
                TextAlignmentOptions.Center,
                new Color32(244, 247, 255, 255));
            Stretch(errorTitle.rectTransform, new Vector2(0.08f, 0.7f), new Vector2(0.92f, 0.86f), Vector2.zero, Vector2.zero);

            var errorText = CreateText(
                "ErrorText",
                errorCard.transform,
                "Please check your network connection.",
                16,
                FontStyles.Normal,
                TextAlignmentOptions.Center,
                new Color32(214, 223, 241, 255));
            Stretch(errorText.rectTransform, new Vector2(0.1f, 0.36f), new Vector2(0.9f, 0.66f), Vector2.zero, Vector2.zero);
            errorText.textWrappingMode = TextWrappingModes.Normal;

            var retryButton = CreateButton(
                "RetryButton",
                errorCard.transform,
                "Try Again",
                new Color32(73, 118, 255, 255),
                Color.white);
            Stretch(retryButton.GetComponent<RectTransform>(), new Vector2(0.32f, 0.12f), new Vector2(0.68f, 0.26f), Vector2.zero, Vector2.zero);

            errorPanel.gameObject.SetActive(false);

            var loginLoadingView = root.AddComponent<LoginLoadingView>();
            CodexLobbyGarageDataBuilder.SetObject(loginLoadingView, "_loadingPanel", loadingPanel.gameObject);
            CodexLobbyGarageDataBuilder.SetObject(loginLoadingView, "_statusText", statusText);
            CodexLobbyGarageDataBuilder.SetObject(loginLoadingView, "_errorPanel", errorPanel.gameObject);
            CodexLobbyGarageDataBuilder.SetObject(loginLoadingView, "_errorText", errorText);
            CodexLobbyGarageDataBuilder.SetObject(loginLoadingView, "_retryButton", retryButton);

            return loginLoadingView;
        }

        private static void DestroyIfExists(string objectName)
        {
            var existing = GameObject.Find(objectName);
            if (existing != null)
                UnityEngine.Object.DestroyImmediate(existing);
        }

        private static T LoadAsset<T>(string path) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                throw new InvalidOperationException($"Required asset not found at {path}");

            return asset;
        }

        private static Image CreatePanel(string name, Transform parent, Color color, bool raycastTarget)
        {
            var go = CreateGameObject(name, parent, typeof(RectTransform), typeof(Image));
            var image = go.GetComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            image.material = null;
            image.type = Image.Type.Sliced;
            image.color = color;
            image.raycastTarget = raycastTarget;
            return image;
        }

        private static TextMeshProUGUI CreateText(
            string name,
            Transform parent,
            string value,
            float fontSize,
            FontStyles fontStyle,
            TextAlignmentOptions alignment,
            Color color)
        {
            var go = CreateGameObject(name, parent, typeof(RectTransform));
            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.color = color;
            text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }

        private static Button CreateButton(string name, Transform parent, string label, Color fill, Color textColor)
        {
            var go = CreateGameObject(name, parent, typeof(RectTransform), typeof(Image), typeof(Button));
            var image = go.GetComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            image.material = null;
            image.type = Image.Type.Sliced;
            image.color = fill;

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = fill;
            colors.highlightedColor = fill * 1.08f;
            colors.pressedColor = fill * 0.92f;
            colors.selectedColor = fill * 1.02f;
            colors.disabledColor = new Color(fill.r * 0.45f, fill.g * 0.45f, fill.b * 0.45f, 0.65f);
            button.colors = colors;

            var labelText = CreateText("Label", go.transform, label, 16, FontStyles.Bold, TextAlignmentOptions.Center, textColor);
            Stretch(labelText.rectTransform, Vector2.zero, Vector2.one, new Vector2(12f, 8f), new Vector2(-12f, -8f));
            return button;
        }

        private static GameObject CreateGameObject(string name, Transform parent = null, params Type[] componentTypes)
        {
            var go = new GameObject(name);
            if (parent != null)
                go.transform.SetParent(parent, false);

            foreach (var componentType in componentTypes)
            {
                if (componentType == typeof(RectTransform))
                {
                    if (go.GetComponent<RectTransform>() == null)
                        go.AddComponent<RectTransform>();

                    continue;
                }

                if (go.GetComponent(componentType) == null)
                    go.AddComponent(componentType);
            }

            return go;
        }

        private static void Stretch(
            RectTransform rectTransform,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = offsetMin;
            rectTransform.offsetMax = offsetMax;
            rectTransform.localScale = Vector3.one;
            rectTransform.localPosition = Vector3.zero;
        }
    }
}
#endif
