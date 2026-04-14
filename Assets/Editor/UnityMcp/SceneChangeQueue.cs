#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectSD.EditorTools.UnityMcp
{
    /// <summary>
    /// Play Mode 중 씬 변경 요청을 큐에 저장했다가 Play Mode 종료 시 자동 적용한다.
    /// GameObject 생성/수정/삭제, Rect 변경 등 모든 씬 수정 작업에 대해 동작한다.
    /// </summary>
    [InitializeOnLoad]
    internal static class SceneChangeQueue
    {
        private struct PendingChange
        {
            public Func<bool> Execute;
            public string Description;
        }

        private static readonly object Lock = new object();
        private static readonly List<PendingChange> PendingChanges = new List<PendingChange>();
        private static bool _isProcessing;
        private static Action<string, int, int> _onQueueProcessed;
        private static bool _wasInPlayMode;

        /// <summary>
        /// Static constructor — 에디터 시작 시 이벤트 등록.
        /// </summary>
        static SceneChangeQueue()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;
            _wasInPlayMode = EditorApplication.isPlayingOrWillChangePlaymode;
        }

        /// <summary>
        /// 도메인 리로드 후 이벤트 재등록.
        /// </summary>
        [InitializeOnLoadMethod]
        private static void InitOnLoad()
        {
            Debug.Log("[Unity MCP] SceneChangeQueue InitOnLoad called");
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;
            _wasInPlayMode = EditorApplication.isPlayingOrWillChangePlaymode;
        }

        /// <summary>
        /// 매 프레임 실행 — 플레이 모드 종료 감지 시 큐 처리.
        /// </summary>
        private static void OnEditorUpdate()
        {
            var isInPlayMode = EditorApplication.isPlayingOrWillChangePlaymode;
            if (_wasInPlayMode && !isInPlayMode && PendingCount > 0)
            {
                _wasInPlayMode = isInPlayMode;
                // 에디터 모드 진입 직후 — 메인 스레드이므로 직접 처리
                try
                {
                    ProcessQueue();
                }
                catch (Exception ex)
                {
                    Debug.LogError("[Unity MCP] SceneChangeQueue processing failed: " + ex);
                }
            }
            _wasInPlayMode = isInPlayMode;
        }

        /// <summary>
        /// 현재 Play Mode인지 확인
        /// </summary>
        public static bool IsInPlayMode => EditorApplication.isPlayingOrWillChangePlaymode;

        /// <summary>
        /// 대기 중인 변경 요청 수
        /// </summary>
        public static int PendingCount
        {
            get
            {
                lock (Lock) { return PendingChanges.Count; }
            }
        }

        /// <summary>
        /// 큐 처리 완료 시 호출될 콜백을 등록한다.
        /// </summary>
        /// <param name="callback">콜백 (scenePath, successCount, failCount)</param>
        public static void OnQueueProcessed(Action<string, int, int> callback)
        {
            _onQueueProcessed = callback;
        }

        /// <summary>
        /// Play Mode 상태가 변경될 때 큐를 처리한다.
        /// EnteredEditMode → 대기 중인 변경사항 적용 (메인 스레드에서 직접 실행)
        /// </summary>
        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                // 에디터 모드 진입 시 바로 처리 — 메인 스레드이므로 직접 실행
                try
                {
                    ProcessQueue();
                }
                catch (Exception ex)
                {
                    Debug.LogError("[Unity MCP] SceneChangeQueue processing failed: " + ex);
                }
            }
        }

        /// <summary>
        /// 씬 변경 작업을 큐에 추가한다.
        /// Play Mode 중이면 false를 반환하고 큐에 저장, 아니면 즉시 실행한다.
        /// </summary>
        /// <param name="execute">실행할 액션 (true: 성공, false: 실패)</param>
        /// <param name="description">디버깅용 설명</param>
        /// <param name="error">실행 중 발생한 예외 메시지 (out)</param>
        /// <returns>1: 즉시 실행 성공, 0: 큐 저장됨, -1: 실패(예외)</returns>
        public static int EnqueueOrExecute(Func<bool> execute, string description, out string error)
        {
            error = null;

            if (!IsInPlayMode)
            {
                try
                {
                    var success = execute();
                    return success ? 1 : -1;
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                    return -1;
                }
            }

            lock (Lock)
            {
                PendingChanges.Add(new PendingChange
                {
                    Execute = execute,
                    Description = description,
                });
            }

            Debug.Log("[Unity MCP] Scene change queued (Play Mode active): " + description);
            return 0;
        }

        /// <summary>
        /// 큐에 저장된 모든 변경사항을 적용한다.
        /// </summary>
        private static bool ProcessQueue()
        {
            List<PendingChange> changesToProcess;
            lock (Lock)
            {
                if (PendingChanges.Count == 0) return true;
                if (_isProcessing) return false;
                _isProcessing = true;

                changesToProcess = new List<PendingChange>(PendingChanges);
                PendingChanges.Clear();
            }

            Debug.Log("[Unity MCP] Processing " + changesToProcess.Count + " pending scene change(s)...");

            var successCount = 0;
            var failCount = 0;

            foreach (var change in changesToProcess)
            {
                try
                {
                    var result = change.Execute();
                    if (result)
                    {
                        successCount++;
                        Debug.Log("[Unity MCP] Applied: " + change.Description);
                    }
                    else
                    {
                        failCount++;
                        Debug.LogWarning("[Unity MCP] Failed to apply: " + change.Description);
                    }
                }
                catch (Exception ex)
                {
                    failCount++;
                    Debug.LogError("[Unity MCP] Exception applying change: " + change.Description + " - " + ex.Message);
                }
            }

            if (successCount > 0)
            {
                try
                {
                    var activeScene = SceneManager.GetActiveScene();
                    if (activeScene.IsValid())
                    {
                        EditorSceneManager.MarkSceneDirty(activeScene);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[Unity MCP] Failed to mark scene dirty: " + ex.Message);
                }
            }

            Debug.Log("[Unity MCP] Scene changes processed: " + successCount + " succeeded, " + failCount + " failed.");

            var callback = _onQueueProcessed;
            if (callback != null)
            {
                try
                {
                    var scene = SceneManager.GetActiveScene();
                    callback(scene.IsValid() ? scene.path : "(unknown)", successCount, failCount);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[Unity MCP] OnQueueProcessed callback failed: " + ex.Message);
                }
            }

            lock (Lock)
            {
                _isProcessing = false;
            }

            return failCount == 0;
        }

        /// <summary>
        /// 큐에 저장된 모든 변경사항을 취소한다.
        /// </summary>
        public static void ClearQueue()
        {
            lock (Lock)
            {
                PendingChanges.Clear();
            }
        }
    }
}
#endif
