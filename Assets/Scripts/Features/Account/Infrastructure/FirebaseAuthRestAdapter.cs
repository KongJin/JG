using Features.Account.Application.Ports;
using System;
using System.Runtime.InteropServices;
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
        private const string IdTokenKey = "account.auth.idToken";
        private const string RefreshTokenKey = "account.auth.refreshToken";
        private const string UidKey = "account.auth.uid";
        private const string TokenExpiryKey = "account.auth.expiryUnixMs";

        private readonly string _apiKey;
        private string _currentIdToken;
        private string _currentRefreshToken;
        private string _currentUid;
        private long _tokenExpiryTimeMs;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void AccountStorage_SetItem(string key, string value);

        [DllImport("__Internal")]
        private static extern string AccountStorage_GetItem(string key);

        [DllImport("__Internal")]
        private static extern void AccountStorage_RemoveItem(string key);
#endif

        public FirebaseAuthRestAdapter(string apiKey)
        {
            _apiKey = apiKey;
            RestoreSession();
        }

        public string GetCurrentUid() => _currentUid;

        public void SignOut()
        {
            _currentIdToken = null;
            _currentRefreshToken = null;
            _currentUid = null;
            _tokenExpiryTimeMs = 0;
            ClearPersistedSession();
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
            if (await TryReusePersistedSession())
            {
                Debug.Log($"[FirebaseAuth] Reused persisted anonymous session for uid={DescribeUid(_currentUid)}.");
                return BuildCurrentToken();
            }

            string url = string.Format(SignUpUrl, _apiKey);
            string body = "{\"returnSecureToken\":true}";

            var response = await PostAsync<SignUpResponse>(url, body);

            _currentIdToken = response.idToken;
            _currentRefreshToken = response.refreshToken;
            _currentUid = response.localId;
            _tokenExpiryTimeMs = ComputeExpiryTimeMs(response.expiresIn);
            PersistSession();
            Debug.Log($"[FirebaseAuth] Created new anonymous session for uid={DescribeUid(_currentUid)}.");

            return BuildCurrentToken();
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
            _tokenExpiryTimeMs = ComputeExpiryTimeMs(response.expiresIn);
            PersistSession();

            return BuildCurrentToken();
        }

        public async Task DeleteAccount(string idToken)
        {
            string url = string.Format(DeleteUrl, _apiKey);

            using var request = new UnityWebRequest(url, "POST")
            {
                timeout = 10
            };
            request.SetRequestHeader("Content-Type", "application/json");

            string bodyJson = $"{{\"idToken\":\"{idToken}\"}}";
            byte[] body = System.Text.Encoding.UTF8.GetBytes(bodyJson);
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
            if (!string.IsNullOrWhiteSpace(response.user_id))
                _currentUid = response.user_id;

            _tokenExpiryTimeMs = ComputeExpiryTimeMs(response.expires_in);
            PersistSession();
        }

        private async Task<bool> TryReusePersistedSession()
        {
            if (string.IsNullOrWhiteSpace(_currentRefreshToken) && string.IsNullOrWhiteSpace(_currentIdToken))
            {
                Debug.Log("[FirebaseAuth] No persisted session snapshot found. Anonymous sign-in will create a new account.");
                return false;
            }

            try
            {
                if (string.IsNullOrWhiteSpace(_currentIdToken) || string.IsNullOrWhiteSpace(_currentUid))
                {
                    await RefreshToken();
                    return !string.IsNullOrWhiteSpace(_currentIdToken) && !string.IsNullOrWhiteSpace(_currentUid);
                }

                await GetIdToken();
                return !string.IsNullOrWhiteSpace(_currentIdToken) && !string.IsNullOrWhiteSpace(_currentUid);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FirebaseAuth] Persisted session restore failed. Falling back to new anonymous sign-in. {ex.Message}");
                SignOut();
                return false;
            }
        }

        private AuthToken BuildCurrentToken()
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            long expiresInMs = Math.Max(0L, _tokenExpiryTimeMs - now);

            return new AuthToken
            {
                IdToken = _currentIdToken,
                RefreshToken = _currentRefreshToken,
                Uid = _currentUid,
                ExpiresInMs = expiresInMs
            };
        }

        private static long ComputeExpiryTimeMs(string expiresInSecondsText)
        {
            if (long.TryParse(expiresInSecondsText, out long expiresInSeconds) && expiresInSeconds > 0)
                return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + (expiresInSeconds * 1000L);

            // Firebase Auth 기본 만료는 보통 1시간이므로 파싱 실패 시 보수적으로 55분 후 재갱신되게 둔다.
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + (55L * 60L * 1000L);
        }

        private void RestoreSession()
        {
            _currentIdToken = GetPersistedValue(IdTokenKey);
            _currentRefreshToken = GetPersistedValue(RefreshTokenKey);
            _currentUid = GetPersistedValue(UidKey);

            string expiryText = GetPersistedValue(TokenExpiryKey);
            _tokenExpiryTimeMs = long.TryParse(expiryText, out long parsedExpiry) ? parsedExpiry : 0L;

            Debug.Log(
                $"[FirebaseAuth] RestoreSession uid={DescribeUid(_currentUid)}, hasIdToken={HasValue(_currentIdToken)}, hasRefreshToken={HasValue(_currentRefreshToken)}, expiryMs={_tokenExpiryTimeMs}.");
        }

        private void PersistSession()
        {
            SetPersistedValue(IdTokenKey, _currentIdToken);
            SetPersistedValue(RefreshTokenKey, _currentRefreshToken);
            SetPersistedValue(UidKey, _currentUid);
            SetPersistedValue(TokenExpiryKey, _tokenExpiryTimeMs.ToString());
            PlayerPrefs.Save();

            Debug.Log(
                $"[FirebaseAuth] PersistSession uid={DescribeUid(_currentUid)}, hasIdToken={HasValue(_currentIdToken)}, hasRefreshToken={HasValue(_currentRefreshToken)}, expiryMs={_tokenExpiryTimeMs}.");
        }

        private static void ClearPersistedSession()
        {
            DeletePersistedValue(IdTokenKey);
            DeletePersistedValue(RefreshTokenKey);
            DeletePersistedValue(UidKey);
            DeletePersistedValue(TokenExpiryKey);
            PlayerPrefs.Save();
        }

        private static string GetPersistedValue(string key)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                string webValue = AccountStorage_GetItem(key);
                if (!string.IsNullOrEmpty(webValue))
                    return webValue;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FirebaseAuth] WebGL storage read failed for key={key}. {ex.Message}");
            }
