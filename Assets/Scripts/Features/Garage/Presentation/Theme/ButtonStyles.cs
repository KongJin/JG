using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Features.Garage.Presentation.Theme
{
    /// <summary>
    /// Garage UI 전용 버튼 스타일 프리셋.
    /// Editor 런타임 모두에서 사용 가능 (UnityEditor 의존 없음).
    /// </summary>
    public static class ButtonStyles
    {
        // ─── 프리셋 정의 ─────────────────────────────────────

        /// <summary>저장, 주요 액션 — 오렌지 배경 + 밝은 텍스트</summary>
        public static readonly ButtonPreset Primary = new()
        {
            BackgroundColor = ThemeColors.AccentOrange,
            TextColor = ThemeColors.TextPrimary,
            MinHeight = 46,
            CornerRadius = 8f,
            HoverColor = ThemeColors.AccentOrange * 1.10f,
            PressedColor = ThemeColors.AccentOrange * 0.85f,
            DisabledColor = ThemeColors.StateDisabled
        };

        /// <summary>Clear Slot 등 파괴적 액션 — 빨간색</summary>
        public static readonly ButtonPreset Danger = new()
        {
            BackgroundColor = ThemeColors.AccentRed,
            TextColor = ThemeColors.TextPrimary,
            MinHeight = 40,
            CornerRadius = 8f,
            HoverColor = ThemeColors.AccentRed * 1.15f,
            PressedColor = ThemeColors.AccentRed * 0.85f,
            DisabledColor = ThemeColors.StateDisabled
        };

        /// <summary>부품 선택 < > 버튼 — 어두운 배경 + 액센트 텍스트</summary>
        public static readonly ButtonPreset Secondary = new()
        {
            BackgroundColor = ThemeColors.BackgroundCard,
            TextColor = ThemeColors.TextPrimary,
            MinHeight = 36,
            CornerRadius = 6f,
            HoverColor = ThemeColors.StateHover,
            PressedColor = ThemeColors.BackgroundSecondary,
            DisabledColor = ThemeColors.StateDisabled
        };

        /// <summary>Google 로그인 등 고스트 버튼 — 투명 배경 + 보더</summary>
        public static readonly ButtonPreset Ghost = new()
        {
            BackgroundColor = Color.clear,
            TextColor = ThemeColors.TextSecondary,
            MinHeight = 40,
            CornerRadius = 8f,
            HoverColor = ThemeColors.StateHover,
            PressedColor = ThemeColors.BackgroundSecondary,
            DisabledColor = ThemeColors.StateDisabled,
            BorderColor = ThemeColors.StateDisabled,
            BorderWidth = 1f
        };

        // ─── 적용 메서드 ─────────────────────────────────────

        /// <summary>
        /// Button에 프리셋을 적용한다. 텍스트 참조는 caller가 명시적으로 넘긴다.
        /// </summary>
        public static void Apply(this Button button, ButtonPreset preset, TMP_Text text = null)
        {
            if (button == null || preset == null) return;

            var image = button.targetGraphic as Image;
            if (image != null)
            {
                image.color = preset.BackgroundColor;
            }

            if (text != null)
            {
                text.color = preset.TextColor;
            }

            ApplyRuntimeColors(button, preset);
        }

        public static void ApplyRuntimeColors(Button button, ButtonPreset preset)
        {
            if (button == null || preset == null) return;

            ApplyRuntimeColors(
                button,
                preset.BackgroundColor,
                preset.HoverColor,
                preset.PressedColor,
                preset.DisabledColor);
        }

        public static void ApplyRuntimeColors(Button button, Color baseColor)
        {
            if (button == null) return;

            ApplyRuntimeColors(
                button,
                baseColor,
                baseColor * 1.15f,
                baseColor * 0.85f,
                ThemeColors.StateDisabled);
        }

        private static void ApplyRuntimeColors(
            Button button,
            Color baseColor,
            Color hoverColor,
            Color pressedColor,
            Color disabledColor)
        {
            var colors = button.colors;
            colors.normalColor = baseColor;
            colors.highlightedColor = hoverColor;
            colors.selectedColor = hoverColor;
            colors.pressedColor = pressedColor;
            colors.disabledColor = disabledColor;
            button.colors = colors;
        }
    }

    /// <summary>
    /// 버튼 프리셋 데이터.
    /// </summary>
    public sealed class ButtonPreset
    {
        public Color BackgroundColor;
        public Color TextColor;
        public float MinHeight;
        public float CornerRadius;
        public Color HoverColor;
        public Color PressedColor;
        public Color DisabledColor;
        public Color BorderColor;
        public float BorderWidth;

        public ButtonPreset Clone()
        {
            return new ButtonPreset
            {
                BackgroundColor = BackgroundColor,
                TextColor = TextColor,
                MinHeight = MinHeight,
                CornerRadius = CornerRadius,
                HoverColor = HoverColor,
                PressedColor = PressedColor,
                DisabledColor = DisabledColor,
                BorderColor = BorderColor,
                BorderWidth = BorderWidth
            };
        }
    }

}
