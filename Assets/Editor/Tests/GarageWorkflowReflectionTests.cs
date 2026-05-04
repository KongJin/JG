using System;
using System.Reflection;
using NUnit.Framework;

namespace Tests.Editor
{
    public sealed class GarageWorkflowReflectionTests
    {
        private static readonly Type GarageRosterType = Type.GetType("Features.Garage.Domain.GarageRoster, Assembly-CSharp");
        private static readonly Type UnitLoadoutType = Type.GetType("Features.Garage.Domain.GarageRoster+UnitLoadout, Assembly-CSharp");
        private static readonly Type GaragePageStateType = Type.GetType("Features.Garage.Presentation.GaragePageState, Assembly-CSharp");
        private static readonly Type GaragePageViewModelBuildersType = Type.GetType("Features.Garage.Presentation.GaragePageViewModelBuilders, Assembly-CSharp");
        private static readonly Type GarageDraftEvaluationType = Type.GetType("Features.Garage.Presentation.GarageDraftEvaluation, Assembly-CSharp");
        private static readonly Type ValidateRosterUseCaseType = Type.GetType("Features.Garage.Application.ValidateRosterUseCase, Assembly-CSharp");
        private static readonly Type UnitType = Type.GetType("Features.Unit.Domain.Unit, Assembly-CSharp");
        private static readonly Type ResultType = Type.GetType("Shared.Kernel.Result, Assembly-CSharp");
        private static readonly Type ResultOfUnitType = Type.GetType("Shared.Kernel.Result`1, Assembly-CSharp")?.MakeGenericType(UnitType);

        [Test]
        public void GarageWorkflowTypes_AreAvailable()
        {
            Assert.NotNull(GarageRosterType);
            Assert.NotNull(UnitLoadoutType);
            Assert.NotNull(GaragePageStateType);
            Assert.NotNull(GaragePageViewModelBuildersType);
            Assert.NotNull(GarageDraftEvaluationType);
            Assert.NotNull(ValidateRosterUseCaseType);
            Assert.NotNull(ResultType);
            Assert.NotNull(ResultOfUnitType);
        }

        [Test]
        public void GarageWorkflow_PublicMethods_AreAvailable()
        {
            Assert.NotNull(GaragePageViewModelBuildersType.GetMethod(
                "BuildResultViewModel",
                BindingFlags.Instance | BindingFlags.Public,
                null,
                new[] { GaragePageStateType, GarageDraftEvaluationType, typeof(string) },
                null));
            Assert.NotNull(GaragePageStateType.GetMethod(
                "Initialize",
                BindingFlags.Instance | BindingFlags.Public,
                null,
                new[] { GarageRosterType },
                null));
            Assert.NotNull(GarageDraftEvaluationType.GetMethod(
                "Create",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { GaragePageStateType, typeof(bool), ResultOfUnitType, ResultOfUnitType, ResultType },
                null));
            Assert.NotNull(ValidateRosterUseCaseType.GetMethod(
                "Execute",
                BindingFlags.Instance | BindingFlags.Public,
                null,
                new[] { GarageRosterType, typeof(string).MakeByRefType() },
                null));
        }
    }
}
