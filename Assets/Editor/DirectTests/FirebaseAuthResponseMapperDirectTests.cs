using System;
using Features.Account.Infrastructure;
using NUnit.Framework;

namespace Tests.Editor
{
    public sealed class FirebaseAuthResponseMapperDirectTests
    {
        [Test]
        public void FromTokenResponse_UsesFallbackUid_AndComputesExpiry()
        {
            var response = new FirebaseAuthRestAdapter.TokenResponse
            {
                id_token = "id-token",
                refresh_token = "refresh-token",
                expires_in = "3600",
                user_id = string.Empty
            };

            var snapshot = FirebaseAuthResponseMapper.FromTokenResponse(response, "fallback-uid");

            Assert.AreEqual("id-token", snapshot.IdToken);
            Assert.AreEqual("refresh-token", snapshot.RefreshToken);
            Assert.AreEqual("fallback-uid", snapshot.Uid);
            Assert.Greater(snapshot.ExpiryUnixMs, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        }
    }
}
