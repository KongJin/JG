using UnityEngine;

namespace Shared.Ui
{
    public static class UiThemeColors
    {
        public static readonly Color TextPrimary = ParseColor("#DBE8F5");

        private static Color ParseColor(string hex)
        {
            if (string.IsNullOrEmpty(hex) || hex[0] != '#' || hex.Length != 7)
                return Color.white;

            int r = int.Parse(hex.Substring(1, 2), System.Globalization.NumberStyles.HexNumber);
            int g = int.Parse(hex.Substring(3, 2), System.Globalization.NumberStyles.HexNumber);
            int b = int.Parse(hex.Substring(5, 2), System.Globalization.NumberStyles.HexNumber);
            return new Color32((byte)r, (byte)g, (byte)b, 255);
        }
    }
}
