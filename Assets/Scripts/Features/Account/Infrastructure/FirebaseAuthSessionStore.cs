using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Features.Account.Infrastructure
{
    internal sealed class AccountSessionSnapshot
    {
        public string IdToken = string.Empty;
        public string RefreshToken = string.Empty;
        public string Uid = string.Empty;
        public long ExpiryUnixMs;
    }

    internal interface IAccountSessionStore
    {
        AccountSessionSnapshot Load();
        void Save(AccountSessionSnapshot snapshot);
        void Clear();
    }

    internal sealed class PlayerPrefsAccountSessionStore : IAccountSessionStore
    {
        private const string IdTokenKey = "account.auth.idToken";
        private const string RefreshTokenKey = "account.auth.refreshToken";
        private const string UidKey = "account.auth.uid";
        private const string TokenExpiryKey = "account.auth.expiryUnixMs";

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void AccountStorage_SetItem(string key, string value);

        [DllImport("__Internal")]
        private static extern string AccountStorage_GetItem(string key);

        [DllImport("__Internal")]
        private static extern void AccountStorage_RemoveItem(string key);
#endif

        public AccountSessionSnapshot Load()
        {
            return new AccountSessionSnapshot
            {
                IdToken = GetPersistedValue(IdTokenKey),
                RefreshToken = GetPersistedValue(RefreshTokenKey),
                Uid = GetPersistedValue(UidKey),
                ExpiryUnixMs = long.TryParse(GetPersistedValue(TokenExpiryKey), out long expiry) ? expiry : 0L
            };
        }

        public void Save(AccountSessionSnapshot snapshot)
        {
            SetPersistedValue(IdTokenKey, snapshot.IdToken);
            SetPersistedValue(RefreshTokenKey, snapshot.RefreshToken);
            SetPersistedValue(UidKey, snapshot.Uid);
            SetPersistedValue(TokenExpiryKey, snapshot.ExpiryUnixMs.ToString());
            PlayerPrefs.Save();
        }

        public void Clear()
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
    }
}
