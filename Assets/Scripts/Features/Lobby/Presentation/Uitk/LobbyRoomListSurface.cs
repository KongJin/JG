using System;
using Shared.Kernel;
using Shared.Ui;
using UnityEngine.UIElements;

namespace Features.Lobby.Presentation
{
    internal sealed class LobbyRoomListSurface
    {
        private const string SelectedRoomClass = "lobby-room-row--selected";
        private const string ClosedRoomClass = "lobby-room-row--closed";
        private const string StatusClosedClass = "lobby-status-chip--closed";

        private readonly VisualElement _roomListViewport;
        private readonly VisualElement _roomList;
        private readonly VisualElement _emptyStateCard;
        private readonly VisualElement _createRoomCard;
        private readonly Label _countLabel;
        private readonly Label _emptyBody;
        private readonly Action<DomainEntityId> _roomSelected;

        public LobbyRoomListSurface(
            VisualElement roomListViewport,
            VisualElement roomList,
            VisualElement emptyStateCard,
            VisualElement createRoomCard,
            Label countLabel,
            Label emptyBody,
            Action<DomainEntityId> roomSelected)
        {
            _roomListViewport = roomListViewport;
            _roomList = roomList;
            _emptyStateCard = emptyStateCard;
            _createRoomCard = createRoomCard;
            _countLabel = countLabel;
            _emptyBody = emptyBody;
            _roomSelected = roomSelected;
        }

        public void HideEmptyState()
        {
            UitkElementUtility.SetDisplay(_roomListViewport, false);
            UitkElementUtility.SetDisplay(_roomList, false);
            UitkElementUtility.SetDisplay(_emptyStateCard, false);
            UitkElementUtility.SetDisplay(_createRoomCard, false);
        }

        public void Render(LobbyRoomListViewModel viewModel)
        {
            viewModel ??= LobbyRoomListViewModel.Empty;
            if (_countLabel != null)
                _countLabel.text = viewModel.CountText;
            if (_emptyBody != null)
                _emptyBody.text = viewModel.EmptyText;

            _roomList?.Clear();
            var hasRooms = viewModel.Rows != null && viewModel.Rows.Count > 0;
            UitkElementUtility.SetDisplay(_roomListViewport, hasRooms);
            UitkElementUtility.SetDisplay(_roomList, hasRooms);
            UitkElementUtility.SetDisplay(_emptyStateCard, !hasRooms);
            UitkElementUtility.SetDisplay(_createRoomCard, hasRooms);

            if (!hasRooms)
                return;

            for (var i = 0; i < viewModel.Rows.Count; i++)
                _roomList?.Add(CreateRoomRow(viewModel.Rows[i]));
        }

        private VisualElement CreateRoomRow(LobbyRoomRowViewModel room)
        {
            var row = new Button(() => _roomSelected?.Invoke(room.RoomId));
            row.AddToClassList("lobby-room-row");
            UitkElementUtility.SetClass(row, SelectedRoomClass, room.IsSelected);
            UitkElementUtility.SetClass(row, ClosedRoomClass, !room.CanJoin);

            var content = new VisualElement();
            content.AddToClassList("lobby-room-row__content");

            var header = new VisualElement();
            header.AddToClassList("lobby-room-row__header");
            header.Add(LobbyUitkElementFactory.CreateTextLabel(
                room.TitleText,
                "lobby-room-row__title"));

            var status = LobbyUitkElementFactory.CreateTextLabel(room.StatusText, "lobby-status-chip");
            UitkElementUtility.SetClass(status, StatusClosedClass, !room.CanJoin);
            header.Add(status);

            content.Add(header);
            content.Add(LobbyUitkElementFactory.CreateTextLabel(
                room.MetaText,
                "lobby-room-row__meta"));

            var footer = new VisualElement();
            footer.AddToClassList("lobby-room-row__footer");

            var slotRow = new VisualElement();
            slotRow.AddToClassList("lobby-slot-row");
            LobbySlotRowRenderer.Render(slotRow, room.FilledSlots, room.TotalSlots);
            footer.Add(slotRow);

            content.Add(footer);
            row.Add(content);
            return row;
        }
    }
}
