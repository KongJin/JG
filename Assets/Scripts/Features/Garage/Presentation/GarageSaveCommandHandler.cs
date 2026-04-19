using Features.Garage.Application;
using System.Threading.Tasks;

namespace Features.Garage.Presentation
{
    internal sealed class GarageSaveCommandHandler
    {
        public async Task<GarageSaveCommandResult> ExecuteAsync(
            GaragePageState state,
            GarageDraftEvaluation evaluation,
            GarageSetup setup,
            GarageResultPanelView resultPanelView,
            System.Action onSaveStarted,
            System.Action onSaveFinished)
        {
            if (!evaluation.CanSave)
            {
                var message = !string.IsNullOrWhiteSpace(evaluation.RosterValidationError)
                    ? evaluation.RosterValidationError
                    : evaluation.HasDraftChanges
                        ? "Draft is not ready to save."
                        : "No unsaved changes.";
                state.SetValidationOverride(message);
                return GarageSaveCommandResult.RenderOnly();
            }

            onSaveStarted?.Invoke();
            resultPanelView.ShowLoading(true);
            var result = await setup.SaveRoster.Execute(state.DraftRoster.Clone());
            onSaveFinished?.Invoke();
            resultPanelView.ShowLoading(false);

            if (!result.IsSuccess)
            {
                state.SetValidationOverride(result.Error);
                return GarageSaveCommandResult.RenderOnly();
            }

            state.CommitDraft();
            resultPanelView.ShowToast("Roster saved!");
            return GarageSaveCommandResult.RenderOnly();
        }
    }

    internal readonly struct GarageSaveCommandResult
    {
        public bool ShouldRender { get; }

        private GarageSaveCommandResult(bool shouldRender)
        {
            ShouldRender = shouldRender;
        }

        public static GarageSaveCommandResult RenderOnly() => new(true);
    }
}
