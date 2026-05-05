using Features.Garage.Application;
using Features.Garage.Domain;

namespace Features.Garage.Presentation
{
    internal enum GarageSaveFlowResultKind
    {
        Ignored,
        Blocked,
        Failed,
        Saved,
    }

    internal readonly struct GarageSaveFlowResult
    {
        private GarageSaveFlowResult(GarageSaveFlowResultKind kind, string message)
        {
            Kind = kind;
            Message = message;
        }

        public GarageSaveFlowResultKind Kind { get; }
        public string Message { get; }

        public static GarageSaveFlowResult Ignored() => new(GarageSaveFlowResultKind.Ignored, string.Empty);
        public static GarageSaveFlowResult Blocked(string message) => new(GarageSaveFlowResultKind.Blocked, message);
        public static GarageSaveFlowResult Failed(string message) => new(GarageSaveFlowResultKind.Failed, message);
        public static GarageSaveFlowResult Saved() => new(GarageSaveFlowResultKind.Saved, string.Empty);
    }

    internal sealed class GarageSaveFlow
    {
        public bool IsSaving { get; private set; }

        public async System.Threading.Tasks.Task<GarageSaveFlowResult> SaveAsync(
            GarageRoster draftRoster,
            GarageDraftEvaluation evaluation,
            SaveRosterUseCase saveRoster,
            System.Action<GarageDraftEvaluation> onStarted,
            System.Action onEnded)
        {
            if (IsSaving)
                return GarageSaveFlowResult.Ignored();

            if (evaluation == null || !evaluation.CanSave)
            {
// csharp-guardrails: allow-null-defense
                return GarageSaveFlowResult.Blocked(evaluation?.SaveBlockedMessage ?? "Draft is not ready to save.");
            }

            if (draftRoster == null || saveRoster == null)
                return GarageSaveFlowResult.Failed("저장할 편성 데이터가 없습니다.");

            IsSaving = true;
            onStarted?.Invoke(evaluation);

            try
            {
                var result = await saveRoster.Execute(draftRoster.Clone());
                return result.IsSuccess
                    ? GarageSaveFlowResult.Saved()
                    : GarageSaveFlowResult.Failed(result.Error);
            }
            finally
            {
                IsSaving = false;
                onEnded?.Invoke();
            }
        }
    }
}
