using Shared.Ui;
using UnityEngine.UIElements;

namespace Features.Lobby.Presentation
{
    internal sealed class LobbyRoomDetailSurface
    {
        private readonly VisualElement _card;
        private readonly VisualElement _actionRow;
        private readonly VisualElement _memberList;
        private readonly Label _title;
        private readonly Label _meta;
        private readonly Button _readyButton;
        private readonly Button _startButton;

        public LobbyRoomDetailSurface(
            VisualElement card,
            VisualElement actionRow,
            VisualElement memberList,
            Label title,
            Label meta,
            Button readyButton,
            Button startButton)
        {
            _card = card;
            _actionRow = actionRow;
            _memberList = memberList;
            _title = title;
            _meta = meta;
            _readyButton = readyButton;
            _startButton = startButton;
        }

        public void Hide()
        {
            UitkElementUtility.SetDisplay(_card, false);
        }

        public bool Render(LobbyRoomDetailViewModel viewModel)
        {
            viewModel ??= LobbyRoomDetailViewModel.Empty;
            var isInRoom = viewModel.MemberRows != null && viewModel.MemberRows.Count > 0;
            UitkElementUtility.SetDisplay(_card, isInRoom);
            if (_actionRow != null)
                _actionRow.style.display = isInRoom ? DisplayStyle.Flex : DisplayStyle.None;

            if (_title != null)
                _title.text = viewModel.TitleText;
            if (_meta != null)
                _meta.text = viewModel.MetaText;

            _memberList?.Clear();
            if (viewModel.MemberRows != null)
            {
                for (var i = 0; i < viewModel.MemberRows.Count; i++)
                {
                    _memberList?.Add(LobbyUitkElementFactory.CreateTextLabel(
                        viewModel.MemberRows[i],
                        "lobby-member-row"));
                }
            }

            if (_readyButton != null)
                _readyButton.text = viewModel.ReadyButtonText;
            if (_startButton != null)
                _startButton.SetEnabled(viewModel.CanStartGame);

            return isInRoom;
        }
    }
}
