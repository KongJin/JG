using Shared.Ui;
using UnityEngine.UIElements;

namespace Features.Lobby.Presentation
{
    internal static class LobbyUitkElementFactory
    {
        public static Label CreateTextLabel(string text, string className)
        {
            var label = UitkElementUtility.CreateLabel(text, className);
            label.style.color = UiThemeColors.TextPrimary;
            return label;
        }
    }
}
