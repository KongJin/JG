using Features.Account.Application.Ports;
using Features.Account.Domain;
using System;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
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
            await WriteDocument(account.uid, "profile", "profile", new
            {
                uid = account.uid,
                displayName = account.displayName,
                authType = account.authType,
                createdAtUnixMs = account.createdAtUnixMs
            }, idToken);
        }

        public async Task<AccountProfile> LoadProfile(string uid, string idToken)
        {
            string json = await ReadDocument(uid, "profile", "profile", idToken);
            if (string.IsNullOrEmpty(json))
                return null;

            return new AccountProfile
            {
                uid = GetFieldString(json, "uid"),
                displayName = GetFieldString(json, "displayName"),
                authType = GetFieldString(json, "authType"),
                createdAtUnixMs = GetFieldLong(json, "createdAtUnixMs")
            };
        }

        public async Task SaveStats(PlayerStats stats, string uid, string idToken)
        {
            await WriteDocument(uid, "stats", "stats", new
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
            string json = await ReadDocument(uid, "stats", "stats", idToken);
            if (string.IsNullOrEmpty(json))
                return null;

            return new PlayerStats
            {
                totalPlayTimeSeconds = GetFieldFloat(json, "totalPlayTimeSeconds"),
                totalGames = GetFieldInt(json, "totalGames"),
                totalVictories = GetFieldInt(json, "totalVictories"),
                totalDefeats = GetFieldInt(json, "totalDefeats"),
                highestWave = GetFieldInt(json, "highestWave"),
                totalSummons = GetFieldInt(json, "totalSummons"),
                totalUnitKills = GetFieldInt(json, "totalUnitKills")
            };
        }

        public async Task SaveGarage(object roster, string uid, string idToken)
        {
            string json = JsonUtility.ToJson(new RosterWrapper { roster = roster });
            await WriteRawDocument(uid, "garage", "roster", json, idToken);
        }

        public async Task<object> LoadGarage(string uid, string idToken)
        {
            string json = await ReadDocument(uid, "garage", "roster", idToken);
            if (string.IsNullOrEmpty(json))
                return null;

            string jsonStr = GetFieldString(json, "json");
            if (string.IsNullOrEmpty(jsonStr))
                return null;

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
            await WriteDocument(uid, "settings", "settings", new
            {
                masterVolume = settings.masterVolume,
                bgmVolume = settings.bgmVolume,
                sfxVolume = settings.sfxVolume,
                language = settings.language
            }, idToken);
        }

        public async Task<UserSettings> LoadSettings(string uid, string idToken)
        {
            string json = await ReadDocument(uid, "settings", "settings", idToken);
            if (string.IsNullOrEmpty(json))
                return null;

            return new UserSettings
            {
                masterVolume = GetFieldFloat(json, "masterVolume"),
                bgmVolume = GetFieldFloat(json, "bgmVolume"),
                sfxVolume = GetFieldFloat(json, "sfxVolume"),
                language = GetFieldString(json, "language")
            };
        }

        public async Task DeleteAccount(string uid, string idToken)
        {
            await DeleteDocument(uid, "profile", "profile", idToken);
            await DeleteDocument(uid, "stats", "stats", idToken);
            await DeleteDocument(uid, "settings", "settings", idToken);
            await DeleteDocument(uid, "garage", "roster", idToken);
        }

        private string BuildDocumentUrl(string uid, string collectionId, string documentId)
        {
            return string.Format(
                DocumentUrl,
                _projectId,
                Uri.EscapeDataString(uid),
                Uri.EscapeDataString(collectionId),
                Uri.EscapeDataString(documentId),
                _apiKey);
        }

        private async Task<string> ReadDocument(string uid, string collectionId, string documentId, string idToken)
        {
            string url = BuildDocumentUrl(uid, collectionId, documentId);

            using var request = new UnityWebRequest(url, "GET")
            {
                timeout = 15
            };
            request.SetRequestHeader("Authorization", "Bearer " + idToken);
            request.downloadHandler = new DownloadHandlerBuffer();

            var result = await SendRequestSafe(request);
            if (!result.success)
                return null;

            return request.downloadHandler.text;
        }

        private async Task WriteDocument(string uid, string collectionId, string documentId, object data, string idToken)
        {
            string url = BuildDocumentUrl(uid, collectionId, documentId);
            string json = BuildFieldsJson(data);

            using var request = new UnityWebRequest(url, "PATCH")
            {
                timeout = 15
            };
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + idToken);
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            request.downloadHandler = new DownloadHandlerBuffer();

            await SendRequest(request);
        }

        private async Task WriteRawDocument(string uid, string collectionId, string documentId, string rawJson, string idToken)
        {
            string url = BuildDocumentUrl(uid, collectionId, documentId);
            string json = $"{{\"fields\":{{\"json\":{{\"stringValue\":\"{EscapeJsonString(rawJson)}\"}}}}}}";

            using var request = new UnityWebRequest(url, "PATCH")
            {
                timeout = 15
            };
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + idToken);
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            request.downloadHandler = new DownloadHandlerBuffer();

            await SendRequest(request);
        }

        private async Task DeleteDocument(string uid, string collectionId, string documentId, string idToken)
        {
            string url = BuildDocumentUrl(uid, collectionId, documentId);

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
            var members = data.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);
            bool useProperties = members.Length == 0;

            var builder = new StringBuilder();
            builder.Append("{\"fields\":{");
            bool first = true;

            if (useProperties)
            {
                var properties = data.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
                for (int i = 0; i < properties.Length; i++)
                {
                    var property = properties[i];
                    if (!property.CanRead || property.GetIndexParameters().Length > 0)
                        continue;

                    AppendField(builder, property.Name, property.GetValue(data), ref first);
                }
            }
            else
            {
                for (int i = 0; i < members.Length; i++)
                    AppendField(builder, members[i].Name, members[i].GetValue(data), ref first);
            }

            builder.Append("}}");
            return builder.ToString();
        }

        private static void AppendField(StringBuilder builder, string name, object value, ref bool first)
        {
            if (!first)
                builder.Append(',');

            first = false;
            builder.Append('"');
            builder.Append(name);
            builder.Append("\":{");
            builder.Append(GetFirestoreType(value));
            builder.Append(':');
            builder.Append(FormatFirestoreValue(value));
            builder.Append('}');
        }

        private static string GetFirestoreType(object value)
        {
            if (value is string) return "\"stringValue\"";
            if (value is bool) return "\"booleanValue\"";
            if (value is int) return "\"integerValue\"";
            if (value is long) return "\"integerValue\"";
            if (value is float) return "\"doubleValue\"";
            if (value is double) return "\"doubleValue\"";
            return "\"stringValue\"";
        }

        private static string FormatFirestoreValue(object value)
        {
            switch (value)
            {
                case null:
                    return "\"\"";
                case string s:
                    return $"\"{EscapeJsonString(s)}\"";
                case bool b:
                    return b ? "true" : "false";
                case int i:
                    return i.ToString(CultureInfo.InvariantCulture);
                case long l:
                    return l.ToString(CultureInfo.InvariantCulture);
                case float f:
                    return f.ToString("G9", CultureInfo.InvariantCulture);
                case double d:
                    return d.ToString("G17", CultureInfo.InvariantCulture);
                default:
                    return $"\"{EscapeJsonString(value.ToString())}\"";
            }
        }

        private static string GetFieldString(string json, string fieldName)
        {
            return FindFieldValue(json, fieldName, "stringValue", unescapeString: true);
        }

        private static int GetFieldInt(string json, string fieldName)
        {
            string value = FindFieldValue(json, fieldName, "integerValue", unescapeString: false);
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result) ? result : 0;
        }

        private static long GetFieldLong(string json, string fieldName)
        {
            string value = FindFieldValue(json, fieldName, "integerValue", unescapeString: false);
            return long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long result) ? result : 0L;
        }

        private static float GetFieldFloat(string json, string fieldName)
        {
            string doubleValue = FindFieldValue(json, fieldName, "doubleValue", unescapeString: false);
            if (float.TryParse(doubleValue, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsedDouble))
                return parsedDouble;

            string integerValue = FindFieldValue(json, fieldName, "integerValue", unescapeString: false);
            return float.TryParse(integerValue, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsedInt)
                ? parsedInt
                : 0f;
        }

        private static string FindFieldValue(string json, string fieldName, string firestoreValueType, bool unescapeString)
        {
            if (string.IsNullOrEmpty(json))
                return null;

            string pattern =
                $"\"{Regex.Escape(fieldName)}\"\\s*:\\s*\\{{\\s*\"{Regex.Escape(firestoreValueType)}\"\\s*:\\s*(?<value>\"(?:\\\\.|[^\"\\\\])*\"|-?[0-9]+(?:\\.[0-9]+)?|true|false)";

            var match = Regex.Match(json, pattern);
            if (!match.Success)
                return null;

            string rawValue = match.Groups["value"].Value;
            if (rawValue.Length >= 2 && rawValue[0] == '"' && rawValue[rawValue.Length - 1] == '"')
                rawValue = rawValue.Substring(1, rawValue.Length - 2);

            return unescapeString ? UnescapeJsonString(rawValue) : rawValue;
        }

        private static Task<SendResult> SendRequestSafe(UnityWebRequest request)
        {
            return SendRequestSafeInternal(request);
        }

        private static Task SendRequest(UnityWebRequest request)
        {
            return SendRequestInternal(request);
        }

        private static async Task<SendResult> SendRequestSafeInternal(UnityWebRequest request)
        {
            const int timeoutMs = 30000; // 30초 타임아웃
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

        private static async Task SendRequestInternal(UnityWebRequest request)
        {
            const int timeoutMs = 30000; // 30초 타임아웃
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

        private static string EscapeJsonString(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            return value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n")
                .Replace("\t", "\\t");
        }

        private static string UnescapeJsonString(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            return value
                .Replace("\\\"", "\"")
                .Replace("\\n", "\n")
                .Replace("\\r", "\r")
                .Replace("\\t", "\t")
                .Replace("\\\\", "\\");
        }
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
        public string error;
    }
}
