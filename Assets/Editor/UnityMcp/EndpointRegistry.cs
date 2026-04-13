#if UNITY_EDITOR
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace ProjectSD.EditorTools.UnityMcp
{
    /// <summary>
    /// MCP 엔드포인트를 동적으로 등록·디스패치하는 레지스트리.
    /// 각 핸들러의 static constructor에서 Register()를 호출하여 자동 등록된다.
    /// </summary>
    internal static class EndpointRegistry
    {
        private static readonly ConcurrentDictionary<string, IMcpEndpoint> Endpoints =
            new ConcurrentDictionary<string, IMcpEndpoint>(StringComparer.OrdinalIgnoreCase);

        private static int _registrationCount;

        public static void Register(IMcpEndpoint endpoint)
        {
            if (endpoint == null) throw new ArgumentNullException(nameof(endpoint));
            var key = endpoint.Method.ToUpperInvariant() + " " + endpoint.Path;
            Endpoints[key] = endpoint;
            _registrationCount++;
        }

        public static async Task<bool> TryDispatch(string method, string path, HttpListenerRequest request, HttpListenerResponse response)
        {
            var key = method.ToUpperInvariant() + " " + path;
            if (Endpoints.TryGetValue(key, out var endpoint))
            {
                await endpoint.HandleAsync(request, response);
                return true;
            }
            return false;
        }

        public static async Task HandleNotFoundAsync(HttpListenerResponse response, string method, string path)
        {
            var availablePaths = Endpoints.Values
                .GroupBy(e => e.Method.ToUpperInvariant())
                .OrderBy(g => g.Key)
                .SelectMany(g => g.OrderBy(e => e.Path).Select(e => "  " + e.Method.ToUpperInvariant() + " " + e.Path))
                .ToArray();

            var body = new
            {
                error = "Not found",
                detail = method + " " + path,
                hint = "Registered endpoints:",
                endpoints = availablePaths
            };

            await UnityMcpBridge.WriteJsonAsync(response, 404, body);
        }

        public static async Task HandleListEndpointsAsync(HttpListenerResponse response)
        {
            var list = Endpoints.Values
                .OrderBy(e => e.Method.ToUpperInvariant())
                .ThenBy(e => e.Path)
                .Select(e => new { method = e.Method.ToUpperInvariant(), path = e.Path, description = e.Description })
                .ToArray();

            var body = new
            {
                count = list.Length,
                endpoints = list
            };

            await UnityMcpBridge.WriteJsonAsync(response, 200, body);
        }

        public static int RegistrationCount => _registrationCount;
    }

    /// <summary>
    /// 단일 MCP 엔드포인트 계약.
    /// </summary>
    internal interface IMcpEndpoint
    {
        string Method { get; }
        string Path { get; }
        string Description { get; }
        Task HandleAsync(HttpListenerRequest request, HttpListenerResponse response);
    }

    /// <summary>
    /// Func 기반 엔드포인트 등록 헬퍼.
    /// </summary>
    internal sealed class FuncEndpoint : IMcpEndpoint
    {
        private readonly Func<HttpListenerRequest, HttpListenerResponse, Task> _handler;

        public string Method { get; }
        public string Path { get; }
        public string Description { get; }

        public FuncEndpoint(string method, string path, string description, Func<HttpListenerRequest, HttpListenerResponse, Task> handler)
        {
            Method = method;
            Path = path;
            Description = description;
            _handler = handler;
        }

        public async Task HandleAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            await _handler(request, response);
        }
    }

    internal static class EndpointRegistryExtensions
    {
        public static void Register(this string method, string path, string description, Func<HttpListenerRequest, HttpListenerResponse, Task> handler)
        {
            EndpointRegistry.Register(new FuncEndpoint(method, path, description, handler));
        }
    }
}
#endif
