using System;
using System.Reflection;
using NUnit.Framework;

namespace Tests.Editor
{
    public sealed class LobbyAndGameValidationReflectionTests
    {
        private static readonly Type LobbyRuleType = Type.GetType("Features.Lobby.Domain.LobbyRule, Assembly-CSharp");
        private static readonly Type LobbyType = Type.GetType("Features.Lobby.Domain.Lobby, Assembly-CSharp");
        private static readonly Type RoomType = Type.GetType("Features.Lobby.Domain.Room, Assembly-CSharp");
        private static readonly Type RoomMemberType = Type.GetType("Features.Lobby.Domain.RoomMember, Assembly-CSharp");
        private static readonly Type TeamType = Type.GetType("Features.Lobby.Domain.TeamType, Assembly-CSharp");
        private static readonly Type DomainEntityIdType = Type.GetType("Shared.Kernel.DomainEntityId, Assembly-CSharp");
        private static readonly Type ResultType = Type.GetType("Shared.Kernel.Result, Assembly-CSharp");
        private static readonly Type ResultOfRoomType = Type.GetType("Shared.Kernel.Result`1, Assembly-CSharp")?.MakeGenericType(RoomType);
        private static readonly Type InitialEnergyValidatorType = Type.GetType("Features.Unit.Application.InitialEnergyValidator, Assembly-CSharp");
        private static readonly Type ValidationResultType = Type.GetType("Features.Unit.Application.InitialEnergyValidator+ValidationResult, Assembly-CSharp");
        private static readonly Type UnitType = Type.GetType("Features.Unit.Domain.Unit, Assembly-CSharp");

        [Test]
        public void LobbyAndGameValidationTypes_AreAvailable()
        {
            Assert.NotNull(LobbyRuleType);
            Assert.NotNull(LobbyType);
            Assert.NotNull(RoomType);
            Assert.NotNull(RoomMemberType);
            Assert.NotNull(TeamType);
            Assert.NotNull(DomainEntityIdType);
            Assert.NotNull(ResultType);
            Assert.NotNull(ResultOfRoomType);
            Assert.NotNull(InitialEnergyValidatorType);
            Assert.NotNull(ValidationResultType);
            Assert.NotNull(UnitType);
        }

        [Test]
        public void LobbyAndGameValidation_PublicApiSurface_IsAvailable()
        {
            Assert.NotNull(LobbyRuleType.GetMethod("ValidateRoomName", BindingFlags.Public | BindingFlags.Static));
            Assert.NotNull(LobbyRuleType.GetMethod("ValidateDifficultyPreset", BindingFlags.Public | BindingFlags.Static));
            Assert.NotNull(LobbyRuleType.GetMethod("EnsureUniqueRoomName", BindingFlags.Public | BindingFlags.Static));
            Assert.NotNull(InitialEnergyValidatorType.GetMethod("Validate", BindingFlags.Public | BindingFlags.Static));
            Assert.NotNull(RoomType.GetMethod("Create", BindingFlags.Public | BindingFlags.Static));
            Assert.NotNull(LobbyType.GetMethod("AddRoom", BindingFlags.Public | BindingFlags.Instance));
        }
    }
}
