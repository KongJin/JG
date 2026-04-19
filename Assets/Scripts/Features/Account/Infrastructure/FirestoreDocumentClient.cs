using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace Features.Account.Infrastructure
{
    internal sealed class FirestoreDocumentClient
    {
        private const string DocumentUrl = "https://firestore.googleapis.com/v1/projects/{0}/databases/(default)/documents/accounts/{1}/{2}/{3}?key={4}";

        private readonly string _apiKey;
        private readonly string _projectId;

        public FirestoreDocumentClient(string apiKey, string projectId)
        {
            _apiKey = apiKey;
            _projectId = projectId;
        }

        public async Task<SendResult> ReadDocumentAsync(string uid, string collectionId, string documentId, string idToken)
        {
            using var request = new UnityWebRequest(BuildDocumentUrl(uid, collectionId, documentId), "GET")
            {
                timeout = 15
            };
            request.SetRequestHeader("Authorization", "Bearer " + idToken);
            request.downloadHandler = new DownloadHandlerBuffer();

            var result = await FirestoreRequestDispatcher.SendRequestSafeAsync(request);
            result.body = request.downloadHandler?.text;
            return result;
        }

        public async Task WriteDocumentAsync(string uid, string collectionId, string documentId, string bodyJson, string idToken)
        {
            using var request = new UnityWebRequest(BuildDocumentUrl(uid, collectionId, documentId), "PATCH")
            {
                timeout = 15
            };
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + idToken);
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(bodyJson));
            request.downloadHandler = new DownloadHandlerBuffer();

            await FirestoreRequestDispatcher.SendRequestAsync(request);
        }

        public async Task DeleteDocumentAsync(string uid, string collectionId, string documentId, string idToken)
        {
            using var request = new UnityWebRequest(BuildDocumentUrl(uid, collectionId, documentId), "DELETE")
            {
                timeout = 15
            };
            request.SetRequestHeader("Authorization", "Bearer " + idToken);
            request.downloadHandler = new DownloadHandlerBuffer();

            await FirestoreRequestDispatcher.SendRequestSafeAsync(request);
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
    }
}
