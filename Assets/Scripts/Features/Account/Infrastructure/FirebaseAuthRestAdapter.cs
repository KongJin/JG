using Features.Account.Application.Ports;
using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Features.Account.Infrastructure
{
    /// <summary>
    /// Firebase Auth REST API 어댑터.
    /// UnityWebRequest 사용 (WebGL 호환).
    /// </summary>
    public sealed class FirebaseAuthRestAdapter : IAuthPort
    {
        private const string SignUpUrl = "https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={0}";
        private const string SignInWithIdpUrl = "https://identitytoolkit.googleapis.com/v1/accounts:signInWithIdp?key={0}";
        private const string TokenUrl = "https://securetoken.googleapis.com/v1/token?key={0}";
        private const string DeleteUrl = "https://identitytoolkit.googleapis.com/v1/accounts:delete?key={0}";

        private readonly string _apiKey;
        private string _currentIdToken;
        private string _currentRefreshToken;
        private string _currentUid;
        private long _tokenExpiryTimeMs;

        public FirebaseAuthRestAdapter(string apiKey)
        {
            _apiKey = apiKey;
        }

        public string GetCurrentUid() => _currentUid;

        public void SignOut()
        {
            _currentIdToken = null;
            _currentRefreshToken = null;
            _currentUid = null;
            _tokenExpiryTimeMs = 0;
        }

        public async Task<string> GetIdToken()
        {
            if (string.IsNullOrEmpty(_currentIdToken))
            {
                throw new InvalidOperationException("Not signed in. Call SignIn first.");
            }

            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (now >= _tokenExpiryTimeMs - 300000) // 만료 5분 전
            {
                await RefreshToken();
            }

            return _currentIdToken;
        }

        public async Task<AuthToken> SignInAnonymously()
        {
            string url = string.Format(SignUpUrl, _apiKey);
            string body = "{\"returnSecureToken\":true}";

            var response = await PostAsync<SignUpResponse>(url, body);

            _currentIdToken = response.idToken;
            _currentRefreshToken = response.refreshToken;
            _currentUid = response.localId;
            _tokenExpiryTimeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + (response.expiresIn * 1000);

            return new AuthToken
            {
                IdToken = _currentIdToken,
                RefreshToken = _currentRefreshToken,
                Uid = _currentUid,
                ExpiresInMs = response.expiresIn * 1000
            };
        }

        public async Task<AuthToken> SignInWithGoogle(string googleIdToken)
        {
            if (string.IsNullOrWhiteSpace(googleIdToken))
                throw new ArgumentException("Google ID token is required.", nameof(googleIdToken));

            string url = string.Format(SignInWithIdpUrl, _apiKey);
            string postBody = $"id_token={Uri.EscapeDataString(googleIdToken)}&providerId=google.com";
            string currentIdToken = string.IsNullOrEmpty(_currentIdToken) ? null : await GetIdToken();
            string body;

            if (string.IsNullOrEmpty(currentIdToken))
            {
                body = $"{{\"postBody\":\"{postBody}\",\"requestUri\":\"http://localhost\",\"returnSecureToken\":true,\"returnIdpCredential\":true}}";
            }
            else
            {
                body = $"{{\"postBody\":\"{postBody}\",\"requestUri\":\"http://localhost\",\"idToken\":\"{currentIdToken}\",\"returnSecureToken\":true,\"returnIdpCredential\":true}}";
            }

            var response = await PostAsync<SignInWithIdpResponse>(url, body);

            _currentIdToken = response.idToken;
            _currentRefreshToken = response.refreshToken;
            _currentUid = response.localId;
            _tokenExpiryTimeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + (response.expiresIn * 1000);

            return new AuthToken
            {
                IdToken = _currentIdToken,
                RefreshToken = _currentRefreshToken,
                Uid = _currentUid,
                ExpiresInMs = response.expiresIn * 1000
            };
        }

        public async Task DeleteAccount(string idToken)
        {
            string url = string.Format(DeleteUrl, _apiKey);

            using var request = new UnityWebRequest(url, "POST")
            {
                timeout = 10
            };
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + idToken);

            byte[] body = System.Text.Encoding.UTF8.GetBytes("{}");
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();

            await SendRequest(request);
        }

        private async Task RefreshToken()
        {
            if (string.IsNullOrEmpty(_currentRefreshToken))
            {
                throw new InvalidOperationException("No refresh token available.");
            }

            string url = string.Format(TokenUrl, _apiKey);
            string body = $"grant_type=refresh_token&refresh_token={_currentRefreshToken}";

            using var request = new UnityWebRequest(url, "POST")
            {
                timeout = 10
            };
            request.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");

            byte[] bodyBytes = System.Text.Encoding.UTF8.GetBytes(body);
            request.uploadHandler = new UploadHandlerRaw(bodyBytes);
            request.downloadHandler = new DownloadHandlerBuffer();

            await SendRequest(request);

            string json = request.downloadHandler.text;
            var response = JsonUtility.FromJson<TokenResponse>(json);

            _currentIdToken = response.id_token;
            _currentRefreshToken = response.refresh_token;
            _tokenExpiryTimeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + (response.expires_in * 1000);
        }

        private static async Task<T> PostAsync<T>(string url, string body)
        {
            using var request = new UnityWebRequest(url, "POST")
            {
                timeout = 15
            };
            request.SetRequestHeader("Content-Type", "application/json");

            byte[] bodyBytes = System.Text.Encoding.UTF8.GetBytes(body);
            request.uploadHandler = new UploadHandlerRaw(bodyBytes);
            request.downloadHandler = new DownloadHandlerBuffer();

            await SendRequest(request);

            string json = request.downloadHandler.text;
            return JsonUtility.FromJson<T>(json);
        }

        private static Task SendRequest(UnityWebRequest request)
        {
            return SendRequestInternal(request);
        }

        private static async Task SendRequestInternal(UnityWebRequest request)
        {
            var tcs = new TaskCompletionSource<bool>();
            const int timeoutMs = 30000; // 30초 타임아웃
            var startTime = DateTime.UtcNow;

            var operation = request.SendWebRequest();
            operation.completed += _ =>
            {
                var elapsedTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;

                if (request.result != UnityWebRequest.Result.Success)
                {
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

        [Serializable]
        private class SignUpResponse
        {
            public string idToken;
            public string refreshToken;
            public string localId;
            public long expiresIn;
        }

        [Serializable]
        private class SignInWithIdpResponse
        {
            public string idToken;
            public string refreshToken;
            public string localId;
            public long expiresIn;
            public string providerId;
            public string displayName;
            public bool isNewUser;
        }

        [Serializable]
        private class TokenResponse
        {
            public string id_token;
            public string refresh_token;
            public int expires_in;
            public string token_type;
        }
    }
}
