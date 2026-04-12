#if UNITY_EDITOR
using System;
using System.Net;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ProjectSD.EditorTools.UnityMcp
{
    internal static class UiHandlers
    {
        public static async Task HandleUiButtonInvokeAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            UiButtonInvokeRequest req = null;
            if (request.HasEntityBody)
            {
                var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
                if (!string.IsNullOrWhiteSpace(body)) req = JsonUtility.FromJson<UiButtonInvokeRequest>(body);
            }

            try
            {
                var result = await UnityMcpBridge.RunOnMainThreadAsync(() => ExecuteUiButtonInvoke(req));
                await UnityMcpBridge.WriteJsonAsync(response, 200, result);
            }
            catch (ArgumentException ex) { await UnityMcpBridge.WriteJsonAsync(response, 400, new ErrorResponse { error = "Invalid button invoke request", detail = ex.Message }); }
            catch (MissingMemberException ex) { await UnityMcpBridge.WriteJsonAsync(response, 404, new ErrorResponse { error = "Button not found", detail = ex.Message }); }
            catch (InvalidOperationException ex) { await UnityMcpBridge.WriteJsonAsync(response, 409, new ErrorResponse { error = "Button invoke unavailable", detail = ex.Message }); }
        }

        public static async Task HandleUiCreatePanelAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
            var req = JsonUtility.FromJson<UiCreatePanelRequest>(body);
            var result = await UnityMcpBridge.RunOnMainThreadAsync(() =>
            {
                Transform parent = null;
                if (!string.IsNullOrEmpty(req.parent))
                {
                    var parentGo = McpSharedHelpers.FindGameObjectByPath(req.parent);
                    if (parentGo == null) parentGo = GameObject.Find(req.parent);
                    if (parentGo == null) throw new Exception("Parent not found: " + req.parent);
                    parent = parentGo.transform;
                }
                var go = new GameObject(string.IsNullOrEmpty(req.name) ? "Panel" : req.name);
                if (parent != null) go.transform.SetParent(parent, false);
                var rectTransform = go.AddComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(req.width > 0 ? req.width : 200, req.height > 0 ? req.height : 100);
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                var image = go.AddComponent<UnityEngine.UI.Image>();
                image.color = new Color(0.1f, 0.1f, 0.15f, 0.9f);
                Undo.RegisterCreatedObjectUndo(go, "MCP Create UI Panel");
                EditorSceneManager.MarkSceneDirty(go.scene);
                return new UiCreatePanelResponse { success = true, message = "Created UI Panel: " + go.name, path = McpSharedHelpers.GetTransformPath(go.transform), name = go.name };
            });
            await UnityMcpBridge.WriteJsonAsync(response, 200, result);
        }

        public static async Task HandleUiCreateRawImageAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
            var req = JsonUtility.FromJson<UiCreateRawImageRequest>(body);
            var result = await UnityMcpBridge.RunOnMainThreadAsync(() =>
            {
                Transform parent = null;
                if (!string.IsNullOrEmpty(req.parent))
                {
                    var parentGo = McpSharedHelpers.FindGameObjectByPath(req.parent);
                    if (parentGo == null) parentGo = GameObject.Find(req.parent);
                    if (parentGo == null) throw new Exception("Parent not found: " + req.parent);
                    parent = parentGo.transform;
                }
                var go = new GameObject(string.IsNullOrEmpty(req.name) ? "RawImage" : req.name);
                if (parent != null) go.transform.SetParent(parent, false);
                var rectTransform = go.AddComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(req.width > 0 ? req.width : 256, req.height > 0 ? req.height : 256);
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                var rawImage = go.AddComponent<UnityEngine.UI.RawImage>();
                rawImage.color = new Color(0.05f, 0.06f, 0.1f, 1f);
                Undo.RegisterCreatedObjectUndo(go, "MCP Create UI RawImage");
                EditorSceneManager.MarkSceneDirty(go.scene);
                return new UiCreateRawImageResponse { success = true, message = "Created UI RawImage: " + go.name, path = McpSharedHelpers.GetTransformPath(go.transform), name = go.name };
            });
            await UnityMcpBridge.WriteJsonAsync(response, 200, result);
        }

        public static async Task HandleUiSetRectAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
            var req = JsonUtility.FromJson<UiSetRectRequest>(body);
            var result = await UnityMcpBridge.RunOnMainThreadAsync(() =>
            {
                var go = McpSharedHelpers.FindGameObjectByPath(req.path);
                if (go == null) throw new Exception("GameObject not found: " + req.path);
                var rect = go.GetComponent<RectTransform>();
                if (rect == null) throw new Exception("RectTransform not found on: " + req.path);
                if (req.anchoredPositionX.HasValue || req.anchoredPositionY.HasValue) { var pos = rect.anchoredPosition; if (req.anchoredPositionX.HasValue) pos.x = req.anchoredPositionX.Value; if (req.anchoredPositionY.HasValue) pos.y = req.anchoredPositionY.Value; rect.anchoredPosition = pos; }
                if (req.sizeDeltaX.HasValue || req.sizeDeltaY.HasValue) { var size = rect.sizeDelta; if (req.sizeDeltaX.HasValue) size.x = req.sizeDeltaX.Value; if (req.sizeDeltaY.HasValue) size.y = req.sizeDeltaY.Value; rect.sizeDelta = size; }
                if (req.anchorMinX.HasValue || req.anchorMinY.HasValue) { var min = rect.anchorMin; if (req.anchorMinX.HasValue) min.x = req.anchorMinX.Value; if (req.anchorMinY.HasValue) min.y = req.anchorMinY.Value; rect.anchorMin = min; }
                if (req.anchorMaxX.HasValue || req.anchorMaxY.HasValue) { var max = rect.anchorMax; if (req.anchorMaxX.HasValue) max.x = req.anchorMaxX.Value; if (req.anchorMaxY.HasValue) max.y = req.anchorMaxY.Value; rect.anchorMax = max; }
                if (req.pivotX.HasValue || req.pivotY.HasValue) { var pivot = rect.pivot; if (req.pivotX.HasValue) pivot.x = req.pivotX.Value; if (req.pivotY.HasValue) pivot.y = req.pivotY.Value; rect.pivot = pivot; }
                Undo.RegisterCompleteObjectUndo(rect, "MCP Set Rect");
                EditorSceneManager.MarkSceneDirty(go.scene);
                return new UiSetRectResponse { success = true, message = "RectTransform updated: " + go.name, path = McpSharedHelpers.GetTransformPath(go.transform) };
            });
            await UnityMcpBridge.WriteJsonAsync(response, 200, result);
        }

        public static async Task HandleUiCreateButtonAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
            var req = JsonUtility.FromJson<UiCreateButtonRequest>(body);
            var result = await UnityMcpBridge.RunOnMainThreadAsync(() =>
            {
                Transform parent = null;
                if (!string.IsNullOrEmpty(req.parent))
                {
                    var parentGo = McpSharedHelpers.FindGameObjectByPath(req.parent);
                    if (parentGo == null) parentGo = GameObject.Find(req.parent);
                    if (parentGo == null) throw new Exception("Parent not found: " + req.parent);
                    parent = parentGo.transform;
                }
                var go = new GameObject(string.IsNullOrEmpty(req.name) ? "Button" : req.name);
                if (parent != null) go.transform.SetParent(parent, false);
                var rectTransform = go.AddComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(160, 30);
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                var image = go.AddComponent<UnityEngine.UI.Image>();
                image.color = new Color(0.2f, 0.4f, 0.9f, 1f);
                image.type = UnityEngine.UI.Image.Type.Sliced;
                go.AddComponent<UnityEngine.UI.Button>();
                var textGo = new GameObject("Text (TMP)");
                textGo.transform.SetParent(go.transform, false);
                var textRect = textGo.AddComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.sizeDelta = Vector2.zero;
                var tmpType = UnityMcpBridge.ResolveTypeByFullName("TMPro.TextMeshProUGUI");
                if (tmpType != null)
                {
                    var tmpText = (TMPro.TextMeshProUGUI)textGo.AddComponent(tmpType);
                    tmpText.text = string.IsNullOrEmpty(req.buttonText) ? "Button" : req.buttonText;
                    tmpText.fontSize = 14;
                    tmpText.alignment = TMPro.TextAlignmentOptions.Center;
                    tmpText.color = Color.white;
                }
                Undo.RegisterCreatedObjectUndo(go, "MCP Create UI Button");
                EditorSceneManager.MarkSceneDirty(go.scene);
                return new UiCreateButtonResponse { success = true, message = "Created UI Button: " + go.name, path = McpSharedHelpers.GetTransformPath(go.transform), name = go.name };
            });
            await UnityMcpBridge.WriteJsonAsync(response, 200, result);
        }

        // =====================================================================
        // Helper Methods
        // =====================================================================

        private static UiButtonInvokeResponse ExecuteUiButtonInvoke(UiButtonInvokeRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.path)) throw new ArgumentException("path is required.");
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("UI button invoke requires play mode.");
            var gameObject = GameObject.Find(req.path);
            if (gameObject == null) throw new MissingMemberException("GameObject not found: " + req.path);
            var button = gameObject.GetComponent<UnityEngine.UI.Button>();
            if (button == null) throw new MissingMemberException("Button component not found on: " + req.path);
            button.onClick.Invoke();
            return new UiButtonInvokeResponse
            {
                success = true, message = "Invoked Button.onClick: " + req.path, path = McpSharedHelpers.GetTransformPath(gameObject.transform),
                name = gameObject.name, activeSelf = gameObject.activeSelf, activeInHierarchy = gameObject.activeInHierarchy,
                interactable = button.interactable, persistentListenerCount = button.onClick.GetPersistentEventCount()
            };
        }
    }
}
#endif
