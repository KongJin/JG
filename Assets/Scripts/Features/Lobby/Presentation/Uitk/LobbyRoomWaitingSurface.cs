using Features.Lobby.Domain;
using Shared.Ui;
using UnityEngine.UIElements;

namespace Features.Lobby.Presentation
{
    internal sealed class LobbyRoomWaitingSurface
    {
        private const string LocalRowClass = "lobby-waiting-participant-row--local";
        private const string EmptyRowClass = "lobby-waiting-participant-row--empty";
        private const string ReadyStatusClass = "lobby-waiting-participant-status--ready";
        private const string SelectedTeamClass = "lobby-team-toggle--selected";

        private readonly VisualElement _page;
        private readonly VisualElement _participantList;
        private readonly VisualElement _deckSlotRow;
        private readonly Label _title;
        private readonly Label _meta;
        private readonly Label _state;
        private readonly Label _host;
        private readonly Label _connection;
        private readonly Label _deckStatus;
        private readonly Label _deckTitle;
        private readonly Label _deckBody;
        private readonly Button _redTeamButton;
        private readonly Button _blueTeamButton;
        private readonly Button _readyButton;
        private readonly Button _startButton;

        public LobbyRoomWaitingSurface(
            VisualElement page,
            VisualElement participantList,
            VisualElement deckSlotRow,
            Label title,
            Label meta,
            Label state,
            Label host,
            Label connection,
            Label deckStatus,
            Label deckTitle,
            Label deckBody,
            Button redTeamButton,
            Button blueTeamButton,
            Button readyButton,
            Button startButton)
        {
            _page = page;
            _participantList = participantList;
            _deckSlotRow = deckSlotRow;
            _title = title;
            _meta = meta;
            _state = state;
            _host = host;
            _connection = connection;
            _deckStatus = deckStatus;
            _deckTitle = deckTitle;
            _deckBody = deckBody;
            _redTeamButton = redTeamButton;
            _blueTeamButton = blueTeamButton;
            _readyButton = readyButton;
            _startButton = startButton;
        }

        public bool PrimaryStartsGame { get; private set; }

        public void Hide()
        {
            UitkElementUtility.SetDisplay(_page, false);
            PrimaryStartsGame = false;
        }

        public bool Render(LobbyRoomWaitingViewModel viewModel)
        {
            viewModel ??= LobbyRoomWaitingViewModel.Empty;
            UitkElementUtility.SetDisplay(_page, viewModel.IsVisible);
            PrimaryStartsGame = viewModel.LocalIsOwner && viewModel.LocalIsReady;

            _title.text = viewModel.TitleText;
            _meta.text = viewModel.MetaText;
            _state.text = viewModel.StateText;
            _host.text = viewModel.HostText;
            _connection.text = viewModel.ConnectionText;
            _deckStatus.text = viewModel.DeckSummary.StatusText;
            _deckTitle.text = viewModel.DeckSummary.SummaryText;
            _deckBody.text = viewModel.DeckSummary.DetailText;
            _readyButton.text = viewModel.PrimaryButtonText;
            _readyButton.SetEnabled(!PrimaryStartsGame || viewModel.CanStartGame);

            UitkElementUtility.SetDisplay(_startButton, false);
            UitkElementUtility.SetClass(_redTeamButton, SelectedTeamClass, viewModel.LocalTeam == TeamType.Red);
            UitkElementUtility.SetClass(_blueTeamButton, SelectedTeamClass, viewModel.LocalTeam == TeamType.Blue);
            RenderParticipants(viewModel);
            LobbySlotRowRenderer.Render(
                _deckSlotRow,
                viewModel.DeckSummary.FilledSlots,
                viewModel.DeckSummary.TotalSlots);
            return viewModel.IsVisible;
        }

        private void RenderParticipants(LobbyRoomWaitingViewModel viewModel)
        {
            _participantList.Clear();
            for (var i = 0; i < viewModel.Participants.Count; i++)
                _participantList.Add(CreateParticipantRow(viewModel.Participants[i]));
        }

        private static VisualElement CreateParticipantRow(LobbyRoomParticipantViewModel participant)
        {
            var row = new VisualElement();
            UitkElementUtility.AddClasses(row, "lobby-waiting-participant-row");
            UitkElementUtility.SetClass(row, LocalRowClass, participant.IsLocal);
            UitkElementUtility.SetClass(row, EmptyRowClass, participant.IsEmpty);

            var main = new VisualElement();
            UitkElementUtility.AddClasses(main, "lobby-waiting-participant-main");
            main.Add(UitkElementUtility.CreateLabel(
                participant.DisplayNameText,
                "lobby-waiting-participant-name"));

            var status = UitkElementUtility.CreateLabel(
                participant.StatusText,
                "lobby-waiting-participant-status");
            UitkElementUtility.SetClass(status, ReadyStatusClass, participant.IsReady);
            main.Add(status);
            row.Add(main);

            var meta = new VisualElement();
            UitkElementUtility.AddClasses(meta, "lobby-waiting-participant-meta");
            meta.Add(UitkElementUtility.CreateLabel(
                participant.TeamText,
                "lobby-waiting-participant-team"));
            if (participant.IsLocal)
            {
                meta.Add(UitkElementUtility.CreateLabel(
                    "나",
                    "lobby-waiting-participant-local"));
            }

            row.Add(meta);
            return row;
        }
    }
}
