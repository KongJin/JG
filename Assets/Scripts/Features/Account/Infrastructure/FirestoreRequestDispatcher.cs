using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Features.Account.Infrastructure
{
    internal static class FirestoreRequestDispatcher
    {
        public static async Task<SendResult> SendRequestSafeAsync(UnityWebRequest request)
        {
            const int timeoutMs = 30000;
            var tcs = new TaskCompletionSource<SendResult>();
            var startTime = DateTime.UtcNow;

            var operation = request.SendWebRequest();
            operation.completed += _ =>
            {
                var elapsedTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;

                if (request.result != UnityWebRequest.Result.Success)
                {
                    if (request.responseCode == 404)
                    {
                        Debug.LogWarning($"[Firestore] Document not found ({elapsedTime}ms): {request.url}");
                        tcs.TrySetResult(new SendResult { success = false, notFound = true });
                        return;
                    }

                    // csharp-guardrails: allow-null-defense
                    var error = request.error ?? "Unknown error";
                    Debug.LogError($"[Firestore] Request failed ({elapsedTime}ms): HTTP {request.responseCode} - {error}\n{request.downloadHandler?.text}");
                    tcs.TrySetResult(new SendResult { success = false, error = $"HTTP {request.responseCode}: {error}" });
                    return;
                }

                Debug.Log($"[Firestore] Request succeeded ({elapsedTime}ms): {request.url}");
                tcs.TrySetResult(new SendResult { success = true });
            };

            using var timeoutCts = new System.Threading.CancellationTokenSource();
            var timeoutTask = Task.Delay(timeoutMs, timeoutCts.Token);
            var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

            if (completedTask == timeoutTask && !operation.isDone)
            {
                request.Abort();
                Debug.LogError($"[Firestore] Request timeout after {timeoutMs}ms: {request.url}");
                tcs.TrySetResult(new SendResult { success = false, error = $"Timeout after {timeoutMs}ms" });
            }

            timeoutCts.Cancel();
            return await tcs.Task;
        }

        public static async Task SendRequestAsync(UnityWebRequest request)
        {
            const int timeoutMs = 30000;
            var tcs = new TaskCompletionSource<bool>();
            var startTime = DateTime.UtcNow;

            var operation = request.SendWebRequest();
            operation.completed += _ =>
            {
                var elapsedTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;

                if (request.result != UnityWebRequest.Result.Success)
                {
                    string message = $"HTTP {request.responseCode} {request.error}";
                    Debug.LogError($"[Firestore] Request failed ({elapsedTime}ms): {message}\n{request.downloadHandler?.text}");
                    tcs.TrySetException(new Exception($"[{elapsedTime}ms] {message}"));
                    return;
                }

                Debug.Log($"[Firestore] Request succeeded ({elapsedTime}ms): {request.url}");
                tcs.TrySetResult(true);
            };

            using var timeoutCts = new System.Threading.CancellationTokenSource();
            var timeoutTask = Task.Delay(timeoutMs, timeoutCts.Token);
            var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

            if (completedTask == timeoutTask && !operation.isDone)
            {
                request.Abort();
                Debug.LogError($"[Firestore] Request timeout after {timeoutMs}ms: {request.url}");
                tcs.TrySetException(new TimeoutException($"Timeout after {timeoutMs}ms: {request.url}"));
            }

            timeoutCts.Cancel();
            await tcs.Task;
        }
    }

    public struct SendResult
    {
        public bool success;
        public bool notFound;
        public string error;
        public string body;
    }
}