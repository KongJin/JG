using Shared.Ui;
using UnityEngine.UIElements;

namespace Features.Lobby.Presentation
{
    internal sealed class LobbyGarageSummarySurface
    {
        private const string StatusClosedClass = "lobby-status-chip--closed";
        private const string StatusReadyClass = "lobby-status-chip--ready";

        private readonly VisualElement _slotRow;
        private readonly Label _status;
        private readonly Label _title;
        private readonly Label _body;

        public LobbyGarageSummarySurface(
            VisualElement slotRow,
            Label status,
            Label title,
            Label body)
        {
            _slotRow = slotRow;
            _status = status;
            _title = title;
            _body = body;
        }

        public void Render(LobbyGarageSummaryViewModel viewModel)
        {
            viewModel ??= LobbyGarageSummaryViewModel.Empty;
            if (_status != null)
            {
                _status.text = viewModel.StatusText;
                UitkElementUtility.SetClass(_status, StatusReadyClass, viewModel.IsReady);
                UitkElementUtility.SetClass(_status, StatusClosedClass, !viewModel.IsReady);
            }

            if (_title != null)
                _title.text = viewModel.SummaryText;
            if (_body != null)
                _body.text = viewModel.DetailText;

            LobbySlotRowRenderer.Render(_slotRow, viewModel.FilledSlots, viewModel.TotalSlots);
        }
    }
}
