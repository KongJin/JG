#if UNITY_EDITOR
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace ProjectSD.EditorTools.UnityMcp
{
    /// <summary>
    /// Play/Stop 요청을 순차적으로 처리하는 큐.
    /// 동시 요청 경합을 방지하고 실제 상태 변경 완료까지 비동기 대기한다.
    /// </summary>
    internal static class PlayModeChangeQueue
    {
        private sealed class PlayModeAction
        {
            public readonly Func<PlayResponse> Action;
            public readonly TaskCompletionSource<PlayResponse> Tcs;

            public PlayModeAction(Func<PlayResponse> action, TaskCompletionSource<PlayResponse> tcs)
            {
                Action = action;
                Tcs = tcs;
            }
        }

        private static readonly ConcurrentQueue<PlayModeAction> Queue = new ConcurrentQueue<PlayModeAction>();
        private static readonly object Lock = new object();
        private static bool _processing;
        private static bool _changing;

        /// <summary>
        /// Play/Stop 작업을 큐에 등록하고 완료까지 대기한다.
        /// </summary>
        public static Task<PlayResponse> EnqueueAsync(Func<PlayResponse> action, int timeoutMs = 10000)
        {
            var tcs = new TaskCompletionSource<PlayResponse>();
            var item = new PlayModeAction(action, tcs);
            Queue.Enqueue(item);

            TryProcess();

            // 타임아웃 적용
            var cts = new System.Threading.CancellationTokenSource(timeoutMs);
            cts.Token.Register(() =>
            {
                if (!tcs.Task.IsCompleted)
                {
                    tcs.SetException(new TimeoutException("Play mode change timed out after " + timeoutMs + "ms"));
                }
            });

            return tcs.Task;
        }

        private static void TryProcess()
        {
            lock (Lock)
            {
                if (_processing) return;
                _processing = true;
            }

            _ = ProcessLoopAsync();
        }

        private static async Task ProcessLoopAsync()
        {
            while (Queue.TryDequeue(out var action))
            {
                try
                {
                    _changing = true;
                    var result = action.Action();
                    await Task.Delay(250); // Unity 상태 전파 대기
                    _changing = false;

                    action.Tcs.SetResult(result);
                }
                catch (Exception ex)
                {
                    _changing = false;
                    action.Tcs.SetException(ex);
                }

                await Task.Delay(100); // 연속 처리 간격
            }

            lock (Lock) { _processing = false; }
        }

        /// <summary>
        /// 현재 Play 모드 변경 진행 중인지 확인.
        /// </summary>
        public static bool IsChanging => _changing;
    }
}
#endif
