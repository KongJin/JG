using Features.Account.Application.Ports;
using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Features.Account.Infrastructure
{
    /// <summary>
    /// Firebase Auth REST API 어댑터.
    /// UnityWebRequest 사용 (WebGL 호환).
    /// </summary>
    public sealed class FirebaseAuthRestAdapter : IAuthPort, IAccountSessionAccess
    {
        private const string SignUpUrl = "https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={0}";
        private const string SignInWithIdpUrl = "https://identitytoolkit.googleapis.com/v1/accounts:signInWithIdp?key={0}";
        private const string TokenUrl = "https://securetoken.googleapis.com/v1/token?key={0}";
        private const string DeleteUrl = "https://identitytoolkit.googleapis.com/v1/accounts:delete?key={0}";

        private readonly string _apiKey;
        private readonly FirebaseAuthHttpClient _httpClient;
        private readonly IAccountSessionStore _sessionStore;
        private string _currentIdToken;
        private string _currentRefreshToken;
        private string _currentUid;
        private long _tokenExpiryTimeMs;

        public FirebaseAuthRestAdapter(string apiKey)
        {
            _apiKey = apiKey;
            _httpClient = new FirebaseAuthHttpClient();
            _sessionStore = new PlayerPrefsAccountSessionStore();
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

            var response = await _httpClient.PostJsonAsync<SignUpResponse>(url, body);
            ApplySnapshot(FirebaseAuthResponseMapper.FromSignUpResponse(response));
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

            var response = await _httpClient.PostJsonAsync<SignInWithIdpResponse>(url, body);
            ApplySnapshot(FirebaseAuthResponseMapper.FromIdpResponse(response));

            return BuildCurrentToken();
        }

        public async Task DeleteAccount(string idToken)
        {
            string url = string.Format(DeleteUrl, _apiKey);

            string bodyJson = $"{{\"idToken\":\"{idToken}\"}}";
            await _httpClient.PostJsonAsync(url, bodyJson);
        }

        private async Task RefreshToken()
        {
            if (string.IsNullOrEmpty(_currentRefreshToken))
            {
                throw new InvalidOperationException("No refresh token available.");
            }

            string url = string.Format(TokenUrl, _apiKey);
            string body = $"grant_type=refresh_token&refresh_token={_currentRefreshToken}";

            var response = await _httpClient.PostFormAsync<TokenResponse>(url, body);
            ApplySnapshot(FirebaseAuthResponseMapper.FromTokenResponse(response, _currentUid));
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

        private void RestoreSession()
        {
            var snapshot = _sessionStore.Load();
            _currentIdToken = snapshot.IdToken;
            _currentRefreshToken = snapshot.RefreshToken;
            _currentUid = snapshot.Uid;
            _tokenExpiryTimeMs = snapshot.ExpiryUnixMs;

            Debug.Log(
                $"[FirebaseAuth] RestoreSession uid={DescribeUid(_currentUid)}, hasIdToken={HasValue(_currentIdToken)}, hasRefreshToken={HasValue(_currentRefreshToken)}, expiryMs={_tokenExpiryTimeMs}.");
        }

        private void PersistSession()
        {
            _sessionStore.Save(new AccountSessionSnapshot
            {
                IdToken = _currentIdToken,
                RefreshToken = _currentRefreshToken,
                Uid = _currentUid,
                ExpiryUnixMs = _tokenExpiryTimeMs
            });

            Debug.Log(
                $"[FirebaseAuth] PersistSession uid={DescribeUid(_currentUid)}, hasIdToken={HasValue(_currentIdToken)}, hasRefreshToken={HasValue(_currentRefreshToken)}, expiryMs={_tokenExpiryTimeMs}.");
        }

        private void ApplySnapshot(AccountSessionSnapshot snapshot)
        {
            _currentIdToken = snapshot.IdToken;
            _currentRefreshToken = snapshot.RefreshToken;
            _currentUid = snapshot.Uid;
            _tokenExpiryTimeMs = snapshot.ExpiryUnixMs;
            PersistSession();
        }

        private void ClearPersistedSession()
        {
            _sessionStore.Clear();
        }

        private static bool HasValue(string value)
        {
            return !string.IsNullOrWhiteSpace(value);
        }

        private static string DescribeUid(string uid)
        {
            return string.IsNullOrWhiteSpace(uid) ? "<empty>" : uid;
        }
    }
}
