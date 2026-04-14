using UnityEngine;

namespace Features.Garage.Presentation.Theme
{
    /// <summary>
    /// Garage UI 전용 색상 토큰.
    /// Garage 피처 전용이므로 Shared에 두지 않음 (architecture.md 규칙).
    /// </summary>
    public static class ThemeColors
    {
        // ─── 배경 ───────────────────────────────────────────
        public static readonly Color BackgroundPrimary   = ParseColor("#0F1220");
        public static readonly Color BackgroundSecondary = ParseColor("#1A1E2E");
        public static readonly Color BackgroundCard      = ParseColor("#1E2436");

        // ─── 텍스트 ─────────────────────────────────────────
        /// <summary>주요 텍스트 — 흰색</summary>
        public static readonly Color TextPrimary         = ParseColor("#FFFFFF");
        /// <summary>보조 텍스트 — 연한 회색 (기존 #A8B4C8 대비도 향상)</summary>
        public static readonly Color TextSecondary       = ParseColor("#A8B4C8");
        /// <summary>비활성/힌트 텍스트 — 어두운 회색</summary>
        public static readonly Color TextMuted           = ParseColor("#6B7A94");

        // ─── 액센트 ─────────────────────────────────────────
        public static readonly Color AccentBlue          = ParseColor("#3E7AE5");
        public static readonly Color AccentOrange        = ParseColor("#E5573E");
        public static readonly Color AccentGreen         = ParseColor("#3EAF57");
        public static readonly Color AccentRed           = ParseColor("#AF2E2E");

        // ─── 상태 ───────────────────────────────────────────
        public static readonly Color StateSelected       = ParseColor("#3E7AE5");
        public static readonly Color StateHover          = ParseColor("#2A3048");
        public static readonly Color StateDisabled       = ParseColor("#3D4560");

        // ─── 슬롯 색상 ──────────────────────────────────────
        public static readonly Color SlotSelected        = new(0.24f, 0.47f, 0.89f, 1f);
        public static readonly Color SlotFilled          = new(0.17f, 0.21f, 0.32f, 1f);
        public static readonly Color SlotEmpty           = new(0.10f, 0.12f, 0.18f, 0.92f);
        public static readonly Color SlotEmptyHover      = new(0.14f, 0.17f, 0.25f, 1f);

        // ─── 헬퍼 ───────────────────────────────────────────
        private static Color ParseColor(string hex)
        {
            if (string.IsNullOrEmpty(hex) || hex[0] != '#')
                return Color.magenta;

            hex = hex.Substring(1);

            if (hex.Length == 6)
            {
                int r = int.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                int g = int.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
                int b = int.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
                return new Color32((byte)r, (byte)g, (byte)b, 255);
            }

            if (hex.Length == 8)
            {
                int r = int.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                int g = int.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
                int b = int.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
                int a = int.Parse(hex.Substring(6, 2), System.Globalization.NumberStyles.HexNumber);
                return new Color32((byte)r, (byte)g, (byte)b, (byte)a);
            }

            return Color.magenta;
        }
    }
}
