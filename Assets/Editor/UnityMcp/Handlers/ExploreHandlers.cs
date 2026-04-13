#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ProjectSD.EditorTools.UnityMcp
{
    /// <summary>
    /// Phase 5: 대화형 요소 목록 — 클릭/입력 가능한 UI 요소 나열.
    /// [Editor-only] 엔드포인트. traverse 제외 (동적 UI 특성상 신뢰 어려움).
    /// </summary>
    internal static class ExploreHandlers
    {
        static ExploreHandlers()
        {
            "GET".Register("/explore/interactive", "List all interactive UI elements (Button, InputField, Toggle)", async (req, res) => await HandleExploreInteractiveAsync(req, res));
        }

        // =====================================================================
        // GET /explore/interactive
        // =====================================================================

        public static async Task HandleExploreInteractiveAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var canvasPath = request.QueryString["canvasPath"];

            var result = await UnityMcpBridge.RunOnMainThreadAsync(() =>
            {
                var scene = SceneManager.GetActiveScene();
                Transform rootTransform = null;

                if (!string.IsNullOrEmpty(canvasPath))
                {
                    var go = McpSharedHelpers.FindGameObjectByPath(canvasPath);
                    if (go != null) rootTransform = go.transform;
                }

                if (rootTransform == null)
                {
                    var canvas = GameObject.Find("Canvas");
                    if (canvas != null) rootTransform = canvas.transform;
                }

                var elements = new List<InteractiveElement>();
                var typeCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                if (rootTransform != null)
                {
                    CollectInteractiveElements(rootTransform, elements, typeCounts);
                }

                // byTypeJson을 수동 생성
                var byTypeJson = "{";
                var first = true;
                foreach (var kv in typeCounts)
                {
                    if (!first) byTypeJson += ",";
                    byTypeJson += "\"" + kv.Key + "\":" + kv.Value;
                    first = false;
                }
                byTypeJson += "}";

                return new ExploreInteractiveResponse
                {
                    scene = scene.name,
                    interactiveElements = elements.ToArray(),
                    totalInteractive = elements.Count,
                    byTypeJson = byTypeJson
                };
            });

            await UnityMcpBridge.WriteJsonAsync(response, 200, result);
        }

        // =====================================================================
        // Helpers
        // =====================================================================

        private static void CollectInteractiveElements(Transform current, List<InteractiveElement> elements, Dictionary<string, int> typeCounts)
        {
            var go = current.gameObject;
            var comps = go.GetComponents<Component>();

            foreach (var c in comps)
            {
                if (c == null) continue;
                var type = c.GetType();
                var typeName = type.Name;

                bool isInteractive = false;
                string displayType = typeName;

                switch (typeName)
                {
                    case "Button":
                        isInteractive = true;
                        displayType = "Button";
                        break;
                    case "TMP_InputField":
                    case "InputField":
                        isInteractive = true;
                        displayType = typeName == "TMP_InputField" ? "TMP_InputField" : "InputField";
                        break;
                    case "Toggle":
                        isInteractive = true;
                        displayType = "Toggle";
                        break;
                    case "Slider":
                        isInteractive = true;
                        displayType = "Slider";
                        break;
                    case "Dropdown":
                    case "TMP_Dropdown":
                        isInteractive = true;
                        displayType = typeName;
                        break;
                }

                if (isInteractive)
                {
                    string text = null;
                    bool interactable = true;

                    // 텍스트 추출
                    if (c is Button button)
                    {
                        interactable = button.interactable;
                        text = GetButtonText(go);
                    }
                    else if (c is Toggle toggle)
                    {
                        interactable = toggle.interactable;
                        text = GetToggleText(toggle);
                    }

                    // 타입 카운트
                    if (typeCounts.TryGetValue(displayType, out var count))
                        typeCounts[displayType] = count + 1;
                    else
                        typeCounts[displayType] = 1;

                    elements.Add(new InteractiveElement
                    {
                        path = McpSharedHelpers.GetTransformPath(go.transform),
                        name = go.name,
                        type = displayType,
                        text = text,
                        interactable = interactable,
                        activeInHierarchy = go.activeInHierarchy
                    });
                }
            }

            for (int i = 0; i < current.childCount; i++)
            {
                CollectInteractiveElements(current.GetChild(i), elements, typeCounts);
            }
        }

        private static string GetButtonText(GameObject go)
        {
            // Button의 자식에서 TextMeshProUGUI 찾기
            var tmpTexts = go.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
            foreach (var tmp in tmpTexts)
            {
                if (tmp != null && !string.IsNullOrEmpty(tmp.text) && tmp.text != "Text (TMP)")
                    return tmp.text;
            }

            // Unity UI Text도 시도
            var texts = go.GetComponentsInChildren<Text>(true);
            foreach (var t in texts)
            {
                if (t != null && !string.IsNullOrEmpty(t.text) && t.text != "Text")
                    return t.text;
            }

            return null;
        }

        private static string GetToggleText(Toggle toggle)
        {
            if (toggle.graphic != null && !string.IsNullOrEmpty(toggle.graphic.name))
                return toggle.graphic.name;
            // Toggle의 자식에서 텍스트 찾기
            var tmpTexts = toggle.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
            foreach (var tmp in tmpTexts)
            {
                if (tmp != null && !string.IsNullOrEmpty(tmp.text))
                    return tmp.text;
            }
            return null;
        }
    }
}
#endif
