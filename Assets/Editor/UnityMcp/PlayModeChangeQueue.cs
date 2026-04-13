#if UNITY_EDITOR
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace ProjectSD.EditorTools.UnityMcp
{
    /// <summary>
    /// Play/Stop 요청을 Unity 메인 스레드에서 순차 처리한다.
    /// HTTP 핸들러는 백그라운드 스레드에서 실행되므로
    /// EditorApplication.update를 통해 메인 스레드로 마샬링한다.
    /// </summary>
    internal static class PlayModeChangeQueue
    {
        private enum PendingAction
        {
            None,
            Play,
            Stop,
        }

        private enum State
        {
            Idle,
            Changing,
            Completed,
            TimedOut,
        }

        private static readonly object Lock = new object();
        private static PendingAction _pending = PendingAction.None;
        private static TaskCompletionSource<PlayResponse> _pendingTcs;
        private static State _state = State.Idle;
        private static CancellationTokenSource _timeoutCts;
        private static bool _initialized;

        /// <summary>
        /// 초기화는 처음 한 번만, 메인 스레드-safe한 시점에 한다.
        /// </summary>
        [InitializeOnLoadMethod]
        private static void Init()
        {
            if (_initialized) return;
            _initialized = true;
            EditorApplication.update -= Drain;
            EditorApplication.update += Drain;
        }

        public static bool IsChanging
        {
            get
            {
                lock (Lock) { return _state == State.Changing; }
            }
        }

        /// <summary>
        /// Play 모드 진입을 예약하고 완료까지 대기한다.
        /// </summary>
        public static async Task<PlayResponse> EnqueuePlayAsync(int timeoutMs = 10000)
        {
            return await EnqueueAsync(PendingAction.Play, timeoutMs);
        }

        /// <summary>
        /// Play 모드 종료를 예약하고 완료까지 대기한다.
        /// </summary>
        public static async Task<PlayResponse> EnqueueStopAsync(int timeoutMs = 10000)
        {
            return await EnqueueAsync(PendingAction.Stop, timeoutMs);
        }

        private static async Task<PlayResponse> EnqueueAsync(PendingAction action, int timeoutMs)
        {
            lock (Lock)
            {
                if (_pending != PendingAction.None)
                {
                    throw new InvalidOperationException("A play mode change is already pending.");
                }
                _pending = action;
                _pendingTcs = new TaskCompletionSource<PlayResponse>();
                _state = State.Idle;
                if (_timeoutCts != null)
                {
                    _timeoutCts.Dispose();
                    _timeoutCts = null;
                }
            }

            _timeoutCts = new System.Threading.CancellationTokenSource(timeoutMs);
            _timeoutCts.Token.Register(() =>
            {
                lock (Lock)
                {
                    if (_pendingTcs != null && !_pendingTcs.Task.IsCompleted && _state != State.Completed)
                    {
                        _state = State.TimedOut;
                        _pendingTcs.SetException(new TimeoutException("Play mode change timed out after " + timeoutMs + "ms"));
                        CancelPendingLocked();
                    }
                }
            });

            return await _pendingTcs.Task;
        }

        private static void CancelPendingLocked()
        {
            _pending = PendingAction.None;
            _pendingTcs = null;
            if (_timeoutCts != null)
            {
                _timeoutCts.Dispose();
                _timeoutCts = null;
            }
        }

        /// <summary>
        /// EditorApplication.update에서 매 프레임 호출된다.
        /// 대기 중인 액션이 있으면 메인 스레드에서 실행한다.
        /// </summary>
        private static void Drain()
        {
            PendingAction action;
            lock (Lock)
            {
                if (_pending == PendingAction.None) return;
                if (_state == State.Changing) return;
                action = _pending;
            }

            // 컴파일 중이면 스킵
            if (EditorApplication.isCompiling)
                return;

            // Stop 요청일 때는 isPlayingOrWillChangePlaymode가 true여도 처리 (플레이 모드에서 정지 가능)
            // Play 요청일 때만 진입 중 중복 진입 방지
            if (action == PendingAction.Play && EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            TaskCompletionSource<PlayResponse> tcs;
            lock (Lock)
            {
                // 다시 체크 — 대기 중에 다른 곳에서 이미 처리됐을 수 있음
                if (_pending == PendingAction.None) return;
                _pending = PendingAction.None;
                _state = State.Changing;
                tcs = _pendingTcs;
            }

            try
            {
                if (action == PendingAction.Play)
                {
                    EditorApplication.isPlaying = true;
                }
                else
                {
                    EditorApplication.isPlaying = false;
                }

                // Unity의 실제 play mode 상태 전환은 다음 프레임에 반영된다.
                var isPlaying = action == PendingAction.Play;
                tcs.SetResult(new PlayResponse
                {
                    action = isPlaying ? "start" : "stop",
                    isPlaying = isPlaying,
                    isPlayingOrWillChange = true,
                    isPlayModeChanging = true,
                    rawIsPlayingOrWillChange = true
                });
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
            finally
            {
                lock (Lock)
                {
                    _state = State.Completed;
                    CancelPendingLocked();
                }
            }
        }
    }
}
#endif
