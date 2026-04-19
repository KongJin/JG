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
        private static readonly Type GaragePagePresenterType = Type.GetType("Features.Garage.Presentation.GaragePagePresenter, Assembly-CSharp");
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
            Assert.NotNull(GaragePagePresenterType);
            Assert.NotNull(GarageDraftEvaluationType);
            Assert.NotNull(ValidateRosterUseCaseType);
            Assert.NotNull(ResultType);
            Assert.NotNull(ResultOfUnitType);
        }

        [Test]
        public void GarageWorkflow_PublicMethods_AreAvailable()
        {
            Assert.NotNull(GaragePagePresenterType.GetMethod("BuildResultViewModel", BindingFlags.Instance | BindingFlags.Public));
            Assert.NotNull(GaragePageStateType.GetMethod("Initialize", BindingFlags.Instance | BindingFlags.Public));
            Assert.NotNull(GarageDraftEvaluationType.GetMethod("Create", BindingFlags.Public | BindingFlags.Static));
            Assert.NotNull(ValidateRosterUseCaseType.GetMethod("Execute", BindingFlags.Instance | BindingFlags.Public));
        }
    }
}
