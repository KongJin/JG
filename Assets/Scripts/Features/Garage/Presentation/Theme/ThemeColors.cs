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
        public static readonly Color BackgroundPrimary   = ParseColor("#0C1220");
        public static readonly Color BackgroundSecondary = ParseColor("#152032");
        public static readonly Color BackgroundCard      = ParseColor("#243247");

        // ─── 텍스트 ─────────────────────────────────────────
        /// <summary>주요 텍스트 — 흰색</summary>
        public static readonly Color TextPrimary         = ParseColor("#E5E7EB");
        /// <summary>보조 텍스트 — tactical readout용 연한 회색</summary>
        public static readonly Color TextSecondary       = ParseColor("#A4B3C7");
        /// <summary>비활성/힌트 텍스트</summary>
        public static readonly Color TextMuted           = ParseColor("#76839A");

        // ─── 액센트 ─────────────────────────────────────────
        public static readonly Color AccentBlue          = ParseColor("#67B8FF");
        public static readonly Color AccentOrange        = ParseColor("#F28A18");
        public static readonly Color AccentAmber         = ParseColor("#F4B942");
        public static readonly Color AccentGreen         = ParseColor("#22C55E");
        public static readonly Color AccentRed           = ParseColor("#EF4444");

        // ─── 상태 ───────────────────────────────────────────
        public static readonly Color StateSelected       = ParseColor("#67B8FF");
        public static readonly Color StateHover          = ParseColor("#334A63");
        public static readonly Color StateDisabled       = ParseColor("#4B5F79");

        // ─── 슬롯 색상 ──────────────────────────────────────
        public static readonly Color SlotSelected        = ParseColor("#395A79");
        public static readonly Color SlotFilled          = ParseColor("#202E41");
        public static readonly Color SlotDirty           = ParseColor("#4A3318");
        public static readonly Color SlotEmpty           = new(0.10f, 0.15f, 0.21f, 0.95f);
        public static readonly Color SlotEmptyHover      = new(0.16f, 0.23f, 0.32f, 1f);
        public static readonly Color PanelOutline        = ParseColor("#39536D");

        // ─── 3D 프리뷰 ─────────────────────────────────────
        public static readonly Color PreviewBackground   = ParseColor("#08111E");
        public static readonly Color PreviewFrameStriker = ParseColor("#F28019");
        public static readonly Color PreviewFrameBastion = ParseColor("#5EB6FF");
        public static readonly Color PreviewFrameRelay   = ParseColor("#22C55E");
        public static readonly Color PreviewFireScatter  = ParseColor("#E63333");
        public static readonly Color PreviewFirePulse    = ParseColor("#E6E633");
        public static readonly Color PreviewFireRail     = ParseColor("#5EB6FF");
        public static readonly Color PreviewMobTreads    = ParseColor("#808080");
        public static readonly Color PreviewMobBurst     = ParseColor("#22C55E");

        // ─── 토스트 ─────────────────────────────────────────
        public static readonly Color ToastErrorBg        = new(0.35f, 0.08f, 0.08f, 0.95f);
        public static readonly Color ToastSuccessBg      = new(0.08f, 0.30f, 0.12f, 0.95f);
        public static readonly Color ToastErrorText      = new(1f, 0.7f, 0.7f, 1f);
        public static readonly Color ToastSuccessText    = new(0.7f, 1f, 0.7f, 1f);

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
