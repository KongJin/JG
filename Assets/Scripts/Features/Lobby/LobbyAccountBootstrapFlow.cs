using Features.Account;
using Features.Account.Presentation;
using UnityEngine;

internal sealed class LobbyAccountBootstrapFlow
{
    public async System.Threading.Tasks.Task<LobbySignInResult> RunAnonymousSignInAsync(
        AccountSetup accountSetup,
        LoginLoadingView loginLoadingView,
        int maxAttempts,
        System.Func<Features.Account.Domain.AccountProfile, System.Threading.Tasks.Task> onSuccess)
    {
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            if (loginLoadingView == null)
                return LobbySignInResult.None();

            try
            {
                var result = await accountSetup.SignInAnonymously.Execute();
                if (loginLoadingView == null)
                    return LobbySignInResult.None();

                if (result.IsSuccess)
                {
                    await onSuccess(result.Value);
                    return LobbySignInResult.Success(result.Value);
                }

                if (loginLoadingView != null)
                    loginLoadingView.OnLoginFailed(result.Error ?? "Unknown error");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[LobbySetup] Anonymous sign-in flow failed: {ex.Message}");
                Debug.LogException(ex);
                if (loginLoadingView != null)
                    loginLoadingView.OnLoginFailed(ex.Message);
            }

            if (attempt < maxAttempts)
                await System.Threading.Tasks.Task.Delay(1000);
        }

        return LobbySignInResult.None();
    }
}

internal readonly struct LobbySignInResult
{
    public Features.Account.Domain.AccountProfile Profile { get; }
    public bool HasProfile { get; }

    private LobbySignInResult(Features.Account.Domain.AccountProfile profile, bool hasProfile)
    {
        Profile = profile;
        HasProfile = hasProfile;
    }

    public static LobbySignInResult Success(Features.Account.Domain.AccountProfile profile) => new(profile, true);
    public static LobbySignInResult None() => new(null, false);
}
