#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectSD.EditorTools.UnityMcp
{
    /// <summary>
    /// 여러 MCP 핸들러에서 공통으로 사용하는 헬퍼 메서드 모음.
    /// 중복 코드 제거 및 단일 진실 공급원(SSOT) 유지 목적.
    /// </summary>
    internal static class McpSharedHelpers
    {
        // =====================================================================
        // Transform / Path Helpers
        // =====================================================================

        public static string GetTransformPath(Transform t)
        {
            var parts = new List<string>();
            var current = t;
            while (current != null)
            {
                parts.Insert(0, current.name);
                current = current.parent;
            }
            return "/" + string.Join("/", parts);
        }

        // =====================================================================
        // GameObject Finding
        // =====================================================================

        public static GameObject FindGameObjectByPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            var parts = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return null;
            var scene = SceneManager.GetActiveScene();
            foreach (var root in scene.GetRootGameObjects())
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

        public static GameObject FindGameObjectByNameInActiveScene(string name)
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

        public static GameObject FindInChildren(GameObject parent, string name)
        {
            var transforms = parent.GetComponentsInChildren<Transform>(true);
            foreach (var t in transforms)
            {
                if (t.gameObject.name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return t.gameObject;
            }
            return null;
        }

        public static GameObject FindInScene(UnityEngine.SceneManagement.Scene scene, string name)
        {
            var rootObjects = scene.GetRootGameObjects();
            foreach (var root in rootObjects)
            {
                var found = FindInChildren(root, name);
                if (found != null) return found;
            }
            return null;
        }

        public static GameObject FindGameObjectByFieldName(GameObject contextGo, string fieldName, string searchScope)
        {
            var scene = SceneManager.GetActiveScene();
            switch ((searchScope ?? "children").ToLowerInvariant())
            {
                case "children": return FindInChildren(contextGo, fieldName);
                case "scene": return FindInScene(scene, fieldName);
                default:
                    if (searchScope.StartsWith("path:"))
                        return FindGameObjectByPath(searchScope.Substring(5));
                    return FindInChildren(contextGo, fieldName);
            }
        }

        // =====================================================================
        // Component Management
        // =====================================================================

        public static Component AddComponentByName(GameObject go, string typeName)
        {
            var knownTypes = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
            {
                { "RectTransform", typeof(RectTransform) },
                { "Canvas", typeof(Canvas) },
                { "CanvasScaler", typeof(UnityEngine.UI.CanvasScaler) },
                { "GraphicRaycaster", typeof(UnityEngine.UI.GraphicRaycaster) },
                { "Image", typeof(UnityEngine.UI.Image) },
                { "RawImage", typeof(UnityEngine.UI.RawImage) },
                { "Button", typeof(UnityEngine.UI.Button) },
                { "Toggle", typeof(UnityEngine.UI.Toggle) },
                { "Slider", typeof(UnityEngine.UI.Slider) },
                { "Scrollbar", typeof(UnityEngine.UI.Scrollbar) },
                { "ScrollRect", typeof(UnityEngine.UI.ScrollRect) },
                { "InputField", typeof(UnityEngine.UI.InputField) },
                { "Text", typeof(UnityEngine.UI.Text) },
                { "Dropdown", typeof(UnityEngine.UI.Dropdown) },
                { "Mask", typeof(UnityEngine.UI.Mask) },
                { "RectMask2D", typeof(UnityEngine.UI.RectMask2D) },
                { "LayoutElement", typeof(UnityEngine.UI.LayoutElement) },
                { "ContentSizeFitter", typeof(UnityEngine.UI.ContentSizeFitter) },
                { "AspectRatioFitter", typeof(UnityEngine.UI.AspectRatioFitter) },
                { "HorizontalLayoutGroup", typeof(UnityEngine.UI.HorizontalLayoutGroup) },
                { "VerticalLayoutGroup", typeof(UnityEngine.UI.VerticalLayoutGroup) },
                { "GridLayoutGroup", typeof(UnityEngine.UI.GridLayoutGroup) },
                { "CanvasGroup", typeof(CanvasGroup) },
            };

            if (knownTypes.TryGetValue(typeName, out var knownType))
                return Undo.AddComponent(go, knownType);

            // TMP types
            if (typeName.Equals("TextMeshProUGUI", StringComparison.OrdinalIgnoreCase)
                || typeName.Equals("TMP_Text", StringComparison.OrdinalIgnoreCase))
            {
                var tmpType = UnityMcpBridge.ResolveTypeByFullName("TMPro.TextMeshProUGUI");
                if (tmpType != null) return Undo.AddComponent(go, tmpType);
                throw new Exception("TextMeshPro not found.");
            }
            if (typeName.Equals("TMP_InputField", StringComparison.OrdinalIgnoreCase))
            {
                var tmpType = UnityMcpBridge.ResolveTypeByFullName("TMPro.TMP_InputField");
                if (tmpType != null) return Undo.AddComponent(go, tmpType);
                throw new Exception("TextMeshPro not found.");
            }

            var foundType = UnityMcpBridge.ResolveTypeByFullName(typeName);
            if (foundType != null && typeof(Component).IsAssignableFrom(foundType))
                return Undo.AddComponent(go, foundType);
            throw new Exception("Component type not found: " + typeName);
        }

        public static bool MatchesComponentTypeFilter(Type type, string[] filters)
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

        public static bool MatchesTypeName(Type type, string typeName)
        {
            return type.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase)
                || (!string.IsNullOrEmpty(type.FullName) && type.FullName.Equals(typeName, StringComparison.OrdinalIgnoreCase));
        }

        // =====================================================================
        // Serialized Property Helpers
        // =====================================================================

        public static string GetSerializedPropertyValue(SerializedProperty sp)
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

        public static void SetSerializedPropertyValue(SerializedProperty sp, string value, string assetPath)
        {
            switch (sp.propertyType)
            {
                case SerializedPropertyType.Integer: sp.intValue = int.Parse(value); break;
                case SerializedPropertyType.Boolean: sp.boolValue = bool.Parse(value); break;
                case SerializedPropertyType.Float: sp.floatValue = float.Parse(value); break;
                case SerializedPropertyType.String: sp.stringValue = value; break;
                case SerializedPropertyType.Enum:
                    if (int.TryParse(value, out var enumIdx)) sp.enumValueIndex = enumIdx;
                    else
                    {
                        var idx = Array.IndexOf(sp.enumDisplayNames, value);
                        if (idx >= 0) sp.enumValueIndex = idx;
                        else throw new Exception("Invalid enum value: " + value);
                    }
                    break;
                case SerializedPropertyType.Color:
                    if (ColorUtility.TryParseHtmlString(value, out var color)) sp.colorValue = color;
                    else throw new Exception("Invalid color: " + value);
                    break;
                case SerializedPropertyType.Vector2: var v2p = ParseFloatArray(value); if (v2p.Length >= 2) sp.vector2Value = new Vector2(v2p[0], v2p[1]); break;
                case SerializedPropertyType.Vector3: var v3p = ParseFloatArray(value); if (v3p.Length >= 3) sp.vector3Value = new Vector3(v3p[0], v3p[1], v3p[2]); break;
                case SerializedPropertyType.Vector4: var v4p = ParseFloatArray(value); if (v4p.Length >= 4) sp.vector4Value = new Vector4(v4p[0], v4p[1], v4p[2], v4p[3]); break;
                case SerializedPropertyType.Rect: var rp = ParseFloatArray(value); if (rp.Length >= 4) sp.rectValue = new Rect(rp[0], rp[1], rp[2], rp[3]); break;
                case SerializedPropertyType.ObjectReference: sp.objectReferenceValue = ResolveObjectReference(sp, value, assetPath); break;
                default: throw new Exception("Unsupported property type: " + sp.propertyType);
            }
        }

        public static string FormatObjectReferenceValue(UnityEngine.Object reference)
        {
            if (reference == null) return "(null)";
            if (reference is GameObject go) return GetTransformPath(go.transform);
            if (reference is Component component) return GetTransformPath(component.transform) + "::" + component.GetType().Name;
            var assetPath = AssetDatabase.GetAssetPath(reference);
            if (!string.IsNullOrEmpty(assetPath)) return assetPath;
            return reference.name;
        }

        public static UnityEngine.Object ResolveObjectReference(SerializedProperty sp, string value, string assetPath)
        {
            var reference = !string.IsNullOrEmpty(assetPath) ? assetPath : value;
            if (string.IsNullOrEmpty(reference) || reference == "(null)") return null;
            if (reference.StartsWith("/", StringComparison.Ordinal)) return ResolveSceneObjectReference(sp, reference);
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(reference);
            if (asset == null) throw new Exception("Asset not found at path: " + reference);
            return asset;
        }

        public static UnityEngine.Object ResolveSceneObjectReference(SerializedProperty sp, string reference)
        {
            var separatorIndex = reference.IndexOf("::", StringComparison.Ordinal);
            var gameObjectPath = separatorIndex >= 0 ? reference.Substring(0, separatorIndex) : reference;
            var componentTypeName = separatorIndex >= 0 ? reference.Substring(separatorIndex + 2) : null;
            var targetGo = GameObject.Find(gameObjectPath);
            if (targetGo == null) throw new Exception("Scene object not found: " + gameObjectPath);
            if (!string.IsNullOrEmpty(componentTypeName))
            {
                var explicitComponent = targetGo.GetComponents<Component>().FirstOrDefault(c => c != null && MatchesTypeName(c.GetType(), componentTypeName));
                if (explicitComponent == null) throw new Exception("Component not found on scene object: " + reference);
                return explicitComponent;
            }
            var fieldType = resolveSerializedPropertyFieldType(sp);
            if (fieldType == null || fieldType == typeof(UnityEngine.Object)) return targetGo;
            if (typeof(GameObject).IsAssignableFrom(fieldType)) return targetGo;
            if (typeof(Transform).IsAssignableFrom(fieldType)) return targetGo.transform;
            if (typeof(Component).IsAssignableFrom(fieldType))
            {
                var component = targetGo.GetComponent(fieldType);
                if (component == null) throw new Exception("Component of type " + fieldType.Name + " not found on " + gameObjectPath);
                return component;
            }
            throw new Exception("Unsupported scene reference field type: " + fieldType.FullName);
        }

        public static Type ResolveSerializedPropertyFieldType(SerializedProperty sp)
        {
            return resolveSerializedPropertyFieldType(sp);
        }

        private static Type resolveSerializedPropertyFieldType(SerializedProperty sp)
        {
            var currentType = sp.serializedObject.targetObject.GetType();
            var path = sp.propertyPath.Replace(".Array.data[", "[");
            var segments = path.Split('.');
            foreach (var rawSegment in segments)
            {
                var fieldName = rawSegment;
                var indexStart = rawSegment.IndexOf('[', StringComparison.Ordinal);
                if (indexStart >= 0) fieldName = rawSegment.Substring(0, indexStart);
                var field = findFieldInTypeHierarchy(currentType, fieldName);
                if (field == null) return null;
                currentType = field.FieldType;
                if (indexStart >= 0 && currentType.IsArray) currentType = currentType.GetElementType();
            }
            return currentType;
        }

        private static FieldInfo findFieldInTypeHierarchy(Type type, string fieldName)
        {
            if (type == null || string.IsNullOrEmpty(fieldName)) return null;
            var cacheKey = type.AssemblyQualifiedName + "\0" + fieldName;
            if (UnityMcpBridge.McpFieldInfoCache.TryGetValue(cacheKey, out var cached)) return cached;
            var current = type;
            while (current != null)
            {
                var field = current.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null) { UnityMcpBridge.McpFieldInfoCache[cacheKey] = field; return field; }
                current = current.BaseType;
            }
            return null;
        }

        public static float[] ParseFloatArray(string value)
        {
            var cleaned = value.Trim('(', ')', ' ');
            var parts = cleaned.Split(',');
            var result = new float[parts.Length];
            for (int i = 0; i < parts.Length; i++) result[i] = float.Parse(parts[i].Trim());
            return result;
        }

        public static PropertyInfo[] CollectSerializedPropertiesForComponent(Component comp, HashSet<string> propertyNameFilter = null)
        {
            var so = new SerializedObject(comp);
            var props = new List<PropertyInfo>();
            var sp = so.GetIterator();
            if (sp.NextVisible(true))
            {
                do
                {
                    if (sp.name == "m_Script") continue;
                    if (propertyNameFilter != null && !propertyNameFilter.Contains(sp.name)) continue;
                    props.Add(new PropertyInfo { name = sp.name, type = sp.propertyType.ToString(), value = GetSerializedPropertyValue(sp) });
                } while (sp.NextVisible(false));
            }
            return props.ToArray();
        }
    }
}
#endif
