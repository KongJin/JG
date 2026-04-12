#if UNITY_EDITOR
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectSD.EditorTools.UnityMcp
{
    [InitializeOnLoad]
    internal static class UnityMcpBridge
    {
        // =====================================================================
        // Constants
        // =====================================================================

        private const int DefaultPort = 51234;
        private const int HealthCheckTimeoutMs = 1000;
        private const int FallbackPortRangeStart = 52000;
        private const int FallbackPortRangeSize = 1000;
        private const int MaxFallbackPortCandidates = 8;
        private const double InitialStartDelaySeconds = 0.5d;
        private const double RetryStartDelaySeconds = 1.0d;
        private const int MaxStartRetryCount = 10;
        private const string PortConfigRelativePath = "ProjectSettings/UnityMcpPort.txt";
        internal const int MaxStoredLogs = 500;
        internal const int MaxLogMessageLength = 2000;
        internal const int MaxStackTraceLength = 6000;
        internal const string DefaultScreenshotDirectoryRelativePath = "Temp/UnityMcp/Screenshots";
        internal const int ScreenshotWriteTimeoutMs = 5000;
        internal const int ScreenshotPollDelayMs = 100;

        // =====================================================================
        // Static Fields
        // =====================================================================

        private static readonly ConcurrentQueue<Action> MainThreadActions = new ConcurrentQueue<Action>();
        internal static readonly object LogLock = new object();
        internal static readonly List<ConsoleLogEntry> ConsoleLogs = new List<ConsoleLogEntry>();
        private static readonly int[] BindRetryDelayMs = { 50, 100, 200 };

        internal static readonly ConcurrentDictionary<string, Type> McpTypeNameCache =
            new ConcurrentDictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
        internal static readonly ConcurrentDictionary<string, FieldInfo> McpFieldInfoCache =
            new ConcurrentDictionary<string, FieldInfo>(StringComparer.Ordinal);
        private static readonly PropertyInfo[] EmptyPropertyList = new PropertyInfo[0];

        private static HttpListener _listener;
        private static CancellationTokenSource _listenerCts;
        private static int _mainThreadId;
        private static int _activePort;
        private static bool _startScheduled;
        private static double _scheduledStartTime;
        private static int _remainingStartRetries;
        private static bool _isPlayModeChanging;

        // =====================================================================
        // Static Constructor
        // =====================================================================

        static UnityMcpBridge()
        {
            _mainThreadId = Thread.CurrentThread.ManagedThreadId;
            EditorApplication.update += DrainMainThreadActions;
            EditorApplication.update += TryStartBridgeFromSchedule;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            Application.logMessageReceivedThreaded += OnLogMessageReceived;
            CompilationPipeline.assemblyCompilationFinished += OnCompilationFinished;
            AssemblyReloadEvents.beforeAssemblyReload += StopBridge;
            EditorApplication.quitting += StopBridge;
            ScheduleStartBridge(InitialStartDelaySeconds, resetRetryCount: true);
        }

        // =====================================================================
        // Menu Items
        // =====================================================================

        [MenuItem("Tools/Unity MCP/Start Bridge")]
        private static void StartBridgeMenu() => StartBridge(resetRetryCount: true);

        [MenuItem("Tools/Unity MCP/Stop Bridge")]
        private static void StopBridgeMenu() => StopBridge();

        [MenuItem("Tools/Unity MCP/Print Status")]
        private static void PrintStatusMenu()
        {
            var port = _activePort > 0 ? _activePort : ResolvePort();
            Debug.LogFormat(
                "[Unity MCP] running={0} playing={1} compiling={2} port={3} prefix={4} config={5} projectKey={6} scene={7}",
                IsRunning, EditorApplication.isPlaying, EditorApplication.isCompiling, port,
                BuildListenerPrefix(port), PortConfigRelativePath, ProjectKey, SceneManager.GetActiveScene().path);
        }

        // =====================================================================
        // Properties
        // =====================================================================

        internal static bool IsRunning => _listener != null && _listener.IsListening;

        internal static int ActivePort => _activePort;

        internal static string ProjectRootPath
        {
            get
            {
                var assetsDirectory = Directory.GetParent(Application.dataPath);
                return assetsDirectory != null ? assetsDirectory.FullName : Directory.GetCurrentDirectory();
            }
        }

        private static string PortConfigPath => Path.Combine(ProjectRootPath, PortConfigRelativePath);

        internal static string ProjectKey => ComputeProjectKey(ProjectRootPath);

        internal static bool IsPlayModeChanging() => _isPlayModeChanging;

        internal static bool IsPlayModeChanging
        {
            get => _isPlayModeChanging;
            set => _isPlayModeChanging = value;
        }

        // =====================================================================
        // Play Mode State
        // =====================================================================

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.ExitingEditMode:
                case PlayModeStateChange.ExitingPlayMode:
                    _isPlayModeChanging = true;
                    break;
                case PlayModeStateChange.EnteredEditMode:
                case PlayModeStateChange.EnteredPlayMode:
                    _isPlayModeChanging = false;
                    break;
            }
        }

        // =====================================================================
        // Bridge Lifecycle
        // =====================================================================

        private static void StartBridge(bool resetRetryCount = false)
        {
            if (resetRetryCount) _remainingStartRetries = MaxStartRetryCount;
            _startScheduled = false;
            var preferredPort = ResolvePort();

            if (IsRunning)
            {
                Debug.Log("[Unity MCP] Bridge already running at " + BuildListenerPrefix(_activePort));
                return;
            }

            StopBridge(logWhenAlreadyStopped: false, logWhenStopped: false);

            foreach (var candidatePort in EnumerateCandidatePorts(preferredPort))
            {
                var listenerPrefix = BuildListenerPrefix(candidatePort);

                if (IsPortInUse(candidatePort))
                {
                    if (TryProbeBridge(listenerPrefix, out var health, out var probeDetail))
                    {
                        if (IsCurrentProjectBridge(health))
                        {
                            _activePort = candidatePort;
                            PersistPort(candidatePort);
                            Debug.Log("[Unity MCP] Bridge already alive for this project at " + listenerPrefix + " (" + probeDetail + "). Reusing existing listener.");
                            return;
                        }
                        Debug.LogWarning("[Unity MCP] Port " + candidatePort + " is already used by a different Unity MCP project (" + probeDetail + "). Trying next candidate.");
                        continue;
                    }
                }

                if (TryStartListener(candidatePort, out var bindDetail))
                {
                    PersistPort(candidatePort);
                    _remainingStartRetries = MaxStartRetryCount;
                    if (candidatePort == preferredPort)
                    {
                        Debug.Log("[Unity MCP] Bridge started at " + listenerPrefix + " (config: " + PortConfigRelativePath + ")");
                    }
                    else
                    {
                        Debug.LogWarning("[Unity MCP] Preferred port " + preferredPort + " was unavailable. Using sticky fallback port " + candidatePort + " at " + listenerPrefix + ".");
                    }
                    return;
                }

                Debug.LogWarning("[Unity MCP] Failed to bind at " + listenerPrefix + ". Detail: " + bindDetail);
            }

            if (_remainingStartRetries > 0)
            {
                var attempt = MaxStartRetryCount - _remainingStartRetries + 1;
                _remainingStartRetries--;
                Debug.LogWarning("[Unity MCP] Could not bind any candidate port for project " + ProjectKey + ". Scheduling retry " + attempt + "/" + MaxStartRetryCount + " in " + RetryStartDelaySeconds.ToString("0.0") + "s.");
                ScheduleStartBridge(RetryStartDelaySeconds);
                return;
            }

            Debug.LogError("[Unity MCP] Failed to start bridge for project " + ProjectKey + ". Tried configured port " + preferredPort + " and sticky fallbacks. See warnings above for the blocked ports.");
            StopBridge(logWhenAlreadyStopped: false, logWhenStopped: false);
        }

        private static void StopBridge() => StopBridge(logWhenAlreadyStopped: true, logWhenStopped: true);

        private static void StopBridge(bool logWhenAlreadyStopped) => StopBridge(logWhenAlreadyStopped, logWhenStopped: true);

        private static void StopBridge(bool logWhenAlreadyStopped, bool logWhenStopped)
        {
            ClearMcpReflectionCaches();

            var listener = _listener;
            var listenerCts = _listenerCts;

            if (listener == null)
            {
                _listener = null;
                _listenerCts = null;
                if (logWhenAlreadyStopped) Debug.Log("[Unity MCP] Bridge already stopped.");
                return;
            }

            try
            {
                if (listenerCts != null && !listenerCts.IsCancellationRequested) listenerCts.Cancel();
            }
            catch (Exception ex) { Debug.LogWarning("[Unity MCP] Failed to cancel listener token: " + ex.Message); }

            try
            {
                if (listener.IsListening) listener.Stop();
                listener.Close();
            }
            catch (Exception ex) { Debug.LogWarning("[Unity MCP] Stop encountered a socket cleanup issue: " + ex.Message); }

            _listener = null;
            _listenerCts = null;
            _activePort = 0;

            if (logWhenStopped) Debug.Log("[Unity MCP] Bridge stopped.");
        }

        private static void ScheduleStartBridge(double delaySeconds, bool resetRetryCount = false)
        {
            if (resetRetryCount) _remainingStartRetries = MaxStartRetryCount;
            var nextAttemptTime = EditorApplication.timeSinceStartup + Math.Max(0d, delaySeconds);
            if (_startScheduled && _scheduledStartTime <= nextAttemptTime) return;
            _scheduledStartTime = nextAttemptTime;
            _startScheduled = true;
        }

        private static void TryStartBridgeFromSchedule()
        {
            if (!_startScheduled) return;
            if (EditorApplication.timeSinceStartup < _scheduledStartTime) return;
            _startScheduled = false;
            StartBridge();
        }

        // =====================================================================
        // Port Management
        // =====================================================================

        internal static int ResolvePort()
        {
            try
            {
                if (!File.Exists(PortConfigPath)) return DefaultPort;
                var raw = File.ReadAllText(PortConfigPath).Trim();
                if (string.IsNullOrEmpty(raw)) return DefaultPort;
                if (int.TryParse(raw, out var port) && port > 0 && port <= 65535) return port;
                Debug.LogWarning("[Unity MCP] Invalid port in " + PortConfigRelativePath + ": " + raw + ". Falling back to " + DefaultPort + ".");
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Unity MCP] Failed to read " + PortConfigRelativePath + ": " + ex.Message + ". Falling back to " + DefaultPort + ".");
            }
            return DefaultPort;
        }

        private static void PersistPort(int port)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(PortConfigPath) ?? ProjectRootPath);
                var nextValue = port.ToString();
                if (File.Exists(PortConfigPath))
                {
                    var currentValue = File.ReadAllText(PortConfigPath).Trim();
                    if (string.Equals(currentValue, nextValue, StringComparison.Ordinal)) return;
                }
                File.WriteAllText(PortConfigPath, nextValue + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Unity MCP] Failed to persist port " + port + " to " + PortConfigRelativePath + ": " + ex.Message);
            }
        }

        private static string BuildListenerPrefix(int port) => "http://127.0.0.1:" + port + "/";

        private static IEnumerable<int> EnumerateCandidatePorts(int preferredPort)
        {
            yield return preferredPort;
            for (var i = 0; i < MaxFallbackPortCandidates; i++)
            {
                var candidate = ComputeStickyFallbackPort(i);
                if (candidate != preferredPort) yield return candidate;
            }
        }

        private static int ComputeStickyFallbackPort(int offset)
        {
            var hash = ComputeStableHash(NormalizeProjectPath(ProjectRootPath));
            var normalizedOffset = Math.Abs(offset % FallbackPortRangeSize);
            var bucket = (int)((hash + (uint)normalizedOffset) % FallbackPortRangeSize);
            return FallbackPortRangeStart + bucket;
        }

        private static uint ComputeStableHash(string value)
        {
            unchecked
            {
                const uint fnvOffsetBasis = 2166136261;
                const uint fnvPrime = 16777619;
                var hash = fnvOffsetBasis;
                for (var i = 0; i < value.Length; i++) { hash ^= value[i]; hash *= fnvPrime; }
                return hash;
            }
        }

        private static string ComputeProjectKey(string projectRootPath) => ComputeStableHash(NormalizeProjectPath(projectRootPath)).ToString("x8");

        private static string NormalizeProjectPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return string.Empty;
            return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Replace('\\', '/').ToLowerInvariant();
        }

        // =====================================================================
        // Port Probing
        // =====================================================================

        private static bool IsPortInUse(int port)
        {
            try { var tcpListener = new TcpListener(IPAddress.Loopback, port); tcpListener.Start(); tcpListener.Stop(); return false; }
            catch (SocketException) { return true; }
        }

        private static bool TryProbeBridge(string listenerPrefix, out HealthResponse health, out string probeDetail)
        {
            health = null;
            try
            {
                var request = WebRequest.CreateHttp(new Uri(new Uri(listenerPrefix), "health"));
                request.Method = "GET";
                request.Timeout = HealthCheckTimeoutMs;
                using (var response = (HttpWebResponse)request.GetResponse())
                using (var stream = response.GetResponseStream())
                using (var reader = stream == null ? null : new StreamReader(stream))
                {
                    if (response.StatusCode != HttpStatusCode.OK) { probeDetail = "health returned " + (int)response.StatusCode; return false; }
                    var body = reader != null ? reader.ReadToEnd() : string.Empty;
                    health = string.IsNullOrWhiteSpace(body) ? null : JsonUtility.FromJson<HealthResponse>(body);
                    probeDetail = DescribeHealthProbe(health, (int)response.StatusCode);
                    return true;
                }
            }
            catch (WebException ex) when (ex.Response is HttpWebResponse httpResponse) { probeDetail = "health returned " + (int)httpResponse.StatusCode; return false; }
            catch { probeDetail = "health probe timed out or failed"; return false; }
        }

        private static bool IsCurrentProjectBridge(HealthResponse health)
        {
            if (health == null) return false;
            if (!string.IsNullOrEmpty(health.projectKey)) return string.Equals(health.projectKey, ProjectKey, StringComparison.Ordinal);
            return !string.IsNullOrEmpty(health.projectRootPath) && string.Equals(NormalizeProjectPath(health.projectRootPath), NormalizeProjectPath(ProjectRootPath), StringComparison.Ordinal);
        }

        private static string DescribeHealthProbe(HealthResponse health, int statusCode)
        {
            if (health == null) return "health returned " + statusCode + " with empty payload";
            var project = !string.IsNullOrEmpty(health.projectKey) ? health.projectKey : NormalizeProjectPath(health.projectRootPath);
            return "health returned " + statusCode + " for project " + project + " on port " + health.port;
        }

        // =====================================================================
        // Listener Management
        // =====================================================================

        private static bool TryStartListener(int port, out string detail)
        {
            var listenerPrefix = BuildListenerPrefix(port);
            for (var attempt = 0; attempt < BindRetryDelayMs.Length; attempt++)
            {
                try
                {
                    CleanupListenerState();
                    _listenerCts = new CancellationTokenSource();
                    _listener = new HttpListener();
                    _listener.Prefixes.Add(listenerPrefix);
                    _listener.Start();
                    _ = Task.Run(() => ListenLoopAsync(_listenerCts.Token));
                    _activePort = port;
                    detail = "started";
                    return true;
                }
                catch (HttpListenerException ex)
                {
                    CleanupListenerState();
                    if (attempt >= BindRetryDelayMs.Length - 1) { detail = ex.Message; return false; }
                    Thread.Sleep(BindRetryDelayMs[attempt]);
                }
                catch (Exception ex) { CleanupListenerState(); detail = ex.Message; return false; }
            }
            detail = "Unknown bind failure.";
            return false;
        }

        private static void CleanupListenerState()
        {
            try { _listenerCts?.Cancel(); } catch { }
            try { _listener?.Close(); } catch { }
            _listener = null;
            _listenerCts = null;
            _activePort = 0;
        }

        // =====================================================================
        // Listen Loop
        // =====================================================================

        private static async Task ListenLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && IsRunning)
            {
                HttpListenerContext context;
                try { context = await _listener.GetContextAsync(); }
                catch (HttpListenerException) when (cancellationToken.IsCancellationRequested) { return; }
                catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested) { return; }
                catch (Exception ex)
                {
                    Debug.LogWarning("[Unity MCP] Listener error: " + ex.Message);
                    try { await Task.Delay(250, cancellationToken); } catch (TaskCanceledException) { return; }
                    continue;
                }
                _ = Task.Run(() => HandleRequestAsync(context), cancellationToken);
            }
        }

        // =====================================================================
        // Request Router
        // =====================================================================

        private static async Task HandleRequestAsync(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;
            response.Headers["Cache-Control"] = "no-cache";

            try
            {
                var method = request.HttpMethod.ToUpperInvariant();
                var path = NormalizePath(request.Url == null ? "/" : request.Url.AbsolutePath);

                // --- Play / Health ---
                if (method == "GET" && path == "/health") { await PlayHandlers.HandleHealthAsync(response); return; }
                if (method == "GET" && path == "/scene/current") { await PlayHandlers.HandleCurrentSceneAsync(response); return; }
                if (method == "POST" && path == "/play/start") { await PlayHandlers.HandlePlayStartAsync(response); return; }
                if (method == "POST" && path == "/play/stop") { await PlayHandlers.HandlePlayStopAsync(response); return; }
                if (method == "POST" && path == "/screenshot/capture") { await PlayHandlers.HandleScreenshotCaptureAsync(request, response); return; }

                // --- Input ---
                if (method == "POST" && path == "/input/click") { await InputHandlers.HandleInputClickAsync(request, response); return; }
                if (method == "POST" && path == "/input/move") { await InputHandlers.HandleInputMoveAsync(request, response); return; }
                if (method == "POST" && path == "/input/drag") { await InputHandlers.HandleInputDragAsync(request, response); return; }
                if (method == "POST" && path == "/input/key") { await InputHandlers.HandleInputKeyAsync(request, response); return; }
                if (method == "POST" && path == "/input/text") { await InputHandlers.HandleInputTextAsync(request, response); return; }
                if (method == "POST" && path == "/input/scroll") { await InputHandlers.HandleInputScrollAsync(request, response); return; }
                if (method == "POST" && path == "/input/key-combo") { await InputHandlers.HandleInputKeyComboAsync(request, response); return; }

                // --- Console ---
                if (method == "GET" && path == "/console/errors") { await ConsoleHandlers.HandleConsoleErrorsAsync(request, response, ConsoleLogs, LogLock); return; }
                if (method == "GET" && path == "/console/logs") { await ConsoleHandlers.HandleConsoleLogsAsync(request, response, ConsoleLogs, LogLock); return; }

                // --- Scene ---
                if (method == "GET" && path == "/scene/hierarchy") { await SceneHandlers.HandleSceneHierarchyAsync(request, response); return; }
                if (method == "POST" && path == "/scene/open") { await SceneHandlers.HandleSceneOpenAsync(request, response); return; }
                if (method == "POST" && path == "/scene/save") { await SceneHandlers.HandleSceneSaveAsync(response); return; }

                // --- GameObject ---
                if (method == "POST" && path == "/gameobject/find") { await GameObjectHandlers.HandleGameObjectFindAsync(request, response); return; }
                if (method == "POST" && path == "/gameobject/create") { await GameObjectHandlers.HandleGameObjectCreateAsync(request, response); return; }
                if (method == "POST" && path == "/gameobject/create-primitive") { await GameObjectHandlers.HandleGameObjectCreatePrimitiveAsync(request, response); return; }
                if (method == "POST" && path == "/gameobject/destroy") { await GameObjectHandlers.HandleGameObjectDestroyAsync(request, response); return; }
                if (method == "POST" && path == "/gameobject/set-active") { await GameObjectHandlers.HandleGameObjectSetActiveAsync(request, response); return; }
                if (method == "POST" && path == "/gameobject/set-sibling") { await GameObjectHandlers.HandleGameObjectSetSiblingAsync(request, response); return; }

                // --- Component ---
                if (method == "POST" && path == "/component/add") { await ComponentHandlers.HandleComponentAddAsync(request, response); return; }
                if (method == "POST" && path == "/component/set") { await ComponentHandlers.HandleComponentSetAsync(request, response); return; }
                if (method == "POST" && path == "/component/get") { await ComponentHandlers.HandleComponentGetAsync(request, response); return; }
                if (method == "POST" && path == "/component/set-serialized-field") { await ComponentHandlers.HandleComponentSetSerializedFieldAsync(request, response); return; }
                if (method == "POST" && path == "/component/auto-connect-fields") { await ComponentHandlers.HandleComponentAutoConnectFieldsAsync(request, response); return; }

                // --- Prefab ---
                if (method == "POST" && path == "/prefab/save") { await PrefabHandlers.HandlePrefabSaveAsync(request, response); return; }
                if (method == "POST" && path == "/prefab/get") { await PrefabHandlers.HandlePrefabGetAsync(request, response); return; }
                if (method == "POST" && path == "/prefab/set") { await PrefabHandlers.HandlePrefabSetAsync(request, response); return; }
                if (method == "POST" && path == "/prefab/add-component") { await PrefabHandlers.HandlePrefabAddComponentAsync(request, response); return; }

                // --- UI ---
                if (method == "POST" && path == "/ui/button/invoke") { await UiHandlers.HandleUiButtonInvokeAsync(request, response); return; }
                if (method == "POST" && path == "/ui/create-button") { await UiHandlers.HandleUiCreateButtonAsync(request, response); return; }
                if (method == "POST" && path == "/ui/create-panel") { await UiHandlers.HandleUiCreatePanelAsync(request, response); return; }
                if (method == "POST" && path == "/ui/create-raw-image") { await UiHandlers.HandleUiCreateRawImageAsync(request, response); return; }
                if (method == "POST" && path == "/ui/set-rect") { await UiHandlers.HandleUiSetRectAsync(request, response); return; }

                // --- Build / Compile / Menu ---
                if (method == "POST" && path == "/asset/refresh") { await BuildHandlers.HandleAssetRefreshAsync(response); return; }
                if (method == "GET" && path == "/compile/status") { await BuildHandlers.HandleCompileStatusAsync(response); return; }
                if (method == "POST" && path == "/compile/request") { await BuildHandlers.HandleCompileRequestAsync(request, response); return; }
                if (method == "POST" && path == "/compile/wait") { await BuildHandlers.HandleCompileWaitAsync(request, response); return; }
                if (method == "POST" && path == "/build/webgl") { await BuildHandlers.HandleBuildWebGLAsync(request, response); return; }
                if (method == "POST" && path == "/menu/execute") { await BuildHandlers.HandleMenuExecuteAsync(request, response); return; }

                await WriteJsonAsync(response, 404, new ErrorResponse { error = "Not found", detail = method + " " + path });
            }
            catch (Exception ex)
            {
                await WriteJsonAsync(response, 500, new ErrorResponse { error = "Bridge failure", detail = ex.Message });
            }
            finally { CloseResponse(response); }
        }

        private static string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path)) return "/";
            var normalized = path.Trim();
            if (!normalized.StartsWith("/", StringComparison.Ordinal)) normalized = "/" + normalized;
            if (normalized.Length > 1 && normalized.EndsWith("/", StringComparison.Ordinal)) normalized = normalized.Substring(0, normalized.Length - 1);
            return normalized;
        }

        // =====================================================================
        // HTTP Utilities
        // =====================================================================

        internal static async Task WriteJsonAsync(HttpListenerResponse response, int statusCode, object payload)
        {
            response.StatusCode = statusCode;
            response.ContentType = "application/json; charset=utf-8";
            var json = JsonUtility.ToJson(payload);
            var buffer = Encoding.UTF8.GetBytes(json);
            response.ContentLength64 = buffer.Length;
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
        }

        internal static async Task WriteRawJsonAsync(HttpListenerResponse response, int statusCode, string json)
        {
            response.StatusCode = statusCode;
            response.ContentType = "application/json; charset=utf-8";
            var buffer = Encoding.UTF8.GetBytes(json);
            response.ContentLength64 = buffer.Length;
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
        }

        private static void CloseResponse(HttpListenerResponse response)
        {
            try { response.Close(); } catch { }
        }

        internal static async Task<string> ReadRequestBodyAsync(HttpListenerRequest request)
        {
            using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
            {
                return await reader.ReadToEndAsync();
            }
        }

        // =====================================================================
        // Main Thread Dispatch
        // =====================================================================

        internal static Task<T> RunOnMainThreadAsync<T>(Func<T> func)
        {
            var tcs = new TaskCompletionSource<T>();
            MainThreadActions.Enqueue(() =>
            {
                try { tcs.SetResult(func()); }
                catch (Exception ex) { tcs.SetException(ex); }
            });
            return tcs.Task;
        }

        internal static Task RunOnMainThreadAsync(Action action)
        {
            var tcs = new TaskCompletionSource<bool>();
            MainThreadActions.Enqueue(() =>
            {
                try { action(); tcs.SetResult(true); }
                catch (Exception ex) { tcs.SetException(ex); }
            });
            return tcs.Task;
        }

        private static void DrainMainThreadActions()
        {
            while (MainThreadActions.TryDequeue(out var action))
            {
                try { action(); }
                catch (Exception ex) { Debug.LogError("[Unity MCP] Main thread action failed: " + ex.Message); }
            }
        }

        // =====================================================================
        // Console Logging
        // =====================================================================

        private static void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            lock (LogLock)
            {
                ConsoleLogs.Add(new ConsoleLogEntry
                {
                    type = type.ToString(),
                    condition = Truncate(condition, MaxLogMessageLength),
                    stackTrace = Truncate(stackTrace, MaxStackTraceLength),
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff")
                });
                while (ConsoleLogs.Count > MaxStoredLogs) ConsoleLogs.RemoveAt(0);
            }
        }

        private static void OnCompilationFinished(string assemblyName, UnityEditor.Compilation.CompilerMessage[] messages)
        {
            lock (LogLock)
            {
                foreach (var m in messages)
                {
                    ConsoleLogs.Add(new ConsoleLogEntry
                    {
                        type = (m.type == CompilerMessageType.Error ? LogType.Error : LogType.Warning).ToString(),
                        condition = Truncate(m.message, MaxLogMessageLength),
                        stackTrace = string.Empty,
                        timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff")
                    });
                }
                while (ConsoleLogs.Count > MaxStoredLogs) ConsoleLogs.RemoveAt(0);
            }
        }

        internal static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength) + "...";
        }

        // =====================================================================
        // Reflection Cache
        // =====================================================================

        internal static void ClearMcpReflectionCaches()
        {
            McpTypeNameCache.Clear();
            McpFieldInfoCache.Clear();
        }

        internal static Type ResolveTypeByFullName(string typeName)
        {
            if (McpTypeNameCache.TryGetValue(typeName, out var cached)) return cached;

            var resolvedType = Type.GetType(typeName);
            if (resolvedType != null)
            {
                McpTypeNameCache[typeName] = resolvedType;
                return resolvedType;
            }

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var asm in assemblies)
            {
                resolvedType = asm.GetType(typeName);
                if (resolvedType != null)
                {
                    McpTypeNameCache[typeName] = resolvedType;
                    return resolvedType;
                }
            }

            var shortName = typeName.Contains('.') ? typeName.Substring(typeName.LastIndexOf('.') + 1) : typeName;
            foreach (var asm in assemblies)
            {
                var types = asm.GetTypes();
                foreach (var t in types)
                {
                    if (t.Name.Equals(shortName, StringComparison.OrdinalIgnoreCase))
                    {
                        McpTypeNameCache[typeName] = t;
                        return t;
                    }
                }
            }

            return null;
        }

        internal static FieldInfo ResolveFieldInfo(Type type, string fieldName)
        {
            var cacheKey = type.FullName + "::" + fieldName;
            if (McpFieldInfoCache.TryGetValue(cacheKey, out var cached)) return cached;

            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            var field = type.GetField(fieldName, flags);
            if (field != null) McpFieldInfoCache[cacheKey] = field;
            return field;
        }
    }
}
#endif
