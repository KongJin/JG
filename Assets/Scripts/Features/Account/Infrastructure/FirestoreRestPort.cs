using Features.Account.Application.Ports;
using Features.Account.Domain;
using Features.Garage.Application;
using Features.Garage.Domain;
using Features.Player.Application.Ports;
using Features.Player.Domain;
using System;
using System.Threading.Tasks;

namespace Features.Account.Infrastructure
{
    /// <summary>
    /// Firestore REST API 포트 구현.
    /// UnityWebRequest 사용 (WebGL 호환).
    /// </summary>
    public sealed class FirestoreRestPort :
        IAccountDataPort,
        SaveRosterUseCase.ICloudGaragePort,
        InitializeGarageUseCase.ICloudGarageLoadPort,
        IOperationRecordCloudPort
    {
        private readonly FirestoreDocumentClient _documentClient;
        private readonly IAccountSessionAccess _sessionAccess;

        public FirestoreRestPort(string apiKey, string projectId, IAccountSessionAccess sessionAccess)
        {
            _documentClient = new FirestoreDocumentClient(apiKey, projectId);
            _sessionAccess = sessionAccess ?? throw new ArgumentNullException(nameof(sessionAccess));
        }

        public async Task SaveProfile(AccountProfile account, string idToken)
        {
            await WriteDocument(account.uid, "profile", "profile", FirestoreAccountMapper.ToProfileFields(account), idToken);
        }

        public async Task<AccountProfile> LoadProfile(string uid, string idToken)
        {
            string json = await ReadDocument(uid, "profile", "profile", idToken);
            if (string.IsNullOrEmpty(json))
                return null;

            return FirestoreAccountMapper.ToProfile(json);
        }

        public async Task SaveStats(PlayerStats stats, string uid, string idToken)
        {
            await WriteDocument(uid, "stats", "stats", FirestoreAccountMapper.ToStatsFields(stats), idToken);
        }

        public async Task<PlayerStats> LoadStats(string uid, string idToken)
        {
            string json = await ReadDocument(uid, "stats", "stats", idToken);
            if (string.IsNullOrEmpty(json))
                return null;

            return FirestoreAccountMapper.ToStats(json);
        }

        public async Task SaveGarage(GarageRoster roster, string uid, string idToken)
        {
            var normalizedRoster = roster ?? new GarageRoster();
            normalizedRoster.Normalize();

            await WriteRawDocument(uid, "garage", "roster", FirestoreGarageMapper.ToJson(normalizedRoster), idToken);
        }

        public async Task<GarageRoster> LoadGarage(string uid, string idToken)
        {
            string json = await ReadDocument(uid, "garage", "roster", idToken);
            if (string.IsNullOrEmpty(json))
                return null;

            return FirestoreGarageMapper.FromDocument(json);
        }

        public async Task SaveOperationRecords(RecentOperationRecords records, string uid, string idToken)
        {
            var normalizedRecords = records ?? new RecentOperationRecords();
            normalizedRecords.Normalize();

            await WriteRawDocument(
                uid,
                "operations",
                "recent",
                FirestoreOperationRecordMapper.ToJson(normalizedRecords),
                idToken);
        }

        public async Task<RecentOperationRecords> LoadOperationRecords(string uid, string idToken)
        {
            string json = await ReadDocument(uid, "operations", "recent", idToken);
            if (string.IsNullOrEmpty(json))
                return null;

            return FirestoreOperationRecordMapper.FromDocument(json);
        }

        public async Task SaveGarageAsync(GarageRoster roster)
        {
            string uid = _sessionAccess.GetCurrentUid();
            string idToken = await _sessionAccess.GetIdToken();

            if (string.IsNullOrWhiteSpace(uid) || string.IsNullOrWhiteSpace(idToken))
                throw new InvalidOperationException("Account session is not ready for garage save.");

            await SaveGarage(roster, uid, idToken);
        }

        public async Task<GarageRoster> LoadGarageAsync()
        {
            string uid = _sessionAccess.GetCurrentUid();
            string idToken = await _sessionAccess.GetIdToken();

            if (string.IsNullOrWhiteSpace(uid) || string.IsNullOrWhiteSpace(idToken))
                return null;

            return await LoadGarage(uid, idToken);
        }

        public async Task SaveOperationRecordsAsync(RecentOperationRecords records)
        {
            string uid = _sessionAccess.GetCurrentUid();
            string idToken = await _sessionAccess.GetIdToken();

            if (string.IsNullOrWhiteSpace(uid) || string.IsNullOrWhiteSpace(idToken))
                throw new InvalidOperationException("Account session is not ready for operation record save.");

            await SaveOperationRecords(records, uid, idToken);
        }

        public async Task<RecentOperationRecords> LoadOperationRecordsAsync()
        {
            string uid = _sessionAccess.GetCurrentUid();
            string idToken = await _sessionAccess.GetIdToken();

            if (string.IsNullOrWhiteSpace(uid) || string.IsNullOrWhiteSpace(idToken))
                return null;

            return await LoadOperationRecords(uid, idToken);
        }

        public async Task SaveSettings(UserSettings settings, string uid, string idToken)
        {
            await WriteDocument(uid, "settings", "settings", FirestoreAccountMapper.ToSettingsFields(settings), idToken);
        }

        public async Task<UserSettings> LoadSettings(string uid, string idToken)
        {
            string json = await ReadDocument(uid, "settings", "settings", idToken);
            if (string.IsNullOrEmpty(json))
                return null;

            return FirestoreAccountMapper.ToSettings(json);
        }

        public async Task DeleteAccount(string uid, string idToken)
        {
            await DeleteDocument(uid, "profile", "profile", idToken);
            await DeleteDocument(uid, "stats", "stats", idToken);
            await DeleteDocument(uid, "settings", "settings", idToken);
            await DeleteDocument(uid, "garage", "roster", idToken);
            await DeleteDocument(uid, "operations", "recent", idToken);
        }

        private async Task<string> ReadDocument(string uid, string collectionId, string documentId, string idToken)
        {
            var result = await _documentClient.ReadDocumentAsync(uid, collectionId, documentId, idToken);
            if (!result.success)
                return null;

            return result.body;
        }

        private async Task WriteDocument(string uid, string collectionId, string documentId, object data, string idToken)
        {
            await _documentClient.WriteDocumentAsync(
                uid,
                collectionId,
                documentId,
                FirestoreFieldSerializer.BuildFieldsJson(data),
                idToken);
        }

        private async Task WriteRawDocument(string uid, string collectionId, string documentId, string rawJson, string idToken)
        {
            await _documentClient.WriteDocumentAsync(
                uid,
                collectionId,
                documentId,
                FirestoreFieldSerializer.BuildRawJsonDocument(rawJson),
                idToken);
        }

        private async Task DeleteDocument(string uid, string collectionId, string documentId, string idToken)
        {
            await _documentClient.DeleteDocumentAsync(uid, collectionId, documentId, idToken);
        }
    }
}
