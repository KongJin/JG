#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectSD.EditorTools.UnityMcp
{
    /// <summary>
    /// 개선된 UI 핸들러 - 코드로 바인딩된 핸들러도 호출 가능
    /// </summary>
    internal static class ImprovedUiHandlers
    {
        static ImprovedUiHandlers()
        {
            // 개선된 UI 호출 엔드포인트
            "POST".Register("/ui/invoke", "Invoke UI element (button, input, custom method)", async (req, res) => await HandleUiInvokeAsync(req, res));

            // UI 요소 상태 조회
            "POST".Register("/ui/get-state", "Get UI element current state", async (req, res) => await HandleUiGetStateAsync(req, res));

            // UI 요소 값 설정
            "POST".Register("/ui/set-value", "Set UI element value (InputField, Toggle, Slider)", async (req, res) => await HandleUiSetValueAsync(req, res));

            // 이벤트 핸들러 목록 조회
            "POST".Register("/ui/list-handlers", "List all event handlers on UI element", async (req, res) => await HandleUiListHandlersAsync(req, res));

            // 기존 엔드포인트 호환성 유지
            "POST".Register("/ui/button/invoke", "Invoke a Unity UI Button.onClick (legacy)", async (req, res) => await HandleUiButtonInvokeAsync(req, res));
            "POST".Register("/ui/create-button", "Create a UI Button with TMP text", async (req, res) => await HandleUiCreateButtonAsync(req, res));
            "POST".Register("/ui/create-panel", "Create a UI Panel (Image)", async (req, res) => await HandleUiCreatePanelAsync(req, res));
            "POST".Register("/ui/create-raw-image", "Create a UI RawImage", async (req, res) => await HandleUiCreateRawImageAsync(req, res));
            "POST".Register("/ui/set-rect", "Modify RectTransform properties", async (req, res) => await HandleUiSetRectAsync(req, res));
        }

        // =====================================================================
        // UI Invoke - 개선된 버전
        // =====================================================================

        public static async Task HandleUiInvokeAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            UiInvokeRequest req = null;
            if (request.HasEntityBody)
            {
                var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
                if (!string.IsNullOrWhiteSpace(body)) req = JsonUtility.FromJson<UiInvokeRequest>(body);
            }

            try
            {
                var result = await UnityMcpBridge.RunOnMainThreadAsync(() => ExecuteUiInvoke(req));
                await UnityMcpBridge.WriteJsonAsync(response, 200, result);
            }
            catch (ArgumentException ex) { await UnityMcpBridge.WriteJsonAsync(response, 400, new ErrorResponse { error = "Invalid UI invoke request", detail = ex.Message }); }
            catch (MissingMemberException ex) { await UnityMcpBridge.WriteJsonAsync(response, 404, new ErrorResponse { error = "UI element not found", detail = ex.Message }); }
            catch (InvalidOperationException ex) { await UnityMcpBridge.WriteJsonAsync(response, 409, new ErrorResponse { error = "UI invoke unavailable", detail = ex.Message }); }
            catch (TargetInvocationException ex)
            {
                await UnityMcpBridge.WriteJsonAsync(response, 500, new ErrorResponse
                {
                    error = "Method invocation failed",
                    detail = ex.InnerException?.Message ?? ex.Message,
                    stackTrace = ex.ToString()
                });
            }
        }

        private static UiInvokeResponse ExecuteUiInvoke(UiInvokeRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.path))
                throw new ArgumentException("path is required.");

            if (!EditorApplication.isPlaying)
                throw new InvalidOperationException("UI invoke requires play mode.");

            var gameObject = McpSharedHelpers.FindGameObjectByPath(req.path);
            if (gameObject == null)
                gameObject = GameObject.Find(req.path);
            if (gameObject == null)
                throw new MissingMemberException("GameObject not found: " + req.path);

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // method가 지정되지 않으면 기본 동작 (click)
            var method = string.IsNullOrEmpty(req.method) ? "click" : req.method.ToLowerInvariant();

            switch (method)
            {
                case "click":
                    return ExecuteClick(gameObject, stopwatch);

                case "submit":
                    return ExecuteSubmit(gameObject, stopwatch);

                case "value":
                    return ExecuteValueMethod(gameObject, req, stopwatch);

                case "custom":
                    if (string.IsNullOrEmpty(req.customMethod))
                        throw new ArgumentException("customMethod is required when method=custom");
                    return ExecuteCustomMethod(gameObject, req.customMethod, req.args, stopwatch);

                default:
                    throw new ArgumentException($"Unknown method: {method}. Use 'click', 'submit', 'value', or 'custom'.");
            }
        }

        private static UiInvokeResponse ExecuteClick(GameObject go, System.Diagnostics.Stopwatch stopwatch)
        {
            var path = McpSharedHelpers.GetTransformPath(go.transform);

            // 1. Button 컴포넌트 확인
            var button = go.GetComponent<Button>();
            if (button != null && button.IsActive())
            {
                if (!button.interactable)
                {
                    return new UiInvokeResponse
                    {
                        success = false,
                        message = "Button is not interactable",
                        path = path,
                        invokedMethod = "Button.onClick",
                        durationMs = stopwatch.ElapsedMilliseconds
                    };
                }

                // Button의 onClick 이벤트 실행
                button.onClick.Invoke();

                // 연결된 핸들러들도 실행 (코드로 바인딩된 것들)
                ExecuteCodedHandlers(go, "OnClick");

                return new UiInvokeResponse
                {
                    success = true,
                    message = "Invoked Button.onClick and coded handlers",
                    path = path,
                    invokedMethod = "Button.onClick",
                    durationMs = stopwatch.ElapsedMilliseconds
                };
            }

            // 2. IPointerClickHandler 구현 확인
            var pointerClickHandler = go.GetComponent<IPointerClickHandler>();
            if (pointerClickHandler != null)
            {
                var eventData = new PointerEventData(EventSystem.current)
                {
                    position = Vector2.zero
                };
                pointerClickHandler.OnPointerClick(eventData);
                return new UiInvokeResponse
                {
                    success = true,
                    message = "Invoked IPointerClickHandler",
                    path = path,
                    invokedMethod = "IPointerClickHandler.OnPointerClick",
                    durationMs = stopwatch.ElapsedMilliseconds
                };
            }

            // 3. 일반 MonoBehaviour의 OnClick 메서드 찾기
            var onClickMethod = FindAndInvokeMethod(go, "OnClick");
            if (onClickMethod != null)
            {
                return new UiInvokeResponse
                {
                    success = true,
                    message = "Invoked OnClick method",
                    path = path,
                    invokedMethod = "OnClick()",
                    result = onClickMethod,
                    durationMs = stopwatch.ElapsedMilliseconds
                };
            }

            throw new MissingMemberException($"No clickable component found on: {path}. Expected Button, IPointerClickHandler, or OnClick() method.");
        }

        private static UiInvokeResponse ExecuteSubmit(GameObject go, System.Diagnostics.Stopwatch stopwatch)
        {
            var path = McpSharedHelpers.GetTransformPath(go.transform);

            // InputField 확인
            var inputField = go.GetComponent<TMPro.TMP_InputField>();
            if (inputField != null)
            {
                inputField.onSubmit?.Invoke(inputField.text);
                return new UiInvokeResponse
                {
                    success = true,
                    message = "Invoked InputField.onSubmit",
                    path = path,
                    invokedMethod = "TMPro.TMP_InputField.onSubmit",
                    durationMs = stopwatch.ElapsedMilliseconds
                };
            }

            // 일반 OnSubmit 메서드 찾기
            var result = FindAndInvokeMethod(go, "OnSubmit", inputField?.text);
            return new UiInvokeResponse
            {
                success = true,
                message = "Invoked OnSubmit method",
                path = path,
                invokedMethod = "OnSubmit()",
                result = result,
                durationMs = stopwatch.ElapsedMilliseconds
            };
        }

        private static UiInvokeResponse ExecuteValueMethod(GameObject go, UiInvokeRequest req, System.Diagnostics.Stopwatch stopwatch)
        {
            var path = McpSharedHelpers.GetTransformPath(go.transform);

            // Toggle 확인
            var toggle = go.GetComponent<Toggle>();
            if (toggle != null)
            {
                var newValue = req.args != null && req.args.Length > 0 ? Convert.ToBoolean(req.args[0]) : !toggle.isOn;
                toggle.isOn = newValue;
                return new UiInvokeResponse
                {
                    success = true,
                    message = $"Set Toggle.isOn to {newValue}",
                    path = path,
                    invokedMethod = "Toggle.isOn",
                    result = newValue.ToString(),
                    durationMs = stopwatch.ElapsedMilliseconds
                };
            }

            // Slider 확인
            var slider = go.GetComponent<Slider>();
            if (slider != null)
            {
                var newValue = req.args != null && req.args.Length > 0 ? Convert.ToSingle(req.args[0]) : slider.value + 0.1f;
                slider.value = Mathf.Clamp(newValue, slider.minValue, slider.maxValue);
                return new UiInvokeResponse
                {
                    success = true,
                    message = $"Set Slider.value to {slider.value}",
                    path = path,
                    invokedMethod = "Slider.value",
                    result = slider.value.ToString(),
                    durationMs = stopwatch.ElapsedMilliseconds
                };
            }

            throw new MissingMemberException($"No value-assignable component found on: {path}. Expected Toggle, Slider, or custom method.");
        }

        private static UiInvokeResponse ExecuteCustomMethod(GameObject go, string methodName, object[] args, System.Diagnostics.Stopwatch stopwatch)
        {
            var path = McpSharedHelpers.GetTransformPath(go.transform);
            var result = FindAndInvokeMethod(go, methodName, args);
            return new UiInvokeResponse
            {
                success = true,
                message = $"Invoked custom method: {methodName}",
                path = path,
                invokedMethod = $"{methodName}()",
                result = result,
                durationMs = stopwatch.ElapsedMilliseconds
            };
        }

        // =====================================================================
        // UI Get State
        // =====================================================================

        public static async Task HandleUiGetStateAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
            var req = JsonUtility.FromJson<UiInvokeRequest>(body);

            try
            {
                var state = await UnityMcpBridge.RunOnMainThreadAsync(() => GetUiState(req));
                await UnityMcpBridge.WriteJsonAsync(response, 200, state);
            }
            catch (MissingMemberException ex)
            {
                await UnityMcpBridge.WriteJsonAsync(response, 404, new ErrorResponse { error = "UI element not found", detail = ex.Message });
            }
        }

        private static object GetUiState(UiInvokeRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.path))
                throw new ArgumentException("path is required.");

            var gameObject = McpSharedHelpers.FindGameObjectByPath(req.path);
            if (gameObject == null)
                gameObject = GameObject.Find(req.path);
            if (gameObject == null)
                throw new MissingMemberException("GameObject not found: " + req.path);

            var state = new Dictionary<string, object>
            {
                ["path"] = McpSharedHelpers.GetTransformPath(gameObject.transform),
                ["name"] = gameObject.name,
                ["activeSelf"] = gameObject.activeSelf,
                ["activeInHierarchy"] = gameObject.activeInHierarchy
            };

            // Button 상태
            var button = gameObject.GetComponent<Button>();
            if (button != null)
            {
                state["type"] = "Button";
                state["interactable"] = button.interactable;
                state["persistentListenerCount"] = button.onClick.GetPersistentEventCount();
            }

            // InputField 상태
            var inputField = gameObject.GetComponent<TMPro.TMP_InputField>();
            if (inputField != null)
            {
                state["type"] = "InputField";
                state["text"] = inputField.text;
                state["isFocused"] = inputField.isFocused;
                state["readOnly"] = inputField.readOnly;
            }

            // Toggle 상태
            var toggle = gameObject.GetComponent<Toggle>();
            if (toggle != null)
            {
                state["type"] = "Toggle";
                state["isOn"] = toggle.isOn;
                state["interactable"] = toggle.interactable;
            }

            // Slider 상태
            var slider = gameObject.GetComponent<Slider>();
            if (slider != null)
            {
                state["type"] = "Slider";
                state["value"] = slider.value;
                state["minValue"] = slider.minValue;
                state["maxValue"] = slider.maxValue;
            }

            // Text 상태
            var text = gameObject.GetComponent<TMPro.TMP_Text>();
            if (text != null)
            {
                state["type"] = "Text";
                state["text"] = text.text;
            }

            return new { success = true, state = state };
        }

        // =====================================================================
        // UI Set Value
        // =====================================================================

        public static async Task HandleUiSetValueAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
            var req = JsonUtility.FromJson<UiSetValueRequest>(body);

            try
            {
                var result = await UnityMcpBridge.RunOnMainThreadAsync(() => SetUiValue(req));
                await UnityMcpBridge.WriteJsonAsync(response, 200, result);
            }
            catch (MissingMemberException ex)
            {
                await UnityMcpBridge.WriteJsonAsync(response, 404, new ErrorResponse { error = "UI element not found", detail = ex.Message });
            }
        }

        private static object SetUiValue(UiSetValueRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.path))
                throw new ArgumentException("path is required.");

            var gameObject = McpSharedHelpers.FindGameObjectByPath(req.path);
            if (gameObject == null)
                gameObject = GameObject.Find(req.path);
            if (gameObject == null)
                throw new MissingMemberException("GameObject not found: " + req.path);

            var path = McpSharedHelpers.GetTransformPath(gameObject.transform);

            // InputField 설정
            var inputField = gameObject.GetComponent<TMPro.TMP_InputField>();
            if (inputField != null)
            {
                inputField.text = req.value ?? string.Empty;
                return new { success = true, message = $"Set InputField.text to '{req.value}'", path = path };
            }

            // Toggle 설정
            var toggle = gameObject.GetComponent<Toggle>();
            if (toggle != null)
            {
                toggle.isOn = req.value == "true" || req.value == "1";
                return new { success = true, message = $"Set Toggle.isOn to {toggle.isOn}", path = path };
            }

            // Slider 설정
            var slider = gameObject.GetComponent<Slider>();
            if (slider != null && float.TryParse(req.value, out var floatValue))
            {
                slider.value = Mathf.Clamp(floatValue, slider.minValue, slider.maxValue);
                return new { success = true, message = $"Set Slider.value to {slider.value}", path = path };
            }

            // Text 설정
            var text = gameObject.GetComponent<TMPro.TMP_Text>();
            if (text != null)
            {
                text.text = req.value ?? string.Empty;
                return new { success = true, message = $"Set Text.text to '{req.value}'", path = path };
            }

            throw new MissingMemberException($"No settable component found on: {path}");
        }

        // =====================================================================
        // UI List Handlers
        // =====================================================================

        public static async Task HandleUiListHandlersAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
            var req = JsonUtility.FromJson<UiInvokeRequest>(body);

            try
            {
                var handlers = await UnityMcpBridge.RunOnMainThreadAsync(() => ListUiHandlers(req));
                await UnityMcpBridge.WriteJsonAsync(response, 200, handlers);
            }
            catch (MissingMemberException ex)
            {
                await UnityMcpBridge.WriteJsonAsync(response, 404, new ErrorResponse { error = "UI element not found", detail = ex.Message });
            }
        }

        private static object ListUiHandlers(UiInvokeRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.path))
                throw new ArgumentException("path is required.");

            var gameObject = McpSharedHelpers.FindGameObjectByPath(req.path);
            if (gameObject == null)
                gameObject = GameObject.Find(req.path);
            if (gameObject == null)
                throw new MissingMemberException("GameObject not found: " + req.path);

            var path = McpSharedHelpers.GetTransformPath(gameObject.transform);
            var handlers = new List<object>();

            // Button의 Unity 이벤트 리스너
            var button = gameObject.GetComponent<Button>();
            if (button != null)
            {
                var persistentCount = button.onClick.GetPersistentEventCount();
                for (int i = 0; i < persistentCount; i++)
                {
                    var target = button.onClick.GetPersistentTarget(i);
                    var methodName = button.onClick.GetPersistentMethodName(i);
                    handlers.Add(new
                    {
                        type = "UnityEvent",
                        target = target != null ? target.GetType().Name : "null",
                        method = methodName,
                        index = i
                    });
                }
            }

            // MonoBehaviour의 OnClick/OnSubmit 등 메서드 찾기
            var components = gameObject.GetComponents<MonoBehaviour>();
            foreach (var comp in components)
            {
                if (comp == null) continue;

                var type = comp.GetType();
                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .Where(m =>
                        !m.IsSpecialName && // getter/setter 제외
                        m.ReturnType == typeof(void) &&
                        m.GetParameters().Length == 0 &&
                        (m.Name.StartsWith("On") || m.Name.Contains("Click") || m.Name.Contains("Submit")))
                    .ToList();

                foreach (var method in methods)
                {
                    handlers.Add(new
                    {
                        type = "MonoBehaviour",
                        component = type.Name,
                        method = method.Name,
                        isPublic = method.IsPublic
                    });
                }
            }

            return new
            {
                success = true,
                path = path,
                count = handlers.Count,
                handlers = handlers
            };
        }

        // =====================================================================
        // Helper Methods
        // =====================================================================

        private static string FindAndInvokeMethod(GameObject go, string methodName, params object[] args)
        {
            var components = go.GetComponents<MonoBehaviour>();
            foreach (var comp in components)
            {
                if (comp == null) continue;

                var method = comp.GetType().GetMethod(methodName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if (method != null)
                {
                    try
                    {
                        var parameters = method.GetParameters();
                        var invokeArgs = new object[parameters.Length];

                        for (int i = 0; i < parameters.Length; i++)
                        {
                            if (i < args.Length && args[i] != null)
                            {
                                invokeArgs[i] = Convert.ChangeType(args[i], parameters[i].ParameterType);
                            }
                            else if (parameters[i].ParameterType == typeof(string))
                            {
                                invokeArgs[i] = string.Empty;
                            }
                            else
                            {
                                invokeArgs[i] = null;
                            }
                        }

                        var result = method.Invoke(comp, invokeArgs);

                        // 코루틴 반환값 처리
                        if (method.ReturnType == typeof(IEnumerator) && result is IEnumerator coroutine)
                        {
                            comp.StartCoroutine(coroutine);
                            return $"Coroutine {methodName} started";
                        }

                        return $"{methodName}() invoked successfully";
                    }
                    catch (TargetInvocationException ex)
                    {
                        Debug.LogError($"[MCP] Error invoking {methodName} on {comp.GetType().Name}: {ex.InnerException?.Message ?? ex.Message}");
                        throw;
                    }
                }
            }

            return null;
        }

        private static void ExecuteCodedHandlers(GameObject go, string baseMethodName)
        {
            var components = go.GetComponents<MonoBehaviour>();
            foreach (var comp in components)
            {
                if (comp == null) continue;

                var method = comp.GetType().GetMethod(baseMethodName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if (method != null)
                {
                    try
                    {
                        method.Invoke(comp, null);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[MCP] Failed to invoke {baseMethodName} on {comp.GetType().Name}: {ex.Message}");
                    }
                }
            }
        }

        // =====================================================================
        // Legacy Handlers (호환성 유지)
        // =====================================================================

        public static async Task HandleUiButtonInvokeAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
            var req = JsonUtility.FromJson<UiButtonInvokeRequest>(body);

            try
            {
                var result = await UnityMcpBridge.RunOnMainThreadAsync(() => ExecuteUiButtonInvoke(req));
                await UnityMcpBridge.WriteJsonAsync(response, 200, result);
            }
            catch (ArgumentException ex) { await UnityMcpBridge.WriteJsonAsync(response, 400, new ErrorResponse { error = "Invalid button invoke request", detail = ex.Message }); }
            catch (MissingMemberException ex) { await UnityMcpBridge.WriteJsonAsync(response, 404, new ErrorResponse { error = "Button not found", detail = ex.Message }); }
            catch (InvalidOperationException ex) { await UnityMcpBridge.WriteJsonAsync(response, 409, new ErrorResponse { error = "Button invoke unavailable", detail = ex.Message }); }
        }

        private static UiButtonInvokeResponse ExecuteUiButtonInvoke(UiButtonInvokeRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.path)) throw new ArgumentException("path is required.");
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("UI button invoke requires play mode.");
            var gameObject = McpSharedHelpers.FindGameObjectByPath(req.path);
            if (gameObject == null) gameObject = GameObject.Find(req.path);
            if (gameObject == null) throw new MissingMemberException("GameObject not found: " + req.path);
            var button = gameObject.GetComponent<Button>();
            if (button == null) throw new MissingMemberException("Button component not found on: " + req.path);
            button.onClick.Invoke();
            return new UiButtonInvokeResponse
            {
                success = true, message = "Invoked Button.onClick: " + req.path, path = McpSharedHelpers.GetTransformPath(gameObject.transform),
                name = gameObject.name, activeSelf = gameObject.activeSelf, activeInHierarchy = gameObject.activeInHierarchy,
                interactable = button.interactable, persistentListenerCount = button.onClick.GetPersistentEventCount()
            };
        }

        // 나머지 legacy 핸들러는 기존 UiHandlers.cs 그대로 사용
        public static async Task HandleUiCreatePanelAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
            var req = JsonUtility.FromJson<UiCreatePanelRequest>(body);

            string createdPath = null;
            string createdName = null;
            int enqueueResult = 0;
            string enqueueError = null;

            await UnityMcpBridge.RunOnMainThreadAsync(() =>
            {
                enqueueResult = SceneChangeQueue.EnqueueOrExecute(() =>
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
                    var image = go.AddComponent<Image>();
                    image.color = new Color(0.1f, 0.1f, 0.15f, 0.9f);
                    Undo.RegisterCreatedObjectUndo(go, "MCP Create UI Panel");
                    EditorSceneManager.MarkSceneDirty(go.scene);
                    createdPath = McpSharedHelpers.GetTransformPath(go.transform);
                    createdName = go.name;
                    return true;
                }, "Create UI Panel: " + (string.IsNullOrEmpty(req.name) ? "Panel" : req.name), out enqueueError);
            });

            if (enqueueResult == -1)
            {
                await UnityMcpBridge.WriteJsonAsync(response, 500, new ErrorResponse
                {
                    error = "Scene change failed",
                    detail = enqueueError,
                    hint = UnityMcpBridge.GetErrorHintFromMessage(enqueueError)
                });
                return;
            }

            var wasQueued = enqueueResult == 0;
            var result = new UiCreatePanelResponse
            {
                success = true, message = "Created UI Panel: " + createdName, path = createdPath, name = createdName,
                queued = wasQueued, pendingCount = wasQueued ? SceneChangeQueue.PendingCount : 0,
                autoSaved = !wasQueued && req.autoSave && McpSharedHelpers.TryAutoSave()
            };
            await UnityMcpBridge.WriteJsonAsync(response, 200, result);
        }

        public static async Task HandleUiCreateRawImageAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
            var req = JsonUtility.FromJson<UiCreateRawImageRequest>(body);

            string createdPath = null;
            string createdName = null;
            int enqueueResult = 0;
            string enqueueError = null;

            await UnityMcpBridge.RunOnMainThreadAsync(() =>
            {
                enqueueResult = SceneChangeQueue.EnqueueOrExecute(() =>
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
                    var rawImage = go.AddComponent<RawImage>();
                    rawImage.color = new Color(0.05f, 0.06f, 0.1f, 1f);
                    Undo.RegisterCreatedObjectUndo(go, "MCP Create UI RawImage");
                    EditorSceneManager.MarkSceneDirty(go.scene);
                    createdPath = McpSharedHelpers.GetTransformPath(go.transform);
                    createdName = go.name;
                    return true;
                }, "Create UI RawImage: " + (string.IsNullOrEmpty(req.name) ? "RawImage" : req.name), out enqueueError);
            });

            if (enqueueResult == -1)
            {
                await UnityMcpBridge.WriteJsonAsync(response, 500, new ErrorResponse
                {
                    error = "Scene change failed",
                    detail = enqueueError,
                    hint = UnityMcpBridge.GetErrorHintFromMessage(enqueueError)
                });
                return;
            }

            var wasQueued = enqueueResult == 0;
            var result = new UiCreateRawImageResponse
            {
                success = true, message = "Created UI RawImage: " + createdName, path = createdPath, name = createdName,
                queued = wasQueued, pendingCount = wasQueued ? SceneChangeQueue.PendingCount : 0,
                autoSaved = !wasQueued && req.autoSave && McpSharedHelpers.TryAutoSave()
            };
            await UnityMcpBridge.WriteJsonAsync(response, 200, result);
        }

        public static async Task HandleUiSetRectAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
            var req = JsonUtility.FromJson<UiSetRectRequest>(body);

            string updatedPath = null;
            int enqueueResult = 0;
            string enqueueError = null;

            await UnityMcpBridge.RunOnMainThreadAsync(() =>
            {
                enqueueResult = SceneChangeQueue.EnqueueOrExecute(() =>
                {
                    var go = McpSharedHelpers.FindGameObjectByPath(req.path);
                    if (go == null) throw new Exception("GameObject not found: " + req.path);
                    var rect = go.GetComponent<RectTransform>();
                    if (rect == null) throw new Exception("RectTransform not found on: " + req.path);
                    if (req.anchoredPositionX.HasValue || req.anchoredPositionY.HasValue)
                    {
                        var pos = rect.anchoredPosition;
                        if (req.anchoredPositionX.HasValue) pos.x = req.anchoredPositionX.Value;
                        if (req.anchoredPositionY.HasValue) pos.y = req.anchoredPositionY.Value;
                        rect.anchoredPosition = pos;
                    }
                    if (req.sizeDeltaX.HasValue || req.sizeDeltaY.HasValue)
                    {
                        var size = rect.sizeDelta;
                        if (req.sizeDeltaX.HasValue) size.x = req.sizeDeltaX.Value;
                        if (req.sizeDeltaY.HasValue) size.y = req.sizeDeltaY.Value;
                        rect.sizeDelta = size;
                    }
                    if (req.anchorMinX.HasValue || req.anchorMinY.HasValue)
                    {
                        var min = rect.anchorMin;
                        if (req.anchorMinX.HasValue) min.x = req.anchorMinX.Value;
                        if (req.anchorMinY.HasValue) min.y = req.anchorMinY.Value;
                        rect.anchorMin = min;
                    }
                    if (req.anchorMaxX.HasValue || req.anchorMaxY.HasValue)
                    {
                        var max = rect.anchorMax;
                        if (req.anchorMaxX.HasValue) max.x = req.anchorMaxX.Value;
                        if (req.anchorMaxY.HasValue) max.y = req.anchorMaxY.Value;
                        rect.anchorMax = max;
                    }
                    if (req.pivotX.HasValue || req.pivotY.HasValue)
                    {
                        var pivot = rect.pivot;
                        if (req.pivotX.HasValue) pivot.x = req.pivotX.Value;
                        if (req.pivotY.HasValue) pivot.y = req.pivotY.Value;
                        rect.pivot = pivot;
                    }
                    Undo.RegisterCompleteObjectUndo(rect, "MCP Set Rect");
                    EditorSceneManager.MarkSceneDirty(go.scene);
                    updatedPath = McpSharedHelpers.GetTransformPath(go.transform);
                    return true;
                }, "Set Rect: " + req.path, out enqueueError);
            });

            if (enqueueResult == -1)
            {
                await UnityMcpBridge.WriteJsonAsync(response, 500, new ErrorResponse
                {
                    error = "Scene change failed",
                    detail = enqueueError,
                    hint = UnityMcpBridge.GetErrorHintFromMessage(enqueueError)
                });
                return;
            }

            var wasQueued = enqueueResult == 0;
            var result = new UiSetRectResponse
            {
                success = true, message = "RectTransform updated: " + req.path, path = updatedPath,
                queued = wasQueued, pendingCount = wasQueued ? SceneChangeQueue.PendingCount : 0,
                autoSaved = !wasQueued && req.autoSave && McpSharedHelpers.TryAutoSave()
            };
            await UnityMcpBridge.WriteJsonAsync(response, 200, result);
        }

        public static async Task HandleUiCreateButtonAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
            var req = JsonUtility.FromJson<UiCreateButtonRequest>(body);

            string createdPath = null;
            string createdName = null;
            int enqueueResult = 0;
            string enqueueError = null;

            await UnityMcpBridge.RunOnMainThreadAsync(() =>
            {
                enqueueResult = SceneChangeQueue.EnqueueOrExecute(() =>
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
                    var image = go.AddComponent<Image>();
                    image.color = new Color(0.2f, 0.4f, 0.9f, 1f);
                    image.type = Image.Type.Sliced;
                    go.AddComponent<Button>();
                    var textGo = new GameObject("Text (TMP)");
                    textGo.transform.SetParent(go.transform, false);
                    var textRect = textGo.AddComponent<RectTransform>();
                    textRect.anchorMin = Vector2.zero;
                    textRect.anchorMax = Vector2.one;
                    textRect.sizeDelta = Vector2.zero;
                    var tmpType = UnityMcpBridge.ResolveTypeByFullName("TMPro.TextMeshProUGUI");
                    if (tmpType != null && typeof(TMPro.TMP_Text).IsAssignableFrom(tmpType))
                    {
                        var tmpText = (TMPro.TMP_Text)textGo.AddComponent(tmpType);
                        tmpText.text = string.IsNullOrEmpty(req.buttonText) ? "Button" : req.buttonText;
                        tmpText.fontSize = 14;
                        tmpText.alignment = TMPro.TextAlignmentOptions.Center;
                        tmpText.color = Color.white;
                    }
                    Undo.RegisterCreatedObjectUndo(go, "MCP Create UI Button");
                    EditorSceneManager.MarkSceneDirty(go.scene);
                    createdPath = McpSharedHelpers.GetTransformPath(go.transform);
                    createdName = go.name;
                    return true;
                }, "Create UI Button: " + (string.IsNullOrEmpty(req.name) ? "Button" : req.name), out enqueueError);
            });

            if (enqueueResult == -1)
            {
                await UnityMcpBridge.WriteJsonAsync(response, 500, new ErrorResponse
                {
                    error = "Scene change failed",
                    detail = enqueueError,
                    hint = UnityMcpBridge.GetErrorHintFromMessage(enqueueError)
                });
                return;
            }

            var wasQueued = enqueueResult == 0;
            var result = new UiCreateButtonResponse
            {
                success = true, message = "Created UI Button: " + createdName, path = createdPath, name = createdName,
                queued = wasQueued, pendingCount = wasQueued ? SceneChangeQueue.PendingCount : 0,
                autoSaved = !wasQueued && req.autoSave && McpSharedHelpers.TryAutoSave()
            };
            await UnityMcpBridge.WriteJsonAsync(response, 200, result);
        }
    }

    // =====================================================================
    // 추가 DTO
    // =====================================================================

    [Serializable]
    internal class UiSetValueRequest
    {
        public string path;
        public string value;
    }
}
#endif
