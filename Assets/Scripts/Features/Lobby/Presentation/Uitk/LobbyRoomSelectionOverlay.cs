using Shared.Ui;
using UnityEngine.UIElements;

namespace Features.Lobby.Presentation
{
    internal sealed class LobbyRoomSelectionOverlay
    {
        private const string StatusClosedClass = "lobby-status-chip--closed";

        private readonly VisualElement _overlay;
        private readonly VisualElement _slotRow;
        private readonly Label _title;
        private readonly Label _meta;
        private readonly Label _status;
        private readonly Label _body;
        private readonly Button _joinButton;

        public LobbyRoomSelectionOverlay(
            VisualElement overlay,
            VisualElement slotRow,
            Label title,
            Label meta,
            Label status,
            Label body,
            Button joinButton)
        {
            _overlay = overlay;
            _slotRow = slotRow;
            _title = title;
            _meta = meta;
            _status = status;
            _body = body;
            _joinButton = joinButton;
        }

        public void SetVisible(bool visible)
        {
            UitkElementUtility.SetDisplay(_overlay, visible);
        }

        public bool Render(LobbyRoomSelectionViewModel viewModel)
        {
            viewModel ??= LobbyRoomSelectionViewModel.Empty;
            // csharp-guardrails: allow-null-defense
            _slotRow?.Clear();

// csharp-guardrails: allow-null-defense
            if (_title != null)
                _title.text = viewModel.TitleText;
// csharp-guardrails: allow-null-defense
            if (_meta != null)
                _meta.text = viewModel.MetaText;
// csharp-guardrails: allow-null-defense
            if (_status != null)
            {
                _status.text = viewModel.StatusText;
                UitkElementUtility.SetClass(_status, StatusClosedClass, !viewModel.CanJoin);
            }
// csharp-guardrails: allow-null-defense
            if (_body != null)
                _body.text = viewModel.BodyText;
            // csharp-guardrails: allow-null-defense
            if (_joinButton != null)
            {
                _joinButton.text = viewModel.JoinButtonText;
                _joinButton.SetEnabled(viewModel.CanJoin);
            }

            LobbySlotRowRenderer.Render(_slotRow, viewModel.FilledSlots, viewModel.TotalSlots);
            SetVisible(viewModel.IsVisible);
            return viewModel.IsVisible;
        }
    }
}
