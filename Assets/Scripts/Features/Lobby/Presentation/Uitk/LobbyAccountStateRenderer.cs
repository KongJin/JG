using Shared.Ui;
using UnityEngine.UIElements;

namespace Features.Lobby.Presentation
{
    internal static class LobbyAccountStateRenderer
    {
        public static void Render(VisualElement accountPage, LobbyAccountViewModel viewModel)
        {
            if (accountPage == null)
                return;

            viewModel ??= LobbyAccountViewModel.Empty;
            UitkElementUtility.SetText(accountPage, "PilotIdLabel", viewModel.PilotIdText);
            UitkElementUtility.SetText(accountPage, "GoogleLinkStatusLabel", viewModel.GoogleLinkStatusText);
            UitkElementUtility.SetText(accountPage, "UidStatusLabel", viewModel.UidStatusText);
            UitkElementUtility.SetText(accountPage, "GarageSyncStateLabel", viewModel.GarageSyncStateText);
            UitkElementUtility.SetText(accountPage, "OperationSyncStateLabel", viewModel.OperationSyncStateText);
            UitkElementUtility.SetText(accountPage, "CloudSyncStateLabel", viewModel.CloudSyncStateText);
            UitkElementUtility.SetText(accountPage, "BlockedReasonBodyLabel", viewModel.BlockedReasonBodyText);
            UitkElementUtility.SetText(accountPage, "GarageSummaryLabel", viewModel.GarageSummaryText);
            UitkElementUtility.SetText(accountPage, "OperationBufferLabel", viewModel.OperationBufferText);
            UitkElementUtility.SetText(accountPage, "ConflictStateLabel", viewModel.ConflictStateText);
            UitkElementUtility.SetText(accountPage, "LoadingStateLabel", viewModel.LoadingStateText);
            UitkElementUtility.SetText(accountPage, "BgmValueLabel", viewModel.BgmValueText);
            UitkElementUtility.SetText(accountPage, "SfxValueLabel", viewModel.SfxValueText);
            UitkElementUtility.SetText(accountPage, "SaveModeLabel", viewModel.SaveModeText);
            UitkElementUtility.SetText(accountPage, "CloudModeLabel", viewModel.CloudModeText);
        }
    }
}
