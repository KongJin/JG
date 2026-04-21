using System;
using System.Reflection;
using NUnit.Framework;

namespace Tests.Editor
{
    public sealed class InfrastructureMapperReflectionTests
    {
        private static readonly Type FirestoreFieldSerializerType = Type.GetType("Features.Account.Infrastructure.FirestoreFieldSerializer, Assembly-CSharp");
        private static readonly Type FirestoreFieldReaderType = Type.GetType("Features.Account.Infrastructure.FirestoreFieldReader, Assembly-CSharp");
        private static readonly Type FirebaseAuthResponseMapperType = Type.GetType("Features.Account.Infrastructure.FirebaseAuthResponseMapper, Assembly-CSharp");
        private static readonly Type TokenResponseType = Type.GetType("Features.Account.Infrastructure.FirebaseAuthRestAdapter+TokenResponse, Assembly-CSharp");
        private static readonly Type LobbyReadyPolicyType = Type.GetType("Features.Lobby.Application.LobbyReadyPolicy, Assembly-CSharp");
        private static readonly Type GarageRosterType = Type.GetType("Features.Garage.Domain.GarageRoster, Assembly-CSharp");
        private static readonly Type GarageUnitLoadoutType = Type.GetType("Features.Garage.Domain.GarageRoster+UnitLoadout, Assembly-CSharp");

        [Test]
        public void InfrastructureReflectionTypes_AreAvailable()
        {
            Assert.NotNull(FirestoreFieldSerializerType);
            Assert.NotNull(FirestoreFieldReaderType);
            Assert.NotNull(FirebaseAuthResponseMapperType);
            Assert.NotNull(TokenResponseType);
            Assert.NotNull(LobbyReadyPolicyType);
            Assert.NotNull(GarageRosterType);
            Assert.NotNull(GarageUnitLoadoutType);
        }

        [Test]
        public void InfrastructureReflection_PublicAndInternalEntryPoints_AreAvailable()
        {
            Assert.NotNull(FirestoreFieldSerializerType.GetMethod("BuildRawJsonDocument", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic));
            Assert.NotNull(FirestoreFieldReaderType.GetMethod("GetString", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic));
            Assert.NotNull(FirebaseAuthResponseMapperType.GetMethod("FromTokenResponse", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic));
            Assert.NotNull(LobbyReadyPolicyType.GetMethod("ComputeReadyEligible", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
            Assert.NotNull(GarageRosterType.GetMethod("SetSlot", BindingFlags.Instance | BindingFlags.Public));
            Assert.NotNull(GarageUnitLoadoutType.GetConstructor(new[] { typeof(string), typeof(string), typeof(string) }));
        }
    }
}
