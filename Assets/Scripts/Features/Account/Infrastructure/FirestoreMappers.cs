using Features.Account.Domain;
using Features.Garage.Domain;
using Features.Player.Domain;
using System;
using UnityEngine;

namespace Features.Account.Infrastructure
{
    internal static class FirestoreAccountMapper
    {
        public static object ToProfileFields(AccountProfile account)
        {
            return new
            {
                uid = account.uid,
                displayName = account.displayName,
                authType = account.authType,
                createdAtUnixMs = account.createdAtUnixMs,
                lastNicknameChangeUnixMs = account.lastNicknameChangeUnixMs
            };
        }

        public static AccountProfile ToProfile(string json)
        {
            return new AccountProfile
            {
                uid = FirestoreFieldReader.GetString(json, "uid"),
                displayName = FirestoreFieldReader.GetString(json, "displayName"),
                authType = FirestoreFieldReader.GetString(json, "authType"),
                createdAtUnixMs = FirestoreFieldReader.GetLong(json, "createdAtUnixMs"),
                lastNicknameChangeUnixMs = FirestoreFieldReader.GetLong(json, "lastNicknameChangeUnixMs")
            };
        }

        public static object ToStatsFields(PlayerStats stats)
        {
            return new
            {
                totalPlayTimeSeconds = stats.totalPlayTimeSeconds,
                totalGames = stats.totalGames,
                totalVictories = stats.totalVictories,
                totalDefeats = stats.totalDefeats,
                highestWave = stats.highestWave,
                totalSummons = stats.totalSummons,
                totalUnitKills = stats.totalUnitKills
            };
        }

        public static PlayerStats ToStats(string json)
        {
            return new PlayerStats
            {
                totalPlayTimeSeconds = FirestoreFieldReader.GetFloat(json, "totalPlayTimeSeconds"),
                totalGames = FirestoreFieldReader.GetInt(json, "totalGames"),
                totalVictories = FirestoreFieldReader.GetInt(json, "totalVictories"),
                totalDefeats = FirestoreFieldReader.GetInt(json, "totalDefeats"),
                highestWave = FirestoreFieldReader.GetInt(json, "highestWave"),
                totalSummons = FirestoreFieldReader.GetInt(json, "totalSummons"),
                totalUnitKills = FirestoreFieldReader.GetInt(json, "totalUnitKills")
            };
        }

        public static object ToSettingsFields(UserSettings settings)
        {
            return new
            {
                masterVolume = settings.masterVolume,
                bgmVolume = settings.bgmVolume,
                sfxVolume = settings.sfxVolume,
                language = settings.language
            };
        }

        public static UserSettings ToSettings(string json)
        {
            return new UserSettings
            {
                masterVolume = FirestoreFieldReader.GetFloat(json, "masterVolume"),
                bgmVolume = FirestoreFieldReader.GetFloat(json, "bgmVolume"),
                sfxVolume = FirestoreFieldReader.GetFloat(json, "sfxVolume"),
                language = FirestoreFieldReader.GetString(json, "language")
            };
        }
    }

    internal static class FirestoreGarageMapper
    {
        public static string ToJson(GarageRoster roster)
        {
            return JsonUtility.ToJson(new RosterWrapper { roster = roster });
        }

        public static GarageRoster FromDocument(string json)
        {
            string jsonStr = FirestoreFieldReader.GetString(json, "json");
            if (string.IsNullOrEmpty(jsonStr))
                return null;

            try
            {
                var wrapper = JsonUtility.FromJson<RosterWrapper>(jsonStr);
                wrapper?.roster?.Normalize();
                return wrapper?.roster;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Firestore] Failed to parse garage roster: {ex.Message}");
                return null;
            }
        }
    }

    [Serializable]
    public sealed class RosterWrapper
    {
        public GarageRoster roster;
    }

    internal static class FirestoreOperationRecordMapper
    {
        public static string ToJson(RecentOperationRecords records)
        {
            records ??= new RecentOperationRecords();
            records.Normalize();
            return JsonUtility.ToJson(new OperationRecordWrapper { records = records });
        }

        public static RecentOperationRecords FromDocument(string json)
        {
            string jsonStr = FirestoreFieldReader.GetString(json, "json");
            if (string.IsNullOrEmpty(jsonStr))
                return null;

            try
            {
                var wrapper = JsonUtility.FromJson<OperationRecordWrapper>(jsonStr);
                var records = wrapper?.records ?? new RecentOperationRecords();
                records.Normalize();
                return records;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Firestore] Failed to parse operation records: {ex.Message}");
                return null;
            }
        }
    }

    [Serializable]
    internal sealed class OperationRecordWrapper
    {
        public RecentOperationRecords records;
    }
}
