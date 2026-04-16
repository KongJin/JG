#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectSD.EditorTools.UnityMcp
{
    /// <summary>
    /// 개선된 GameObject 핸들러 - 컴포넌트 필드 값, 메서드 정보 포함
    /// </summary>
    internal static class ImprovedGameObjectHandlers
    {
        static ImprovedGameObjectHandlers()
        {
            // 개선된 GameObject 엔드포인트
            "POST".Register("/gameobject/find-with-fields", "Find GameObject with component field values", async (req, res) => await HandleGameObjectFindWithFieldsAsync(req, res));

            // 기존 엔드포인트 호환성 유지
            "POST".Register("/gameobject/find", "Find GameObject by path or name (legacy)", async (req, res) => await HandleGameObjectFindAsync(req, res));
            "POST".Register("/gameobject/create", "Create new GameObject", async (req, res) => await HandleGameObjectCreateAsync(req, res));
            "POST".Register("/gameobject/create-primitive", "Create primitive (cube/sphere/etc)", async (req, res) => await HandleGameObjectCreatePrimitiveAsync(req, res));
            "POST".Register("/gameobject/destroy", "Destroy GameObject", async (req, res) => await HandleGameObjectDestroyAsync(req, res));
            "POST".Register("/gameobject/set-active", "Set GameObject active state", async (req, res) => await HandleGameObjectSetActiveAsync(req, res));
            "POST".Register("/gameobject/set-sibling", "Set sibling order", async (req, res) => await HandleGameObjectSetSiblingAsync(req, res));
            "POST".Register("/gameobject/set-parent", "Change parent of a GameObject", async (req, res) => await HandleGameObjectSetParentAsync(req, res));
        }

        // =====================================================================
        // GameObject Find with Fields - 개선된 버전
        // =====================================================================

        public static async Task HandleGameObjectFindWithFieldsAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
            var req = JsonUtility.FromJson<FindRequest>(body);

            var result = await UnityMcpBridge.RunOnMainThreadAsync(() =>
            {
                GameObject go = null;
                if (!string.IsNullOrEmpty(req.path))
                {
                    go = McpSharedHelpers.FindGameObjectByPath(req.path);
                    if (go == null) go = GameObject.Find(req.path);
                }
                else if (!string.IsNullOrEmpty(req.name))
                {
                    go = McpSharedHelpers.FindGameObjectByNameInActiveScene(req.name);
                }

                if (go == null) return new GameObjectWithFieldsResponse { found = false };
                return BuildGameObjectWithFieldsResponse(go, req.componentFilter);
            });

            await UnityMcpBridge.WriteJsonAsync(response, 200, result);
        }

        private static GameObjectWithFieldsResponse BuildGameObjectWithFieldsResponse(GameObject go, string[] componentFilter)
        {
            var filterActive = componentFilter != null && componentFilter.Length > 0;
            var components = go.GetComponents<Component>();
            var compInfos = new List<ComponentWithFields>();

            foreach (var comp in components)
            {
                if (comp == null) continue;
                var type = comp.GetType();

                // 필터링
                if (filterActive && !McpSharedHelpers.MatchesComponentTypeFilter(type, componentFilter))
                    continue;

                var compInfo = new ComponentWithFields
                {
                    typeName = type.Name,
                    fullTypeName = type.FullName
                };

                // SerializedField 정보 수집
                compInfo.fields = CollectSerializedFields(comp).ToArray();

                // Public 메서드 정보 수집
                compInfo.methods = CollectPublicMethods(type).ToArray();

                compInfos.Add(compInfo);
            }

            return new GameObjectWithFieldsResponse
            {
                found = true,
                name = go.name,
                path = McpSharedHelpers.GetTransformPath(go.transform),
                activeSelf = go.activeSelf,
                layer = LayerMask.LayerToName(go.layer),
                tag = go.tag,
                components = compInfos.ToArray()
            };
        }

        private static List<SerializedFieldInfo> CollectSerializedFields(Component comp)
        {
            var fields = new List<SerializedFieldInfo>();
            var type = comp.GetType();
            var so = new SerializedObject(comp);

            // SerializedProperty를 통해 모든 속성 수집
            var prop = so.GetIterator();
            var children = new List<SerializedProperty>();

            while (prop.Next(true))
            {
                if (prop.propertyType == SerializedPropertyType.Generic || prop.propertyType == SerializedPropertyType.ObjectReference)
                {
                    children.Add(prop.Copy());
                }
            }

            // GetIterator가 너무 많은 내부 속성을 포함하므로, 리플렉션으로 필터링
            var serializedFields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(f => f.IsPublic || f.GetCustomAttribute<SerializeField>() != null)
                .ToList();

            foreach (var field in serializedFields)
            {
                var value = field.GetValue(comp);
                var fieldValue = value?.ToString() ?? "null";
                var isNull = value == null;
                var isObjectRef = value is UnityEngine.Object;

                string objectPath = null;
                if (isObjectRef && !isNull)
                {
                    var objRef = value as UnityEngine.Object;
                    if (objRef is GameObject goRef)
                    {
                        objectPath = McpSharedHelpers.GetTransformPath(goRef.transform);
                    }
                    else if (objRef is Component compRef)
                    {
                        objectPath = McpSharedHelpers.GetTransformPath(compRef.transform);
                    }
                }

                // 값이 너무 길면 잘라내기
                if (fieldValue.Length > 200) fieldValue = fieldValue.Substring(0, 200) + "...";

                fields.Add(new SerializedFieldInfo
                {
                    name = field.Name,
                    type = GetFieldTypeString(field.FieldType),
                    value = fieldValue,
                    isNull = isNull,
                    isObjectReference = isObjectRef,
                    objectPath = objectPath
                });
            }

            return fields;
        }

        private static List<MethodInfo> CollectPublicMethods(Type type)
        {
            var methods = new List<MethodInfo>();

            var allMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            foreach (var method in allMethods)
            {
                // 특수 메서드 제외
                if (method.IsSpecialName) continue;
                // Unity 이벤트 메서드로 보이는 것만 포함
                if (!method.Name.StartsWith("On") && !method.Name.Contains("Click") && !method.Name.Contains("Submit") && !method.Name.Contains("Handle"))
                    continue;

                var paramTypes = method.GetParameters()
                    .Select(p => GetParameterTypeString(p.ParameterType))
                    .ToArray();

                methods.Add(new MethodInfo
                {
                    name = method.Name,
                    returnType = GetReturnTypeString(method.ReturnType),
                    parameterTypes = paramTypes,
                    isPublic = method.IsPublic
                });
            }

            return methods;
        }

        private static string GetFieldTypeString(Type type)
        {
            if (type == null) return "null";
            if (type == typeof(string)) return "string";
            if (type == typeof(int)) return "int";
            if (type == typeof(float)) return "float";
            if (type == typeof(bool)) return "bool";
            if (type == typeof(Vector2)) return "Vector2";
            if (type == typeof(Vector3)) return "Vector3";
            if (type == typeof(Color)) return "Color";
            if (typeof(UnityEngine.Object).IsAssignableFrom(type)) return type.Name + " (Object)";
            if (type.IsEnum) return type.Name + " (enum)";
            return type.Name;
        }

        private static string GetParameterTypeString(Type type)
        {
            if (type == null) return "null";
            if (type == typeof(string)) return "string";
            if (type == typeof(int)) return "int";
            if (type == typeof(float)) return "float";
            if (type == typeof(bool)) return "bool";
            if (type == typeof(Vector2)) return "Vector2";
            if (type == typeof(Vector3)) return "Vector3";
            return type.Name;
        }

        private static string GetReturnTypeString(Type type)
        {
            if (type == null) return "null";
            if (type == typeof(void)) return "void";
            if (type == typeof(System.Collections.IEnumerator)) return "IEnumerator";
            if (type == typeof(string)) return "string";
            if (type == typeof(int)) return "int";
            if (type == typeof(float)) return "float";
            if (type == typeof(bool)) return "bool";
            return type.Name;
        }

        // =====================================================================
        // Legacy Handlers (호환성 유지)
        // =====================================================================

        public static async Task HandleGameObjectFindAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
            var req = JsonUtility.FromJson<FindRequest>(body);

            var result = await UnityMcpBridge.RunOnMainThreadAsync(() =>
            {
                GameObject go = null;
                if (!string.IsNullOrEmpty(req.path))
                {
                    go = McpSharedHelpers.FindGameObjectByPath(req.path);
                    if (go == null) go = GameObject.Find(req.path);
                }
                else if (!string.IsNullOrEmpty(req.name))
                {
                    go = McpSharedHelpers.FindGameObjectByNameInActiveScene(req.name);
                }

                if (go == null) return new GameObjectResponse { found = false };
                return BuildGameObjectResponse(go, req.lightweight, req.componentFilter);
            });

            await UnityMcpBridge.WriteJsonAsync(response, 200, result);
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
                if (!lightweight && !filterActive) info.properties = McpSharedHelpers.CollectSerializedPropertiesForComponent(comp);
                else info.properties = new PropertyInfo[0];
                if (filterActive && !McpSharedHelpers.MatchesComponentTypeFilter(type, componentFilter)) continue;
                compInfos.Add(info);
            }

            return new GameObjectResponse
            {
                found = true, name = go.name, path = McpSharedHelpers.GetTransformPath(go.transform),
                activeSelf = go.activeSelf, layer = LayerMask.LayerToName(go.layer),
                tag = go.tag, components = compInfos.ToArray()
            };
        }

        public static async Task HandleGameObjectCreateAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
            var req = JsonUtility.FromJson<CreateRequest>(body);

            string createdName = null;
            string createdPath = null;
            int createdInstanceId = 0;
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

                    var go = new GameObject(string.IsNullOrEmpty(req.name) ? "New GameObject" : req.name);
                    if (parent != null) go.transform.SetParent(parent, false);
                    Undo.RegisterCreatedObjectUndo(go, "MCP Create " + go.name);

                    if (req.components != null)
                        foreach (var compName in req.components) McpSharedHelpers.AddComponentByName(go, compName);

                    if (!string.IsNullOrEmpty(req.uiPreset)) ApplyUiPreset(go, req.uiPreset, req.width, req.height);
                    EditorSceneManager.MarkSceneDirty(go.scene);

                    createdName = go.name;
                    createdPath = McpSharedHelpers.GetTransformPath(go.transform);
                    createdInstanceId = go.GetInstanceID();
                    return true;
                }, "Create GameObject: " + (string.IsNullOrEmpty(req.name) ? "New GameObject" : req.name), out enqueueError);
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
            var result = new CreateResponse
            {
                name = createdName,
                path = createdPath,
                instanceId = createdInstanceId,
                queued = wasQueued,
                pendingCount = wasQueued ? SceneChangeQueue.PendingCount : 0,
                autoSaved = !wasQueued && req.autoSave && McpSharedHelpers.TryAutoSave()
            };

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
                    foreach (var compName in req.components) McpSharedHelpers.AddComponentByName(go, compName);

                EditorSceneManager.MarkSceneDirty(go.scene);
                return new CreateResponse { name = go.name, path = McpSharedHelpers.GetTransformPath(go.transform), instanceId = go.GetInstanceID() };
            });

            await UnityMcpBridge.WriteJsonAsync(response, 200, result);
        }

        public static async Task HandleGameObjectDestroyAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
            var req = JsonUtility.FromJson<FindRequest>(body);

            string destroyMessage = null;
            int enqueueResult = 0;
            string enqueueError = null;

            await UnityMcpBridge.RunOnMainThreadAsync(() =>
            {
                enqueueResult = SceneChangeQueue.EnqueueOrExecute(() =>
                {
                    var go = !string.IsNullOrEmpty(req.path)
                        ? McpSharedHelpers.FindGameObjectByPath(req.path)
                        : null;
                    if (go == null && !string.IsNullOrEmpty(req.name))
                        go = McpSharedHelpers.FindGameObjectByNameInActiveScene(req.name);
                    if (go == null) throw new Exception("GameObject not found: " + (req.path ?? req.name));
                    Undo.DestroyObjectImmediate(go);
                    EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                    destroyMessage = "Destroyed: " + (req.path ?? req.name);
                    return true;
                }, "Destroy GameObject: " + (req.path ?? req.name), out enqueueError);
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
            var result = new GenericResponse
            {
                success = true,
                message = destroyMessage,
                queued = wasQueued,
                pendingCount = wasQueued ? SceneChangeQueue.PendingCount : 0,
                autoSaved = !wasQueued && McpSharedHelpers.TryAutoSave()
            };

            await UnityMcpBridge.WriteJsonAsync(response, 200, result);
        }

        public static async Task HandleGameObjectSetActiveAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
            var req = JsonUtility.FromJson<GameObjectSetActiveRequest>(body);

            GenericResponse result = null;
            int enqueueResult = 0;
            string enqueueError = null;

            await UnityMcpBridge.RunOnMainThreadAsync(() =>
            {
                enqueueResult = SceneChangeQueue.EnqueueOrExecute(() =>
                {
                    var go = McpSharedHelpers.FindGameObjectByPath(req.path);
                    if (go == null) go = GameObject.Find(req.path);
                    if (go == null) throw new Exception("GameObject not found: " + req.path);
                    Undo.RecordObject(go, "MCP SetActive " + req.path);
                    go.SetActive(req.active);
                    EditorSceneManager.MarkSceneDirty(go.scene);
                    result = new GenericResponse { success = true, message = "SetActive(" + req.active + "): " + req.path };
                    return true;
                }, "SetActive(" + req.active + "): " + req.path, out enqueueError);
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
            result.queued = wasQueued;
            result.pendingCount = wasQueued ? SceneChangeQueue.PendingCount : 0;
            result.autoSaved = !wasQueued && req.autoSave && McpSharedHelpers.TryAutoSave();

            await UnityMcpBridge.WriteJsonAsync(response, 200, result);
        }

        public static async Task HandleGameObjectSetSiblingAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
            var req = JsonUtility.FromJson<GameObjectSetSiblingRequest>(body);

            GameObjectSetSiblingResponse result = null;
            int enqueueResult = 0;
            string enqueueError = null;

            await UnityMcpBridge.RunOnMainThreadAsync(() =>
            {
                enqueueResult = SceneChangeQueue.EnqueueOrExecute(() =>
                {
                    var go = McpSharedHelpers.FindGameObjectByPath(req.path);
                    if (go == null) go = GameObject.Find(req.path);
                    if (go == null) throw new Exception("GameObject not found: " + req.path);

                    if (req.siblingIndex >= 0) go.transform.SetSiblingIndex(req.siblingIndex);
                    EditorSceneManager.MarkSceneDirty(go.scene);

                    result = new GameObjectSetSiblingResponse
                    {
                        success = true,
                        message = "Set sibling: " + req.path,
                        path = McpSharedHelpers.GetTransformPath(go.transform),
                        siblingIndex = go.transform.GetSiblingIndex()
                    };
                    return true;
                }, "Set sibling: " + req.path, out enqueueError);
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
            result.queued = wasQueued;
            result.pendingCount = wasQueued ? SceneChangeQueue.PendingCount : 0;
            result.autoSaved = !wasQueued && req.autoSave && McpSharedHelpers.TryAutoSave();

            await UnityMcpBridge.WriteJsonAsync(response, 200, result);
        }

        public static async Task HandleGameObjectSetParentAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
            var req = JsonUtility.FromJson<GameObjectSetParentRequest>(body);

            GameObjectSetParentResponse result = null;
            int enqueueResult = 0;
            string enqueueError = null;

            await UnityMcpBridge.RunOnMainThreadAsync(() =>
            {
                enqueueResult = SceneChangeQueue.EnqueueOrExecute(() =>
                {
                    var go = McpSharedHelpers.FindGameObjectByPath(req.path);
                    if (go == null) go = GameObject.Find(req.path);
                    if (go == null) throw new Exception("GameObject not found: " + req.path);

                    Transform newParent = null;
                    if (!string.IsNullOrEmpty(req.parentPath))
                    {
                        var parentGo = McpSharedHelpers.FindGameObjectByPath(req.parentPath);
                        if (parentGo == null) parentGo = GameObject.Find(req.parentPath);
                        if (parentGo == null) throw new Exception("Parent not found: " + req.parentPath);
                        newParent = parentGo.transform;
                    }

                    Undo.SetTransformParent(go.transform, newParent, "MCP Set Parent: " + req.path);
                    EditorSceneManager.MarkSceneDirty(go.scene);

                    result = new GameObjectSetParentResponse
                    {
                        success = true,
                        message = "Set parent: " + req.path + " → " + (req.parentPath ?? "(root)"),
                        path = McpSharedHelpers.GetTransformPath(go.transform),
                        parentPath = req.parentPath ?? ""
                    };
                    return true;
                }, "Set parent: " + req.path + " → " + (req.parentPath ?? "(root)"), out enqueueError);
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
            result.queued = wasQueued;
            result.pendingCount = wasQueued ? SceneChangeQueue.PendingCount : 0;
            result.autoSaved = !wasQueued && req.autoSave && McpSharedHelpers.TryAutoSave();

            await UnityMcpBridge.WriteJsonAsync(response, 200, result);
        }

        // =====================================================================
        // Helper Methods
        // =====================================================================

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
                case "raw-image-top": rect.anchorMin = new Vector2(0.5f, 1f); rect.anchorMax = new Vector2(0.5f, 1f); rect.pivot = new Vector2(0.5f, 1f); rect.anchoredPosition = new Vector2(0f, -200f); rect.sizeDelta = new Vector2(w, h); break;
                default: rect.anchorMin = new Vector2(0.5f, 0.5f); rect.anchorMax = new Vector2(0.5f, 0.5f); rect.pivot = new Vector2(0.5f, 0.5f); rect.anchoredPosition = Vector2.zero; rect.sizeDelta = new Vector2(w, h); break;
            }
        }
    }
}
#endif
