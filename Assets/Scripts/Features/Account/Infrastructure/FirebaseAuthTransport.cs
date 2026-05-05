using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Features.Account.Infrastructure
{
    [Serializable]
    internal class SignUpResponse
    {
        public string idToken = string.Empty;
        public string refreshToken = string.Empty;
        public string localId = string.Empty;
        public string expiresIn = string.Empty;
    }

    [Serializable]
    internal class SignInWithIdpResponse
    {
        public string idToken = string.Empty;
        public string refreshToken = string.Empty;
        public string localId = string.Empty;
        public string expiresIn = string.Empty;
        public string providerId = string.Empty;
        public string displayName = string.Empty;
        public bool isNewUser;
    }

    [Serializable]
    internal class TokenResponse
    {
        public string id_token = string.Empty;
        public string refresh_token = string.Empty;
        public string expires_in = string.Empty;
        public string token_type = string.Empty;
        public string user_id = string.Empty;
    }

    internal sealed class FirebaseAuthHttpClient
    {
        public async Task<T> PostJsonAsync<T>(string url, string body)
        {
            using var request = BuildRequest(url, "application/json", body);
            await SendRequestAsync(request);
            return JsonUtility.FromJson<T>(request.downloadHandler.text);
        }

        public async Task<T> PostFormAsync<T>(string url, string body)
        {
            using var request = BuildRequest(url, "application/x-www-form-urlencoded", body);
            await SendRequestAsync(request);
            return JsonUtility.FromJson<T>(request.downloadHandler.text);
        }

        public async Task PostJsonAsync(string url, string body)
        {
            using var request = BuildRequest(url, "application/json", body);
            await SendRequestAsync(request);
        }

        private static UnityWebRequest BuildRequest(string url, string contentType, string body)
        {
            var request = new UnityWebRequest(url, "POST")
            {
                timeout = 15
            };
            request.SetRequestHeader("Content-Type", contentType);
            request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(body));
            request.downloadHandler = new DownloadHandlerBuffer();
            return request;
        }

        private static async Task SendRequestAsync(UnityWebRequest request)
        {
            var tcs = new TaskCompletionSource<bool>();
            const int timeoutMs = 30000;
            var startTime = DateTime.UtcNow;

            var operation = request.SendWebRequest();
            operation.completed += _ =>
            {
                var elapsedTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;

                if (request.result != UnityWebRequest.Result.Success)
                {
                    // csharp-guardrails: allow-null-defense
                    var error = request.error ?? "Unknown error";
                    Debug.LogError($"[FirebaseAuth] Request failed ({elapsedTime}ms): {error} - {request.url}");
                    tcs.TrySetException(new Exception($"[{elapsedTime}ms] {error}"));
                    return;
                }

                Debug.Log($"[FirebaseAuth] Request succeeded ({elapsedTime}ms): {request.url}");
                tcs.TrySetResult(true);
            };

            using var timeoutCts = new System.Threading.CancellationTokenSource();
            var timeoutTask = Task.Delay(timeoutMs, timeoutCts.Token);
            var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

            if (completedTask == timeoutTask && !operation.isDone)
            {
                request.Abort();
                tcs.TrySetException(new TimeoutException($"FirebaseAuth request timeout after {timeoutMs}ms: {request.url}"));
            }

            timeoutCts.Cancel();
            await tcs.Task;
        }
    }

    internal static class FirebaseAuthResponseMapper
    {
        public static AccountSessionSnapshot FromSignUpResponse(SignUpResponse response)
        {
            return new AccountSessionSnapshot
            {
                IdToken = response.idToken,
                RefreshToken = response.refreshToken,
                Uid = response.localId,
                ExpiryUnixMs = ComputeExpiry(response.expiresIn)
            };
        }

        public static AccountSessionSnapshot FromIdpResponse(SignInWithIdpResponse response)
        {
            return new AccountSessionSnapshot
            {
                IdToken = response.idToken,
                RefreshToken = response.refreshToken,
                Uid = response.localId,
                ExpiryUnixMs = ComputeExpiry(response.expiresIn)
            };
        }

        public static AccountSessionSnapshot FromTokenResponse(TokenResponse response, string currentUid)
        {
            return new AccountSessionSnapshot
            {
                IdToken = response.id_token,
                RefreshToken = response.refresh_token,
                Uid = string.IsNullOrWhiteSpace(response.user_id) ? currentUid : response.user_id,
                ExpiryUnixMs = ComputeExpiry(response.expires_in)
            };
        }

        private static long ComputeExpiry(string expiresInSecondsText)
        {
            if (long.TryParse(expiresInSecondsText, out long expiresInSeconds) && expiresInSeconds > 0)
                return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + (expiresInSeconds * 1000L);

            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + (55L * 60L * 1000L);
        }
    }
}