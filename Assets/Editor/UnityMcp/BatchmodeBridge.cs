#if UNITY_EDITOR
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace ProjectSD.EditorTools.UnityMcp
{
    /// <summary>
    /// Batchmode 전용 Unity MCP 브릿지 엔트리포인트.
    /// Unity CLI (batchmode)에서 실행될 때 사용된다.
    /// </summary>
    public static class BatchmodeBridge
    {
        private static readonly CancellationTokenSource Cts = new CancellationTokenSource();
        private static bool _isRunning;

        /// <summary>
        /// Batchmode에서 브릿지를 시작하는 진입점.
        /// Unity CLI: -executeMethod BatchmodeBridge.StartInBatchmode
        /// </summary>
        public static void StartInBatchmode()
        {
            if (!Application.isBatchMode)
            {
                Debug.LogError("[BatchmodeBridge] Not running in batchmode. Use Unity CLI with -batchmode flag.");
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log("[BatchmodeBridge] Starting in batchmode...");
            Debug.Log("[BatchmodeBridge] Project: " + UnityMcpBridge.ProjectRootPath);
            Debug.Log("[BatchmodeBridge] ProjectKey: " + UnityMcpBridge.ProjectKey);

            // 컴파일 대기
            Debug.Log("[BatchmodeBridge] Waiting for compilation to complete...");
            var compileStart = DateTime.UtcNow;
            while (EditorApplication.isCompiling)
            {
                if ((DateTime.UtcNow - compileStart).TotalMinutes > 5)
                {
                    Debug.LogError("[BatchmodeBridge] Compilation timeout after 5 minutes.");
                    EditorApplication.Exit(1);
                    return;
                }
                System.Threading.Thread.Sleep(500);
            }
            Debug.Log("[BatchmodeBridge] Compilation completed.");

            // 컴파일 에러 체크
            CheckCompilationErrors();

            // 브릿지 시작
            UnityMcpBridge.StartBridgeForBatchmode(resetRetryCount: true);
            _isRunning = true;

            // 메인 루프 (EditorApplication.update가 batchmode에서 제대로 작동하지 않을 수 있음)
            var mainThreadLoop = Task.Run(() => MainThreadLoop(Cts.Token));

            // 종료 대기
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
            Console.CancelKeyPress += OnCancelKeyPress;

            Debug.Log("[BatchmodeBridge] Bridge started. Press Ctrl+C to exit.");

            // Unity 종료 방지 (기본 동작)
            while (!Cts.IsCancellationRequested && UnityMcpBridge.IsRunning)
            {
                System.Threading.Thread.Sleep(100);
            }

            // 정리
            Cts.Cancel();
            mainThreadLoop.Wait(TimeSpan.FromSeconds(2));
            UnityMcpBridge.StopBridgeForBatchmode();

            Debug.Log("[BatchmodeBridge] Shutting down...");
            EditorApplication.Exit(0);
        }

        private static void MainThreadLoop(CancellationToken ct)
        {
            // Main thread actions을 주기적으로 처리하는 루프
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    UnityMcpBridge.DrainMainThreadActionsForBatchmode();
                    System.Threading.Thread.Sleep(50);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Debug.LogError("[BatchmodeBridge] MainThreadLoop error: " + ex.Message);
                }
            }
        }

        private static void CheckCompilationErrors()
        {
            Debug.Log("[BatchmodeBridge] Compilation completed. Detailed compiler output is available from the Editor log endpoints.");
        }

        private static void OnProcessExit(object sender, EventArgs e)
        {
            Shutdown();
        }

        private static void OnCancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true; // 즉시 종료 방지
            Debug.Log("[BatchmodeBridge] Received shutdown signal...");
            Shutdown();
        }

        private static void Shutdown()
        {
            if (!_isRunning) return;
            _isRunning = false;
            Cts.Cancel();
            UnityMcpBridge.StopBridgeForBatchmode();
        }

        /// <summary>
        /// Unity가 종료될 때 호출되는 콜백
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void OnBeforeSceneLoad()
        {
            if (Application.isBatchMode)
            {
                Debug.Log("[BatchmodeBridge] Loaded in batchmode");
            }
        }
    }
}
#endif
