using Features.Garage.Presentation;
using NUnit.Framework;
using Shared.Kernel;
using ComposedUnit = Features.Unit.Domain.Unit;

namespace Tests.Editor
{
    public sealed class GaragePagePresenterDirectTests
    {
        [Test]
        public void ReadyUnlocked_WhenCommittedRosterIsValidAndDraftIsClean()
        {
            var state = CreateInitializedState(3);
            var presenter = new GaragePagePresenter(null);
            var evaluation = CreateEvaluation(state, resultSuccess: true);

            var viewModel = presenter.BuildResultViewModel(state, evaluation);

            Assert.IsTrue(viewModel.IsReady);
            Assert.IsFalse(viewModel.IsDirty);
            Assert.IsFalse(viewModel.CanSave);
            Assert.AreEqual("저장됨", viewModel.PrimaryActionLabel);
        }

        [Test]
        public void DraftChanges_BlockReady_AndSwitchPrimaryActionToSave()
        {
            var state = CreateInitializedState(3);
            state.SetEditingFrameId("frame-updated");

            var presenter = new GaragePagePresenter(null);
            var evaluation = CreateEvaluation(state, resultSuccess: true);

            var viewModel = presenter.BuildResultViewModel(state, evaluation);

            Assert.IsFalse(viewModel.IsReady);
            Assert.IsTrue(viewModel.IsDirty);
            Assert.IsTrue(viewModel.CanSave);
            Assert.AreEqual("편성 저장", viewModel.PrimaryActionLabel);
            StringAssert.Contains("SAVE REQUIRED", viewModel.RosterStatusText);
        }

        [Test]
        public void CommitDraft_RestoresReadyState()
        {
            var state = CreateInitializedState(3);
            state.SetEditingFrameId("frame-updated");
            state.CommitDraft();

            var presenter = new GaragePagePresenter(null);
            var evaluation = CreateEvaluation(state, resultSuccess: true);

            var viewModel = presenter.BuildResultViewModel(state, evaluation);

            Assert.IsTrue(viewModel.IsReady);
            Assert.IsFalse(viewModel.IsDirty);
            Assert.IsFalse(viewModel.CanSave);
            StringAssert.Contains("SYNCED ROSTER", viewModel.RosterStatusText);
        }

        private static GaragePageState CreateInitializedState(int completeUnitCount)
        {
            var roster = new Features.Garage.Domain.GarageRoster();
            for (int i = 0; i < completeUnitCount; i++)
            {
                roster.SetSlot(i, new Features.Garage.Domain.GarageRoster.UnitLoadout(
                    $"frame{i}",
                    $"fire{i}",
                    $"mob{i}"));
            }

            var state = new GaragePageState();
            state.Initialize(roster);
            return state;
        }

        private static GarageDraftEvaluation CreateEvaluation(GaragePageState state, bool resultSuccess)
        {
            var composeFailure = Result<ComposedUnit>.Failure("compose-not-needed");
            var rosterValidation = resultSuccess ? Result.Success() : Result.Failure("validation failed");
            return GarageDraftEvaluation.Create(state, false, composeFailure, rosterValidation);
        }
    }
}
