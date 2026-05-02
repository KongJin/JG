#if UNITY_EDITOR
#pragma warning disable CS0649 // Request DTO fields are populated by Unity JsonUtility.
using System;
using System.Collections;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace ProjectSD.EditorTools.UnityMcp
{
    internal static class GameObjectInvokeHandlers
    {
        static GameObjectInvokeHandlers()
        {
            "POST".Register("/gameobject/invoke", "Invoke a MonoBehaviour method on a GameObject by path.", async (req, res) => await HandleGameObjectInvokeAsync(req, res));
        }

        public static async Task HandleGameObjectInvokeAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var req = await ReadJsonRequestAsync<GameObjectInvokeRequest>(request);

            try
            {
                var result = await UnityMcpBridge.RunOnMainThreadAsync(() => InvokeGameObjectMethod(req));
                await UnityMcpBridge.WriteJsonAsync(response, 200, result);
            }
            catch (ArgumentException ex)
            {
                await UnityMcpBridge.WriteJsonAsync(response, 400, new ErrorResponse { error = "Invalid GameObject invoke request", detail = ex.Message });
            }
            catch (MissingMemberException ex)
            {
                await UnityMcpBridge.WriteJsonAsync(response, 404, new ErrorResponse { error = "GameObject method not found", detail = ex.Message });
            }
            catch (TargetInvocationException ex)
            {
                await UnityMcpBridge.WriteJsonAsync(response, 500, new ErrorResponse
                {
                    error = "GameObject method invocation failed",
                    detail = ex.InnerException != null ? ex.InnerException.Message : ex.Message,
                    stackTrace = ex.ToString()
                });
            }
        }

        internal static GameObjectInvokeResponse InvokeGameObjectMethodForTest(GameObjectInvokeRequest req)
        {
            return InvokeGameObjectMethod(req);
        }

        private static GameObjectInvokeResponse InvokeGameObjectMethod(GameObjectInvokeRequest req)
        {
            if (req == null)
                throw new ArgumentException("request body is required.");
            if (string.IsNullOrWhiteSpace(req.path))
                throw new ArgumentException("path is required.");
            if (string.IsNullOrWhiteSpace(req.method))
                throw new ArgumentException("method is required.");

            var go = McpSharedHelpers.FindGameObjectByPath(req.path);
            if (go == null)
                go = GameObject.Find(req.path);
            if (go == null)
                throw new MissingMemberException("GameObject not found: " + req.path);

            var args = ResolveInvokeArgs(req);
            var components = go.GetComponents<MonoBehaviour>();
            foreach (var comp in components)
            {
                if (comp == null)
                    continue;

                var method = comp.GetType().GetMethod(req.method, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (method == null)
                    continue;

                var result = InvokeMethod(comp, method, args);
                return new GameObjectInvokeResponse
                {
                    success = true,
                    path = McpSharedHelpers.GetTransformPath(go.transform),
                    componentType = comp.GetType().Name,
                    method = req.method,
                    result = result
                };
            }

            throw new MissingMemberException("Method not found on GameObject components: " + req.method);
        }

        private static string InvokeMethod(MonoBehaviour comp, System.Reflection.MethodInfo method, string[] args)
        {
            var parameters = method.GetParameters();
            var invokeArgs = new object[parameters.Length];

            for (var i = 0; i < parameters.Length; i++)
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
            if (method.ReturnType == typeof(IEnumerator) && result is IEnumerator coroutine)
            {
                comp.StartCoroutine(coroutine);
                return "Coroutine " + method.Name + " started";
            }

            return result != null ? result.ToString() : "void";
        }

        private static string[] ResolveInvokeArgs(GameObjectInvokeRequest req)
        {
            if (req.args != null && req.args.Length > 0)
                return req.args;

            var values = new[] { req.arg0, req.arg1, req.arg2 };
            var count = values.Length;
            while (count > 0 && values[count - 1] == null)
                count--;

            if (count == 0)
                return Array.Empty<string>();

            var result = new string[count];
            Array.Copy(values, result, count);
            return result;
        }

        private static async Task<T> ReadJsonRequestAsync<T>(HttpListenerRequest request) where T : class
        {
            if (!request.HasEntityBody)
                return null;

            var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
            return string.IsNullOrWhiteSpace(body) ? null : JsonUtility.FromJson<T>(body);
        }
    }

    [Serializable]
    internal sealed class GameObjectInvokeRequest
    {
        public string path;
        public string method;
        public string[] args;
        public string arg0;
        public string arg1;
        public string arg2;
    }

    [Serializable]
    internal sealed class GameObjectInvokeResponse
    {
        public bool success;
        public string path;
        public string componentType;
        public string method;
        public string result;
    }
}
#pragma warning restore CS0649
#endif
