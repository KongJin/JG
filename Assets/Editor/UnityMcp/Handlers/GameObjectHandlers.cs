#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectSD.EditorTools.UnityMcp
{
    internal static class GameObjectHandlers
    {
        public static async Task HandleGameObjectFindAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
            var req = JsonUtility.FromJson<FindRequest>(body);

            var result = await UnityMcpBridge.RunOnMainThreadAsync(() =>
            {
                GameObject go = null;
                if (!string.IsNullOrEmpty(req.path))
                {
                    go = FindGameObjectByPath(req.path);
                    if (go == null) go = GameObject.Find(req.path);
                }
                else if (!string.IsNullOrEmpty(req.name))
                {
                    go = FindGameObjectByNameInActiveScene(req.name);
                }

                if (go == null) return new GameObjectResponse { found = false };
                return BuildGameObjectResponse(go, req.lightweight, req.componentFilter);
            });

            await UnityMcpBridge.WriteJsonAsync(response, 200, result);
        }

        public static async Task HandleGameObjectCreateAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
            var req = JsonUtility.FromJson<CreateRequest>(body);

            var result = await UnityMcpBridge.RunOnMainThreadAsync(() =>
            {
                Transform parent = null;
                if (!string.IsNullOrEmpty(req.parent))
                {
                    var parentGo = FindGameObjectByPath(req.parent);
                    if (parentGo == null) parentGo = GameObject.Find(req.parent);
                    if (parentGo == null) throw new Exception("Parent not found: " + req.parent);
                    parent = parentGo.transform;
                }

                var go = new GameObject(string.IsNullOrEmpty(req.name) ? "New GameObject" : req.name);
                if (parent != null) go.transform.SetParent(parent, false);
                Undo.RegisterCreatedObjectUndo(go, "MCP Create " + go.name);

                if (req.components != null)
                {
                    foreach (var compName in req.components) AddComponentByName(go, compName);
                }

                if (!string.IsNullOrEmpty(req.uiPreset)) ApplyUiPreset(go, req.uiPreset, req.width, req.height);
                EditorSceneManager.MarkSceneDirty(go.scene);

                return new CreateResponse { name = go.name, path = GetTransformPath(go.transform), instanceId = go.GetInstanceID() };
            });

            await UnityMcpBridge.WriteJsonAsync(response, 200, result);
        }

        public static async Task HandleGameObjectCreatePrimitiveAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
            var req = JsonUtility.FromJson<CreatePrimitiveRequest>(body);

            var result = await UnityMcpBridge.RunOnMainThreadAsync(() =>
            {
                PrimitiveType primitiveType;
                switch ((req.primitiveType ?? "").ToLowerInvariant())
                {
                    case "sphere": primitiveType = PrimitiveType.Sphere; break;
                    case "capsule": primitiveType = PrimitiveType.Capsule; break;
                    case "cylinder": primitiveType = PrimitiveType.Cylinder; break;
                    case "cube": primitiveType = PrimitiveType.Cube; break;
                    case "plane": primitiveType = PrimitiveType.Plane; break;
                    case "quad": primitiveType = PrimitiveType.Quad; break;
                    default: throw new Exception("Unknown primitive type: " + req.primitiveType);
                }

                var go = GameObject.CreatePrimitive(primitiveType);
                if (!string.IsNullOrEmpty(req.name)) go.name = req.name;
                Undo.RegisterCreatedObjectUndo(go, "MCP CreatePrimitive " + go.name);

                if (req.components != null)
                {
                    foreach (var compName in req.components) AddComponentByName(go, compName);
                }

                EditorSceneManager.MarkSceneDirty(go.scene);
                return new CreateResponse { name = go.name, path = GetTransformPath(go.transform), instanceId = go.GetInstanceID() };
            });

            await UnityMcpBridge.WriteJsonAsync(response, 200, result);
        }

        public static async Task HandleGameObjectDestroyAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
            var req = JsonUtility.FromJson<FindRequest>(body);

            var result = await UnityMcpBridge.RunOnMainThreadAsync(() =>
            {
                var path = !string.IsNullOrEmpty(req.path) ? req.path : req.name;
                var go = GameObject.Find(path);
                if (go == null) throw new Exception("GameObject not found: " + path);
                Undo.DestroyObjectImmediate(go);
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                return new GenericResponse { success = true, message = "Destroyed: " + path };
            });

            await UnityMcpBridge.WriteJsonAsync(response, 200, result);
        }

        public static async Task HandleGameObjectSetActiveAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
            var req = JsonUtility.FromJson<GameObjectSetActiveRequest>(body);

            var result = await UnityMcpBridge.RunOnMainThreadAsync(() =>
            {
                var go = GameObject.Find(req.path);
                if (go == null) throw new Exception("GameObject not found: " + req.path);
                Undo.RecordObject(go, "MCP SetActive " + req.path);
                go.SetActive(req.active);
                EditorSceneManager.MarkSceneDirty(go.scene);
                return new GenericResponse { success = true, message = "SetActive(" + req.active + "): " + req.path };
            });

            await UnityMcpBridge.WriteJsonAsync(response, 200, result);
        }

        public static async Task HandleGameObjectSetSiblingAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
            var req = JsonUtility.FromJson<GameObjectSetSiblingRequest>(body);

            var result = await UnityMcpBridge.RunOnMainThreadAsync(() =>
            {
                var go = GameObject.Find(req.path);
                if (go == null) throw new Exception("GameObject not found: " + req.path);

                if (!string.IsNullOrEmpty(req.parentPath))
                {
                    var parentGo = FindGameObjectByPath(req.parentPath);
                    if (parentGo == null) parentGo = GameObject.Find(req.parentPath);
                    if (parentGo == null) throw new Exception("Parent not found: " + req.parentPath);
                    go.transform.SetParent(parentGo.transform, req.setWorldPositionStays);
                }

                if (req.siblingIndex >= 0) go.transform.SetSiblingIndex(req.siblingIndex);
                EditorSceneManager.MarkSceneDirty(go.scene);

                return new GameObjectSetSiblingResponse
                {
                    success = true,
                    message = "Set sibling: " + req.path,
                    path = GetTransformPath(go.transform),
                    siblingIndex = go.transform.GetSiblingIndex()
                };
            });

            await UnityMcpBridge.WriteJsonAsync(response, 200, result);
        }

        // =====================================================================
        // Helper Methods
        // =====================================================================

        internal static string GetTransformPath(Transform t)
        {
            var parts = new List<string>();
            var current = t;
            while (current != null) { parts.Insert(0, current.name); current = current.parent; }
            return "/" + string.Join("/", parts);
        }

        private static GameObject FindGameObjectByPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            var parts = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return null;
            var scene = SceneManager.GetActiveScene();
            var rootObjects = scene.GetRootGameObjects();

            foreach (var root in rootObjects)
            {
                if (root.name == parts[0])
                {
                    var current = root.transform;
                    bool found = true;
                    for (int i = 1; i < parts.Length; i++)
                    {
                        var child = current.Find(parts[i]);
                        if (child == null) { found = false; break; }
                        current = child;
                    }
                    if (found) return current.gameObject;
                }
            }
            return null;
        }

        private static GameObject FindGameObjectByNameInActiveScene(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            var scene = SceneManager.GetActiveScene();
            if (!scene.isLoaded) return null;
            var roots = scene.GetRootGameObjects();
            var queue = new Queue<Transform>();
            for (var i = 0; i < roots.Length; i++) queue.Enqueue(roots[i].transform);
            while (queue.Count > 0)
            {
                var t = queue.Dequeue();
                if (t.name == name)
                {
                    var candidate = t.gameObject;
                    if (!EditorUtility.IsPersistent(candidate)) return candidate;
                }
                for (var c = 0; c < t.childCount; c++) queue.Enqueue(t.GetChild(c));
            }
            return null;
        }

        private static GameObjectResponse BuildGameObjectResponse(GameObject go, bool lightweight, string[] componentFilter)
        {
            var filterActive = componentFilter != null && componentFilter.Length > 0;
            var components = go.GetComponents<Component>();
            var compInfos = new List<ComponentInfo>();
            foreach (var comp in components)
            {
                if (comp == null) continue;
                var type = comp.GetType();
                var info = new ComponentInfo { typeName = type.Name, fullTypeName = type.FullName };
                if (!lightweight && !filterActive) info.properties = CollectSerializedPropertiesForComponent(comp);
                else info.properties = EmptyPropertyList();
                if (filterActive && !MatchesComponentTypeFilter(type, componentFilter)) continue;
                compInfos.Add(info);
            }

            return new GameObjectResponse
            {
                found = true, name = go.name, path = GetTransformPath(go.transform),
                activeSelf = go.activeSelf, layer = LayerMask.LayerToName(go.layer),
                tag = go.tag, components = compInfos.ToArray()
            };
        }

        private static bool MatchesComponentTypeFilter(Type type, string[] filters)
        {
            if (filters == null || filters.Length == 0) return false;
            foreach (var f in filters)
            {
                if (string.IsNullOrEmpty(f)) continue;
                if (type.Name.Equals(f, StringComparison.OrdinalIgnoreCase)) return true;
                if (!string.IsNullOrEmpty(type.FullName) && type.FullName.Equals(f, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        private static PropertyInfo[] EmptyPropertyList() => new PropertyInfo[0];

        private static PropertyInfo[] CollectSerializedPropertiesForComponent(Component comp)
        {
            var so = new SerializedObject(comp);
            var props = new List<PropertyInfo>();
            var sp = so.GetIterator();
            if (sp.NextVisible(true))
            {
                do
                {
                    if (sp.name == "m_Script") continue;
                    props.Add(new PropertyInfo { name = sp.name, type = sp.propertyType.ToString(), value = GetSerializedPropertyValue(sp) });
                } while (sp.NextVisible(false));
            }
            return props.ToArray();
        }

        private static string GetSerializedPropertyValue(SerializedProperty sp)
        {
            switch (sp.propertyType)
            {
                case SerializedPropertyType.Integer: return sp.intValue.ToString();
                case SerializedPropertyType.Boolean: return sp.boolValue.ToString();
                case SerializedPropertyType.Float: return sp.floatValue.ToString("F4");
                case SerializedPropertyType.String: return sp.stringValue ?? "";
                case SerializedPropertyType.Enum:
                    return sp.enumDisplayNames != null && sp.enumValueIndex >= 0 && sp.enumValueIndex < sp.enumDisplayNames.Length
                        ? sp.enumDisplayNames[sp.enumValueIndex] : sp.enumValueIndex.ToString();
                case SerializedPropertyType.ObjectReference: return FormatObjectReferenceValue(sp.objectReferenceValue);
                case SerializedPropertyType.Color: var c = sp.colorValue; return string.Format("({0:F2},{1:F2},{2:F2},{3:F2})", c.r, c.g, c.b, c.a);
                case SerializedPropertyType.Vector2: var v2 = sp.vector2Value; return string.Format("({0:F2},{1:F2})", v2.x, v2.y);
                case SerializedPropertyType.Vector3: var v3 = sp.vector3Value; return string.Format("({0:F2},{1:F2},{2:F2})", v3.x, v3.y, v3.z);
                case SerializedPropertyType.Vector4: var v4 = sp.vector4Value; return string.Format("({0:F2},{1:F2},{2:F2},{3:F2})", v4.x, v4.y, v4.z, v4.w);
                case SerializedPropertyType.Rect: var r = sp.rectValue; return string.Format("(x:{0:F1},y:{1:F1},w:{2:F1},h:{3:F1})", r.x, r.y, r.width, r.height);
                default: return "(" + sp.propertyType + ")";
            }
        }

        private static string FormatObjectReferenceValue(UnityEngine.Object reference)
        {
            if (reference == null) return "(null)";
            if (reference is GameObject go) return GetTransformPath(go.transform);
            if (reference is Component component) return GetTransformPath(component.transform) + "::" + component.GetType().Name;
            var assetPath = AssetDatabase.GetAssetPath(reference);
            if (!string.IsNullOrEmpty(assetPath)) return assetPath;
            return reference.name;
        }

        private static void ApplyUiPreset(GameObject go, string preset, float? width, float? height)
        {
            var rect = go.GetComponent<RectTransform>();
            if (rect == null) return;
            float w = width ?? 160f;
            float h = height ?? 30f;
            switch ((preset ?? "").ToLowerInvariant())
            {
                case "button-bottom-center": rect.anchorMin = new Vector2(0.5f, 0f); rect.anchorMax = new Vector2(0.5f, 0f); rect.pivot = new Vector2(0.5f, 0f); rect.anchoredPosition = new Vector2(0f, 30f); rect.sizeDelta = new Vector2(w, h); break;
                case "button-top-center": rect.anchorMin = new Vector2(0.5f, 1f); rect.anchorMax = new Vector2(0.5f, 1f); rect.pivot = new Vector2(0.5f, 1f); rect.anchoredPosition = new Vector2(0f, -30f); rect.sizeDelta = new Vector2(w, h); break;
                case "panel-top-center": rect.anchorMin = new Vector2(0.5f, 1f); rect.anchorMax = new Vector2(0.5f, 1f); rect.pivot = new Vector2(0.5f, 1f); rect.anchoredPosition = new Vector2(0f, -120f); rect.sizeDelta = new Vector2(w, h); break;
                case "panel-bottom-center": rect.anchorMin = new Vector2(0.5f, 0f); rect.anchorMax = new Vector2(0.5f, 0f); rect.pivot = new Vector2(0.5f, 0f); rect.anchoredPosition = new Vector2(0f, 80f); rect.sizeDelta = new Vector2(w, h); break;
                case "stretch-parent": rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.pivot = new Vector2(0.5f, 0.5f); rect.anchoredPosition = Vector2.zero; rect.sizeDelta = Vector2.zero; break;
                case "center-fixed": rect.anchorMin = new Vector2(0.5f, 0.5f); rect.anchorMax = new Vector2(0.5f, 0.5f); rect.pivot = new Vector2(0.5f, 0.5f); rect.anchoredPosition = Vector2.zero; rect.sizeDelta = new Vector2(w, h); break;
                case "raw-image-top": rect.anchorMin = new Vector2(0.5f, 1f); rect.anchorMax = new Vector2(0.5f, 1f); rect.pivot = new Vector2(0.5f, 1f); rect.anchoredPosition = new Vector2(0f, -200f); rect.sizeDelta = new Vector2(w ?? 256f, h ?? 256f); break;
                default: rect.anchorMin = new Vector2(0.5f, 0.5f); rect.anchorMax = new Vector2(0.5f, 0.5f); rect.pivot = new Vector2(0.5f, 0.5f); rect.anchoredPosition = Vector2.zero; rect.sizeDelta = new Vector2(w, h); break;
            }
        }

        private static Component AddComponentByName(GameObject go, string typeName)
        {
            var knownTypes = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
            {
                { "RectTransform", typeof(RectTransform) }, { "Canvas", typeof(Canvas) },
                { "CanvasScaler", typeof(UnityEngine.UI.CanvasScaler) }, { "GraphicRaycaster", typeof(UnityEngine.UI.GraphicRaycaster) },
                { "Image", typeof(UnityEngine.UI.Image) }, { "RawImage", typeof(UnityEngine.UI.RawImage) },
                { "Button", typeof(UnityEngine.UI.Button) }, { "Toggle", typeof(UnityEngine.UI.Toggle) },
                { "Slider", typeof(UnityEngine.UI.Slider) }, { "Scrollbar", typeof(UnityEngine.UI.Scrollbar) },
                { "ScrollRect", typeof(UnityEngine.UI.ScrollRect) }, { "InputField", typeof(UnityEngine.UI.InputField) },
                { "Text", typeof(UnityEngine.UI.Text) }, { "Dropdown", typeof(UnityEngine.UI.Dropdown) },
                { "Mask", typeof(UnityEngine.UI.Mask) }, { "RectMask2D", typeof(UnityEngine.UI.RectMask2D) },
                { "LayoutElement", typeof(UnityEngine.UI.LayoutElement) }, { "ContentSizeFitter", typeof(UnityEngine.UI.ContentSizeFitter) },
                { "AspectRatioFitter", typeof(UnityEngine.UI.AspectRatioFitter) },
                { "HorizontalLayoutGroup", typeof(UnityEngine.UI.HorizontalLayoutGroup) },
                { "VerticalLayoutGroup", typeof(UnityEngine.UI.VerticalLayoutGroup) },
                { "GridLayoutGroup", typeof(UnityEngine.UI.GridLayoutGroup) }, { "CanvasGroup", typeof(CanvasGroup) },
            };

            if (knownTypes.TryGetValue(typeName, out var knownType)) return Undo.AddComponent(go, knownType);

            // TMP types
            if (typeName.Equals("TextMeshProUGUI", StringComparison.OrdinalIgnoreCase) || typeName.Equals("TMP_Text", StringComparison.OrdinalIgnoreCase))
            {
                var tmpType = UnityMcpBridge.ResolveTypeByFullName("TMPro.TextMeshProUGUI");
                if (tmpType != null) return Undo.AddComponent(go, tmpType);
                throw new Exception("TextMeshPro not found.");
            }

            var foundType = UnityMcpBridge.ResolveTypeByFullName(typeName);
            if (foundType != null && typeof(Component).IsAssignableFrom(foundType)) return Undo.AddComponent(go, foundType);
            throw new Exception("Component type not found: " + typeName);
        }
    }
}
#endif
