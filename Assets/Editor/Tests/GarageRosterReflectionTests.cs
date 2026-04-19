using System;
using System.Reflection;
using NUnit.Framework;

namespace Tests.Editor
{
    /// <summary>
    /// Assembly-CSharp를 직접 참조하지 않고 reflection으로 핵심 GarageRoster 동작을 검증한다.
    /// 기존 프로젝트가 asmdef-less 구조라 Test Runner 연결선만 먼저 복구한다.
    /// </summary>
    public sealed class GarageRosterReflectionTests
    {
        private static readonly Type RosterType = Type.GetType("Features.Garage.Domain.GarageRoster, Assembly-CSharp");
        private static readonly Type UnitLoadoutType = Type.GetType("Features.Garage.Domain.GarageRoster+UnitLoadout, Assembly-CSharp");

        [Test]
        public void GarageRosterType_IsAvailableFromAssemblyCSharp()
        {
            Assert.NotNull(RosterType);
            Assert.NotNull(UnitLoadoutType);
        }

        [Test]
        public void GarageRoster_PublicApiSurface_IsAvailable()
        {
            Assert.NotNull(RosterType.GetMethod("SetSlot", BindingFlags.Instance | BindingFlags.Public));
            Assert.NotNull(RosterType.GetMethod("GetFilledLoadouts", BindingFlags.Instance | BindingFlags.Public));
            Assert.NotNull(RosterType.GetProperty("Count", BindingFlags.Instance | BindingFlags.Public));
            Assert.NotNull(RosterType.GetProperty("IsValid", BindingFlags.Instance | BindingFlags.Public));
        }
    }
}