#endif

            return PlayerPrefs.GetString(key, string.Empty);
        }

        private static void SetPersistedValue(string key, string value)
        {
            string safeValue = value ?? string.Empty;

            PlayerPrefs.SetString(key, safeValue);

#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                AccountStorage_SetItem(key, safeValue);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FirebaseAuth] WebGL storage write failed for key={key}. {ex.Message}");
            }
#endif
        }

        private static void DeletePersistedValue(string key)
        {
            PlayerPrefs.DeleteKey(key);

#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                AccountStorage_RemoveItem(key);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FirebaseAuth] WebGL storage delete failed for key={key}. {ex.Message}");
            }
#endif
        }

        private static bool HasValue(string value)
        {
            return !string.IsNullOrWhiteSpace(value);
        }

        private static string DescribeUid(string uid)
        {
            return string.IsNullOrWhiteSpace(uid) ? "<empty>" : uid;
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
            public string expiresIn;
        }

        [Serializable]
        private class SignInWithIdpResponse
        {
            public string idToken;
            public string refreshToken;
            public string localId;
            public string expiresIn;
            public string providerId;
            public string displayName;
            public bool isNewUser;
        }

        [Serializable]
        private class TokenResponse
        {
            public string id_token;
            public string refresh_token;
            public string expires_in;
            public string token_type;
            public string user_id;
        }
    }
}
