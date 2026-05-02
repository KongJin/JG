#if UNITY_EDITOR
#pragma warning disable CS0649 // Request DTO fields are populated by Unity JsonUtility.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace ProjectSD.EditorTools.UnityMcp
{
    internal static class UitkHandlers
    {
        private const int DefaultMaxDepth = 6;

        static UitkHandlers()
        {
            "GET".Register("/uitk/state", "List UI Toolkit UIDocuments and VisualElement trees.", async (req, res) => await HandleUitkStateAsync(req, res));
            "POST".Register("/uitk/get-state", "Read a UI Toolkit VisualElement state by UIDocument and element name/path.", async (req, res) => await HandleUitkGetStateAsync(req, res));
            "POST".Register("/uitk/set-value", "Set a UI Toolkit field/text value by UIDocument and element name/path.", async (req, res) => await HandleUitkSetValueAsync(req, res));
            "POST".Register("/uitk/invoke", "Invoke a UI Toolkit VisualElement action such as click, focus, or value.", async (req, res) => await HandleUitkInvokeAsync(req, res));
            "POST".Register("/uitk/wait-for-element", "Wait for a UI Toolkit VisualElement to exist or match text.", async (req, res) => await HandleUitkWaitForElementAsync(req, res));
        }

        public static async Task HandleUitkStateAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var documentPath = request.QueryString["documentPath"];
            var documentName = request.QueryString["documentName"];
            var maxDepth = ParseClampedInt(request.QueryString["maxDepth"], DefaultMaxDepth, 1, 50);

            try
            {
                var result = await UnityMcpBridge.RunOnMainThreadAsync(() =>
                {
                    var documents = ResolveDocuments(documentPath, documentName)
                        .Select(document => BuildDocumentState(document, maxDepth))
                        .ToArray();

                    return new UitkDocumentsResponse
                    {
                        success = true,
                        sceneName = SceneManager.GetActiveScene().name,
                        count = documents.Length,
                        documents = documents
                    };
                });

                await UnityMcpBridge.WriteJsonAsync(response, 200, result);
            }
            catch (Exception ex)
            {
                await WriteUitkErrorAsync(response, 400, "UITK state unavailable", ex);
            }
        }

        public static async Task HandleUitkGetStateAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var req = await ReadJsonRequestAsync<UitkElementRequest>(request);

            try
            {
                var result = await UnityMcpBridge.RunOnMainThreadAsync(() =>
                {
                    var document = ResolveSingleDocument(req);
                    var element = ResolveElement(document.rootVisualElement, req.elementName, req.elementPath);
                    if (element == null)
                        throw new MissingMemberException(BuildMissingElementMessage(req));

                    return new UitkElementStateResponse
                    {
                        success = true,
                        documentPath = McpSharedHelpers.GetTransformPath(document.transform),
                        documentName = document.gameObject.name,
                        state = BuildElementState(element)
                    };
                });

                await UnityMcpBridge.WriteJsonAsync(response, 200, result);
            }
            catch (MissingMemberException ex)
            {
                await WriteUitkErrorAsync(response, 404, "UITK element not found", ex);
            }
            catch (Exception ex)
            {
                await WriteUitkErrorAsync(response, 400, "Invalid UITK get-state request", ex);
            }
        }

        public static async Task HandleUitkSetValueAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var req = await ReadJsonRequestAsync<UitkElementRequest>(request);

            try
            {
                var result = await UnityMcpBridge.RunOnMainThreadAsync(() =>
                {
                    var document = ResolveSingleDocument(req);
                    var element = ResolveElementOrThrow(document.rootVisualElement, req);
                    var changed = SetElementValue(element, req.value);
                    return new UitkActionResponse
                    {
                        success = true,
                        documentPath = McpSharedHelpers.GetTransformPath(document.transform),
                        documentName = document.gameObject.name,
                        elementName = element.name,
                        elementType = element.GetType().Name,
                        invokedMethod = changed,
                        message = "Set UI Toolkit value"
                    };
                });

                await UnityMcpBridge.WriteJsonAsync(response, 200, result);
            }
            catch (MissingMemberException ex)
            {
                await WriteUitkErrorAsync(response, 404, "UITK element not found", ex);
            }
            catch (Exception ex)
            {
                await WriteUitkErrorAsync(response, 400, "Invalid UITK set-value request", ex);
            }
        }

        public static async Task HandleUitkInvokeAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var req = await ReadJsonRequestAsync<UitkElementRequest>(request);

            try
            {
                var result = await UnityMcpBridge.RunOnMainThreadAsync(() =>
                {
                    var document = ResolveSingleDocument(req);
                    var element = ResolveElementOrThrow(document.rootVisualElement, req);
                    var invoked = InvokeElement(element, req.method, req.value);
                    return new UitkActionResponse
                    {
                        success = true,
                        documentPath = McpSharedHelpers.GetTransformPath(document.transform),
                        documentName = document.gameObject.name,
                        elementName = element.name,
                        elementType = element.GetType().Name,
                        invokedMethod = invoked,
                        message = "Invoked UI Toolkit element"
                    };
                });

                await UnityMcpBridge.WriteJsonAsync(response, 200, result);
            }
            catch (MissingMemberException ex)
            {
                await WriteUitkErrorAsync(response, 404, "UITK element not found", ex);
            }
            catch (InvalidOperationException ex)
            {
                await WriteUitkErrorAsync(response, 409, "UITK invoke unavailable", ex);
            }
            catch (Exception ex)
            {
                await WriteUitkErrorAsync(response, 400, "Invalid UITK invoke request", ex);
            }
        }

        public static async Task HandleUitkWaitForElementAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var req = await ReadJsonRequestAsync<UitkElementRequest>(request);
            var timeoutMs = Math.Max(100, req != null && req.timeoutMs > 0 ? req.timeoutMs : 10000);
            var pollIntervalMs = Math.Max(20, req != null && req.pollIntervalMs > 0 ? req.pollIntervalMs : 100);
            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            var started = DateTime.UtcNow;
            Exception lastError = null;

            while (DateTime.UtcNow < deadline)
            {
                try
                {
                    var result = await UnityMcpBridge.RunOnMainThreadAsync(() =>
                    {
                        var document = ResolveSingleDocument(req);
                        var element = ResolveElement(document.rootVisualElement, req.elementName, req.elementPath);
                        if (element == null)
                            return null;

                        if (!string.IsNullOrEmpty(req.expectedText))
                        {
                            var text = ExtractText(element) ?? string.Empty;
                            var matches = req.exact
                                ? string.Equals(text, req.expectedText, StringComparison.Ordinal)
                                : text.IndexOf(req.expectedText, StringComparison.Ordinal) >= 0;
                            if (!matches)
                                return null;
                        }

                        return new UitkElementStateResponse
                        {
                            success = true,
                            documentPath = McpSharedHelpers.GetTransformPath(document.transform),
                            documentName = document.gameObject.name,
                            state = BuildElementState(element)
                        };
                    });

                    if (result != null)
                    {
                        result.waitedMs = (int)(DateTime.UtcNow - started).TotalMilliseconds;
                        await UnityMcpBridge.WriteJsonAsync(response, 200, result);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    lastError = ex;
                }

                await Task.Delay(pollIntervalMs);
            }

            await UnityMcpBridge.WriteJsonAsync(response, 408, new ErrorResponse
            {
                error = "UITK wait timed out",
                detail = lastError != null ? lastError.Message : BuildMissingElementMessage(req),
                hint = "Use GET /uitk/state to inspect UIDocument and VisualElement names."
            });
        }

        internal static VisualElement FindElementForTest(VisualElement root, string elementName, string elementPath)
        {
            return ResolveElement(root, elementName, elementPath);
        }

        internal static UitkElementState BuildElementStateForTest(VisualElement element)
        {
            return BuildElementState(element);
        }

        internal static string SetElementValueForTest(VisualElement element, string value)
        {
            return SetElementValue(element, value);
        }

        internal static string InvokeElementForTest(VisualElement element, string method, string value = null)
        {
            return InvokeElement(element, method, value);
        }

        private static UIDocument[] ResolveDocuments(string documentPath, string documentName)
        {
            var documents = UnityEngine.Object.FindObjectsByType<UIDocument>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            if (!string.IsNullOrEmpty(documentPath))
            {
                var go = McpSharedHelpers.FindGameObjectByPath(documentPath);
                var document = go != null ? go.GetComponent<UIDocument>() : null;
                return document != null ? new[] { document } : Array.Empty<UIDocument>();
            }

            if (!string.IsNullOrEmpty(documentName))
                return documents.Where(document => document != null && document.gameObject.name == documentName).ToArray();

            return documents.Where(document => document != null).ToArray();
        }

        private static UIDocument ResolveSingleDocument(UitkElementRequest req)
        {
            if (req == null)
                throw new ArgumentException("request body is required.");

            var documents = ResolveDocuments(req.documentPath, req.documentName);
            if (documents.Length == 1)
                return documents[0];

            if (documents.Length == 0)
                throw new MissingMemberException("UIDocument not found. Provide documentPath or documentName.");

            if (string.IsNullOrEmpty(req.documentPath) && string.IsNullOrEmpty(req.documentName))
                throw new InvalidOperationException("Multiple UIDocuments are loaded. Provide documentPath or documentName.");

            throw new InvalidOperationException("UIDocument selector matched multiple documents. Use a full documentPath.");
        }

        private static VisualElement ResolveElementOrThrow(VisualElement root, UitkElementRequest req)
        {
            var element = ResolveElement(root, req.elementName, req.elementPath);
            if (element == null)
                throw new MissingMemberException(BuildMissingElementMessage(req));
            return element;
        }

        private static VisualElement ResolveElement(VisualElement root, string elementName, string elementPath)
        {
            if (root == null)
                return null;

            if (!string.IsNullOrEmpty(elementPath))
                return ResolveElementPath(root, elementPath);

            if (!string.IsNullOrEmpty(elementName))
                return root.Q<VisualElement>(elementName);

            return root;
        }

        private static VisualElement ResolveElementPath(VisualElement root, string elementPath)
        {
            var parts = elementPath
                .Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(part => !string.IsNullOrWhiteSpace(part))
                .ToArray();

            if (parts.Length == 0)
                return root;

            var current = root;
            var index = string.Equals(current.name, parts[0], StringComparison.Ordinal) ? 1 : 0;
            for (var i = index; i < parts.Length; i++)
            {
                var next = current.Children().FirstOrDefault(child => string.Equals(child.name, parts[i], StringComparison.Ordinal));
                if (next == null)
                    return null;
                current = next;
            }

            return current;
        }

        private static UitkDocumentState BuildDocumentState(UIDocument document, int maxDepth)
        {
            var root = document.rootVisualElement;
            return new UitkDocumentState
            {
                name = document.gameObject.name,
                path = McpSharedHelpers.GetTransformPath(document.transform),
                activeInHierarchy = document.gameObject.activeInHierarchy,
                rootName = root != null ? root.name : string.Empty,
                panelSettings = document.panelSettings != null ? document.panelSettings.name : string.Empty,
                sourceAsset = document.visualTreeAsset != null ? document.visualTreeAsset.name : string.Empty,
                children = root != null ? BuildElementTree(root, 0, maxDepth) : Array.Empty<UitkElementNode>()
            };
        }

        private static UitkElementNode[] BuildElementTree(VisualElement element, int depth, int maxDepth)
        {
            if (element == null || depth >= maxDepth)
                return Array.Empty<UitkElementNode>();

            return element.Children()
                .Select(child => new UitkElementNode
                {
                    name = child.name,
                    type = child.GetType().Name,
                    text = ExtractText(child) ?? string.Empty,
                    enabledInHierarchy = child.enabledInHierarchy,
                    display = child.resolvedStyle.display.ToString(),
                    classList = child.GetClasses().ToArray(),
                    childCount = child.childCount,
                    children = BuildElementTree(child, depth + 1, maxDepth)
                })
                .ToArray();
        }

        private static UitkElementState BuildElementState(VisualElement element)
        {
            var bound = element.worldBound;
            return new UitkElementState
            {
                name = element.name,
                type = element.GetType().Name,
                text = ExtractText(element) ?? string.Empty,
                value = ExtractValue(element) ?? string.Empty,
                enabledSelf = element.enabledSelf,
                enabledInHierarchy = element.enabledInHierarchy,
                visible = element.visible,
                display = element.resolvedStyle.display.ToString(),
                classList = element.GetClasses().ToArray(),
                childCount = element.childCount,
                worldX = bound.x,
                worldY = bound.y,
                worldWidth = bound.width,
                worldHeight = bound.height,
                childNames = element.Children().Select(child => child.name).ToArray()
            };
        }

        private static string ExtractText(VisualElement element)
        {
            if (element is TextElement textElement)
                return textElement.text;

            return null;
        }

        private static string ExtractValue(VisualElement element)
        {
            switch (element)
            {
                case TextField textField:
                    return textField.value;
                case IntegerField integerField:
                    return integerField.value.ToString();
                case FloatField floatField:
                    return floatField.value.ToString("R");
                case Toggle toggle:
                    return toggle.value ? "true" : "false";
                case DropdownField dropdownField:
                    return dropdownField.value;
                default:
                    return null;
            }
        }

        private static string SetElementValue(VisualElement element, string value)
        {
            switch (element)
            {
                case TextField textField:
                    textField.value = value ?? string.Empty;
                    return "TextField.value";
                case IntegerField integerField:
                    if (!int.TryParse(value, out var intValue))
                        throw new ArgumentException("IntegerField value must be an integer.");
                    integerField.value = intValue;
                    return "IntegerField.value";
                case FloatField floatField:
                    if (!float.TryParse(value, out var floatValue))
                        throw new ArgumentException("FloatField value must be a float.");
                    floatField.value = floatValue;
                    return "FloatField.value";
                case Toggle toggle:
                    toggle.value = IsTruthy(value);
                    return "Toggle.value";
                case DropdownField dropdownField:
                    dropdownField.value = value ?? string.Empty;
                    return "DropdownField.value";
                case TextElement textElement:
                    textElement.text = value ?? string.Empty;
                    return "TextElement.text";
                default:
                    throw new MissingMemberException("VisualElement is not value-assignable: " + element.GetType().Name);
            }
        }

        private static string InvokeElement(VisualElement element, string method, string value)
        {
            var normalized = string.IsNullOrEmpty(method) ? "click" : method.ToLowerInvariant();
            switch (normalized)
            {
                case "click":
                    if (!element.enabledInHierarchy)
                        throw new InvalidOperationException("VisualElement is disabled: " + element.name);

                    if (element is Button button && TryInvokeButtonClicked(button))
                        return "Button.clicked";

                    using (var evt = ClickEvent.GetPooled())
                    {
                        element.SendEvent(evt);
                    }

                    return "ClickEvent";
                case "focus":
                    element.Focus();
                    return "VisualElement.Focus";
                case "value":
                    return SetElementValue(element, value);
                default:
                    throw new ArgumentException("Unknown UITK method: " + method + ". Use click, focus, or value.");
            }
        }

        private static bool TryInvokeButtonClicked(Button button)
        {
            var clickable = button.clickable;
            if (clickable == null)
                return false;

            var type = clickable.GetType();
            while (type != null)
            {
                var fields = type.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                foreach (var field in fields)
                {
                    if (field.GetValue(clickable) is Action action)
                    {
                        action.Invoke();
                        return true;
                    }
                }

                type = type.BaseType;
            }

            return false;
        }

        private static bool IsTruthy(string value)
        {
            return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, "1", StringComparison.Ordinal) ||
                   string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, "on", StringComparison.OrdinalIgnoreCase);
        }

        private static int ParseClampedInt(string raw, int defaultValue, int min, int max)
        {
            if (!int.TryParse(raw, out var value))
                return defaultValue;
            return Mathf.Clamp(value, min, max);
        }

        private static string BuildMissingElementMessage(UitkElementRequest req)
        {
            if (req == null)
                return "UITK request body is missing.";
            if (!string.IsNullOrEmpty(req.elementPath))
                return "VisualElement path not found: " + req.elementPath;
            if (!string.IsNullOrEmpty(req.elementName))
                return "VisualElement name not found: " + req.elementName;
            return "VisualElement selector is missing. Provide elementName or elementPath.";
        }

        private static async Task<T> ReadJsonRequestAsync<T>(HttpListenerRequest request) where T : class
        {
            if (!request.HasEntityBody)
                return null;

            var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
            return string.IsNullOrWhiteSpace(body) ? null : JsonUtility.FromJson<T>(body);
        }

        private static async Task WriteUitkErrorAsync(HttpListenerResponse response, int statusCode, string error, Exception ex)
        {
            await UnityMcpBridge.WriteJsonAsync(response, statusCode, new ErrorResponse
            {
                error = error,
                detail = ex.Message,
                stackTrace = ex.ToString(),
                hint = "Use GET /uitk/state to inspect UIDocument and VisualElement names. UGUI /ui routes are disabled in this project."
            });
        }
    }

    [Serializable]
    internal sealed class UitkElementRequest
    {
        public string documentPath;
        public string documentName;
        public string elementName;
        public string elementPath;
        public string method;
        public string value;
        public string expectedText;
        public bool exact;
        public int timeoutMs;
        public int pollIntervalMs;
    }

    [Serializable]
    internal sealed class UitkDocumentsResponse
    {
        public bool success;
        public string sceneName;
        public int count;
        public UitkDocumentState[] documents;
    }

    [Serializable]
    internal sealed class UitkDocumentState
    {
        public string name;
        public string path;
        public bool activeInHierarchy;
        public string rootName;
        public string panelSettings;
        public string sourceAsset;
        public UitkElementNode[] children;
    }

    [Serializable]
    internal sealed class UitkElementNode
    {
        public string name;
        public string type;
        public string text;
        public bool enabledInHierarchy;
        public string display;
        public string[] classList;
        public int childCount;
        public UitkElementNode[] children;
    }

    [Serializable]
    internal sealed class UitkElementStateResponse
    {
        public bool success;
        public string documentPath;
        public string documentName;
        public int waitedMs;
        public UitkElementState state;
    }

    [Serializable]
    internal sealed class UitkElementState
    {
        public string name;
        public string type;
        public string text;
        public string value;
        public bool enabledSelf;
        public bool enabledInHierarchy;
        public bool visible;
        public string display;
        public string[] classList;
        public int childCount;
        public float worldX;
        public float worldY;
        public float worldWidth;
        public float worldHeight;
        public string[] childNames;
    }

    [Serializable]
    internal sealed class UitkActionResponse
    {
        public bool success;
        public string documentPath;
        public string documentName;
        public string elementName;
        public string elementType;
        public string invokedMethod;
        public string message;
    }
}
#pragma warning restore CS0649
#endif
