#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace ProjectSD.EditorTools.UnityMcp
{
    /// <summary>
    /// Phase 3: Evaluate — 런타임 public 필드 상태 조회.
    /// [Editor-only] [Play-mode-only] 엔드포인트.
    /// 메서드 호출은 부작용/직렬화 문제로 제외 (조회만).
    /// </summary>
    internal static class EvalHandlers
    {
        static EvalHandlers()
        {
            "POST".Register("/eval/find-component", "Find component on GameObject and read public fields", async (req, res) => await HandleEvalFindComponentAsync(req, res));
            "POST".Register("/eval/get-public-state", "Read public field values from a MonoBehaviour", async (req, res) => await HandleEvalGetPublicStateAsync(req, res));
        }

        // =====================================================================
        // POST /eval/find-component
        // =====================================================================

        public static async Task HandleEvalFindComponentAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            if (!EditorApplication.isPlaying)
            {
                await UnityMcpBridge.WriteJsonAsync(response, 409, new ErrorResponseEnvelope
                {
                    code = "PLAY_MODE_REQUIRED",
                    message = "This endpoint requires play mode",
                    detail = "Use POST /play/start to enter play mode first"
                });
                return;
            }

            EvalFindComponentRequest req = null;
            if (request.HasEntityBody)
            {
                var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
                if (!string.IsNullOrWhiteSpace(body))
                {
                    req = JsonUtility.FromJson<EvalFindComponentRequest>(body);
                }
            }

            if (req == null || string.IsNullOrWhiteSpace(req.path) || string.IsNullOrWhiteSpace(req.componentType))
            {
                await UnityMcpBridge.WriteJsonAsync(response, 400, new ErrorResponseEnvelope
                {
                    code = "NOT_FOUND",
                    message = "path and componentType are required",
                    detail = "Expected: { path: \"/GarageSetup\", componentType: \"GarageSetup\", fields: [\"_eventBus\"] }"
                });
                return;
            }

            var result = await UnityMcpBridge.RunOnMainThreadAsync(() =>
            {
                var go = McpSharedHelpers.FindGameObjectByPath(req.path);
                if (go == null)
                {
                    throw new Exception("GameObject not found: " + req.path);
                }

                var comps = go.GetComponents<Component>();
                Component target = null;
                foreach (var c in comps)
                {
                    if (c != null && c.GetType().Name.Equals(req.componentType, StringComparison.OrdinalIgnoreCase))
                    {
                        target = c;
                        break;
                    }
                }

                if (target == null)
                {
                    throw new Exception("Component " + req.componentType + " not found on " + req.path);
                }

                var fields = McpSharedHelpers.GetPublicFieldValues(target, req.fields);
                return new EvalFindComponentResponse
                {
                    success = true,
                    componentPath = req.path,
                    componentType = target.GetType().FullName,
                    fieldsJson = DictionaryToJson(fields)
                };
            });

            await UnityMcpBridge.WriteJsonAsync(response, 200, result);
        }

        // =====================================================================
        // POST /eval/get-public-state
        // =====================================================================

        public static async Task HandleEvalGetPublicStateAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            if (!EditorApplication.isPlaying)
            {
                await UnityMcpBridge.WriteJsonAsync(response, 409, new ErrorResponseEnvelope
                {
                    code = "PLAY_MODE_REQUIRED",
                    message = "This endpoint requires play mode",
                    detail = "Use POST /play/start to enter play mode first"
                });
                return;
            }

            EvalGetPublicStateRequest req = null;
            if (request.HasEntityBody)
            {
                var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
                if (!string.IsNullOrWhiteSpace(body))
                {
                    req = JsonUtility.FromJson<EvalGetPublicStateRequest>(body);
                }
            }

            if (req == null || string.IsNullOrWhiteSpace(req.path) || string.IsNullOrWhiteSpace(req.componentType))
            {
                await UnityMcpBridge.WriteJsonAsync(response, 400, new ErrorResponseEnvelope
                {
                    code = "NOT_FOUND",
                    message = "path and componentType are required",
                    detail = "Expected: { path: \"/GarageSetup\", componentType: \"GarageSetup\" }"
                });
                return;
            }

            var result = await UnityMcpBridge.RunOnMainThreadAsync(() =>
            {
                var go = McpSharedHelpers.FindGameObjectByPath(req.path);
                if (go == null)
                {
                    throw new Exception("GameObject not found: " + req.path);
                }

                var comps = go.GetComponents<Component>();
                Component target = null;
                foreach (var c in comps)
                {
                    if (c != null && c.GetType().Name.Equals(req.componentType, StringComparison.OrdinalIgnoreCase))
                    {
                        target = c;
                        break;
                    }
                }

                if (target == null)
                {
                    throw new Exception("Component " + req.componentType + " not found on " + req.path);
                }

                var fields = McpSharedHelpers.GetPublicFieldValues(target, req.fields);
                return new EvalGetPublicStateResponse
                {
                    success = true,
                    componentPath = req.path,
                    componentType = target.GetType().FullName,
                    fieldsJson = DictionaryToJson(fields)
                };
            });

            await UnityMcpBridge.WriteJsonAsync(response, 200, result);
        }

        // =====================================================================
        // Helpers
        // =====================================================================

        /// <summary>
        /// Dictionary를 간단한 JSON 문자열로 변환.
        /// JsonUtility는 Dictionary를 지원하지 않으므로 수동 직렬화.
        /// </summary>
        private static string DictionaryToJson(Dictionary<string, string> dict)
        {
            if (dict == null || dict.Count == 0) return "{}";
            var sb = new System.Text.StringBuilder();
            sb.Append("{");
            var first = true;
            foreach (var kv in dict)
            {
                if (!first) sb.Append(",");
                sb.Append("\"").Append(EscapeJson(kv.Key)).Append("\":\"").Append(EscapeJson(kv.Value)).Append("\"");
                first = false;
            }
            sb.Append("}");
            return sb.ToString();
        }

        private static string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\n").Replace("\t", "\\t");
        }
    }
}
#endif
