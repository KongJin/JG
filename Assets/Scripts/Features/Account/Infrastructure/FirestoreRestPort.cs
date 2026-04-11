using Features.Account.Application.Ports;
using Features.Account.Domain;
using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Features.Account.Infrastructure
{
    /// <summary>
    /// Firestore REST API 포트 구현.
    /// UnityWebRequest 사용 (WebGL 호환).
    /// </summary>
    public sealed class FirestoreRestPort : IAccountDataPort
    {
        private const string DocumentUrl = "https://firestore.googleapis.com/v1/projects/{0}/databases/(default)/documents/accounts/{1}/{2}/{3}?key={4}";

        private readonly string _apiKey;
        private readonly string _projectId;

        public FirestoreRestPort(string apiKey, string projectId)
        {
            _apiKey = apiKey;
            _projectId = projectId;
        }

        public async Task SaveProfile(AccountProfile account, string idToken)
        {
            await WriteDocument("profile", "profile", new
            {
                uid = account.uid,
                displayName = account.displayName,
                authType = account.authType,
                createdAtUnixMs = account.createdAtUnixMs
            }, idToken);
        }

        public async Task<AccountProfile> LoadProfile(string uid, string idToken)
        {
            var doc = await ReadDocument("profile", "profile", idToken);
            if (doc == null) return null;

            return new AccountProfile
            {
                uid = GetFieldString(doc, "uid"),
                displayName = GetFieldString(doc, "displayName"),
                authType = GetFieldString(doc, "authType"),
                createdAtUnixMs = GetFieldLong(doc, "createdAtUnixMs")
            };
        }

        public async Task SaveStats(PlayerStats stats, string uid, string idToken)
        {
            await WriteDocument("stats", "stats", new
            {
                totalPlayTimeSeconds = stats.totalPlayTimeSeconds,
                totalGames = stats.totalGames,
                totalVictories = stats.totalVictories,
                totalDefeats = stats.totalDefeats,
                highestWave = stats.highestWave,
                totalSummons = stats.totalSummons,
                totalUnitKills = stats.totalUnitKills
            }, idToken);
        }

        public async Task<PlayerStats> LoadStats(string uid, string idToken)
        {
            var doc = await ReadDocument("stats", "stats", idToken);
            if (doc == null) return null;

            return new PlayerStats
            {
                totalPlayTimeSeconds = GetFieldFloat(doc, "totalPlayTimeSeconds"),
                totalGames = GetFieldInt(doc, "totalGames"),
                totalVictories = GetFieldInt(doc, "totalVictories"),
                totalDefeats = GetFieldInt(doc, "totalDefeats"),
                highestWave = GetFieldInt(doc, "highestWave"),
                totalSummons = GetFieldInt(doc, "totalSummons"),
                totalUnitKills = GetFieldInt(doc, "totalUnitKills")
            };
        }

        public async Task SaveGarage(object roster, string uid, string idToken)
        {
            string json = JsonUtility.ToJson(new RosterWrapper { roster = roster });
            await WriteRawDocument("garage", "roster", json, idToken);
        }

        public async Task<object> LoadGarage(string uid, string idToken)
        {
            var doc = await ReadDocument("garage", "roster", idToken);
            if (doc == null) return null;

            string jsonStr = GetFieldString(doc, "json");
            if (string.IsNullOrEmpty(jsonStr)) return null;

            try
            {
                var wrapper = JsonUtility.FromJson<RosterWrapper>(jsonStr);
                return wrapper?.roster;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Firestore] Failed to parse garage roster: {ex.Message}");
                return null;
            }
        }

        public async Task SaveSettings(UserSettings settings, string uid, string idToken)
        {
            await WriteDocument("settings", "settings", new
            {
                masterVolume = settings.masterVolume,
                bgmVolume = settings.bgmVolume,
                sfxVolume = settings.sfxVolume,
                language = settings.language
            }, idToken);
        }

        public async Task<UserSettings> LoadSettings(string uid, string idToken)
        {
            var doc = await ReadDocument("settings", "settings", idToken);
            if (doc == null) return null;

            return new UserSettings
            {
                masterVolume = GetFieldFloat(doc, "masterVolume"),
                bgmVolume = GetFieldFloat(doc, "bgmVolume"),
                sfxVolume = GetFieldFloat(doc, "sfxVolume"),
                language = GetFieldString(doc, "language")
            };
        }

        public async Task DeleteAccount(string uid, string idToken)
        {
            await DeleteDocument("profile", "profile", idToken);
            await DeleteDocument("stats", "stats", idToken);
            await DeleteDocument("settings", "settings", idToken);
            await DeleteDocument("garage", "roster", idToken);
        }

        private string BuildDocumentUrl(string collectionId, string documentId)
        {
            return string.Format(DocumentUrl, _projectId, "{uid_placeholder}", collectionId, documentId, _apiKey);
        }

        private async Task<FirestoreDocument> ReadDocument(string collectionId, string documentId, string idToken)
        {
            string url = string.Format(DocumentUrl, _projectId, "{uid_placeholder}", collectionId, documentId, _apiKey);
            // Note: uid_placeholder is not used in the URL path for single client - the auth token determines access

            using var request = new UnityWebRequest(url, "GET")
            {
                timeout = 15
            };
            request.SetRequestHeader("Authorization", "Bearer " + idToken);
            request.downloadHandler = new DownloadHandlerBuffer();

            var result = await SendRequestSafe(request);
            if (!result.success) return null;

            string json = request.downloadHandler.text;
            return JsonUtility.FromJson<FirestoreDocument>(json);
        }

        private async Task WriteDocument(string collectionId, string documentId, object data, string idToken)
        {
            string url = string.Format(DocumentUrl, _projectId, "{uid_placeholder}", collectionId, documentId, _apiKey);
            string json = BuildFieldsJson(data);

            using var request = new UnityWebRequest(url, "PATCH")
            {
                timeout = 15
            };
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + idToken);

            byte[] body = System.Text.Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();

            await SendRequest(request);
        }

        private async Task WriteRawDocument(string collectionId, string documentId, string rawJson, string idToken)
        {
            string url = string.Format(DocumentUrl, _projectId, "{uid_placeholder}", collectionId, documentId, _apiKey);
            string json = $"{{\"fields\":{{\"json\":{{\"stringValue\":\"{rawJson}\"}}}}}}";

            using var request = new UnityWebRequest(url, "PATCH")
            {
                timeout = 15
            };
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + idToken);

            byte[] body = System.Text.Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();

            await SendRequest(request);
        }

        private async Task DeleteDocument(string collectionId, string documentId, string idToken)
        {
            string url = string.Format(DocumentUrl, _projectId, "{uid_placeholder}", collectionId, documentId, _apiKey);

            using var request = new UnityWebRequest(url, "DELETE")
            {
                timeout = 15
            };
            request.SetRequestHeader("Authorization", "Bearer " + idToken);
            request.downloadHandler = new DownloadHandlerBuffer();

            await SendRequestSafe(request);
        }

        private static string BuildFieldsJson(object data)
        {
            // Simple reflection-based field builder
            var json = "{\"fields\":{";
            var type = data.GetType();
            var fields = type.GetFields();
            bool first = true;

            foreach (var field in fields)
            {
                if (!first) json += ",";
                first = false;

                var value = field.GetValue(data);
                string fieldType = GetFirestoreType(value);
                string fieldValue = FormatFirestoreValue(value);
                json += $"\"{field.Name}\":{{{fieldType}:{fieldValue}}}";
            }

            json += "}}";
            return json;
        }

        private static string GetFirestoreType(object value)
        {
            if (value is string) return "\"stringValue\"";
            if (value is int) return "\"integerValue\"";
            if (value is long) return "\"integerValue\"";
            if (value is float) return "\"doubleValue\"";
            if (value is double) return "\"doubleValue\"";
            return "\"stringValue\"";
        }

        private static string FormatFirestoreValue(object value)
        {
            if (value is string s) return $"\"{s}\"";
            if (value is int i) return i.ToString();
            if (value is long l) return l.ToString();
            if (value is float f) return f.ToString("G9");
            if (value is double d) return d.ToString("G9");
            return $"\"{value}\"";
        }

        private static string GetFieldString(FirestoreDocument doc, string fieldName)
        {
            if (doc?.fields == null) return null;
            foreach (var f in doc.fields)
            {
                if (f.key == fieldName)
                    return f.value.stringValue;
            }
            return null;
        }

        private static int GetFieldInt(FirestoreDocument doc, string fieldName)
        {
            if (doc?.fields == null) return 0;
            foreach (var f in doc.fields)
            {
                if (f.key == fieldName)
                {
                    if (int.TryParse(f.value.integerValue, out int v)) return v;
                    if (long.TryParse(f.value.integerValue, out long lv)) return (int)lv;
                }
            }
            return 0;
        }

        private static long GetFieldLong(FirestoreDocument doc, string fieldName)
        {
            if (doc?.fields == null) return 0;
            foreach (var f in doc.fields)
            {
                if (f.key == fieldName)
                {
                    if (long.TryParse(f.value.integerValue, out long v)) return v;
                }
            }
            return 0;
        }

        private static float GetFieldFloat(FirestoreDocument doc, string fieldName)
        {
            if (doc?.fields == null) return 0f;
            foreach (var f in doc.fields)
            {
                if (f.key == fieldName)
                {
                    if (float.TryParse(f.value.doubleValue, out float v)) return v;
                    if (int.TryParse(f.value.integerValue, out int iv)) return iv;
                }
            }
            return 0f;
        }

        private static Task<SendResult> SendRequestSafe(UnityWebRequest request)
        {
            var tcs = new TaskCompletionSource<SendResult>();

            var operation = request.SendWebRequest();
            operation.completed += _ =>
            {
                if (request.result != UnityWebRequest.Result.Success)
                {
                    if (request.responseCode == 404)
                    {
                        tcs.SetResult(new SendResult { success = false, notFound = true });
                    }
                    else
                    {
                        Debug.LogError($"[Firestore] Request failed: {request.error}");
                        tcs.SetResult(new SendResult { success = false });
                    }
                }
                else
                {
                    tcs.SetResult(new SendResult { success = true });
                }
            };

            return tcs.Task;
        }

        private static Task SendRequest(UnityWebRequest request)
        {
            var tcs = new TaskCompletionSource<bool>();

            var operation = request.SendWebRequest();
            operation.completed += _ =>
            {
                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[Firestore] Request failed: {request.error}");
                    tcs.SetException(new Exception(request.error));
                }
                else
                {
                    tcs.SetResult(true);
                }
            };

            return tcs.Task;
        }
    }

    [Serializable]
    public sealed class FirestoreDocument
    {
        public FirestoreField[] fields;
        public string name;
    }

    [Serializable]
    public sealed class FirestoreField
    {
        public string key;
        public FirestoreValue value;
    }

    [Serializable]
    public sealed class FirestoreValue
    {
        public string stringValue;
        public string integerValue;
        public string doubleValue;
    }

    [Serializable]
    public sealed class RosterWrapper
    {
        public object roster;
    }

    public struct SendResult
    {
        public bool success;
        public bool notFound;
    }
}

