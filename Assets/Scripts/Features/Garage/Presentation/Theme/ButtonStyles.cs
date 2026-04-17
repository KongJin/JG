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

        /// <summary>저장, 주요 액션 — 파란색 배경 + 흰 텍스트</summary>
        public static readonly ButtonPreset Primary = new()
        {
            BackgroundColor = ThemeColors.AccentBlue,
            TextColor = ThemeColors.TextPrimary,
            FontSize = 16,
            MinHeight = 44,
            CornerRadius = 8f,
            HoverColor = ThemeColors.AccentBlue * 1.15f,
            PressedColor = ThemeColors.AccentBlue * 0.85f,
            DisabledColor = ThemeColors.StateDisabled
        };

        /// <summary>Clear Slot 등 파괴적 액션 — 빨간색</summary>
        public static readonly ButtonPreset Danger = new()
        {
            BackgroundColor = ThemeColors.AccentRed,
            TextColor = ThemeColors.TextPrimary,
            FontSize = 14,
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
            FontSize = 14,
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
            FontSize = 14,
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
        /// Button에 프리셋을 적용한다. Image + TMP_Text가 있어야 함.
        /// </summary>
        public static void Apply(this Button button, ButtonPreset preset)
        {
            if (button == null || preset == null) return;

            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = preset.BackgroundColor;
            }

            var text = button.GetComponentInChildren<TMP_Text>();
            if (text != null)
            {
                text.color = preset.TextColor;
                text.fontSize = preset.FontSize;
                text.alignment = TextAlignmentOptions.Center;
            }

            // 호버/클릭 시각 피드백 (EventTrigger 기반 — 런타임 전용)
            if (UnityEngine.Application.isPlaying)
            {
                ButtonFeedback.Attach(button, preset);
            }
        }
    }

    /// <summary>
    /// 버튼 프리셋 데이터.
    /// </summary>
    public sealed class ButtonPreset
    {
        public Color BackgroundColor;
        public Color TextColor;
        public int FontSize;
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
                FontSize = FontSize,
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

    /// <summary>
    /// 런타임 버튼 피드백 (호버/클릭 시 색상 변화).
    /// Editor 비플레이 모드에서는 작동하지 않음.
    /// </summary>
    internal sealed class ButtonFeedback : MonoBehaviour,
        UnityEngine.EventSystems.IPointerEnterHandler,
        UnityEngine.EventSystems.IPointerExitHandler,
        UnityEngine.EventSystems.IPointerDownHandler,
        UnityEngine.EventSystems.IPointerUpHandler
    {
        private Image _image;
        private ButtonPreset _preset;
        private Button _button;
        private Color _baseColor;

        public static void Attach(Button button, ButtonPreset preset)
        {
            if (button == null || preset == null) return;
            if (button.GetComponent<ButtonFeedback>() != null) return;

            var feedback = button.gameObject.AddComponent<ButtonFeedback>();
            feedback._image = button.GetComponent<Image>();
            feedback._preset = preset.Clone();
            feedback._button = button;
            feedback._baseColor = preset.BackgroundColor;
        }

        public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (_button.interactable && _image != null)
                _image.color = _preset.HoverColor;
        }

        public void OnPointerExit(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (_button.interactable && _image != null)
                _image.color = _baseColor;
        }

        public void OnPointerDown(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (_button.interactable && _image != null)
                _image.color = _preset.PressedColor;
        }

        public void OnPointerUp(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (_button.interactable && _image != null)
                _image.color = _preset.HoverColor;
        }

        public void UpdateBaseColor(Color color)
        {
            _baseColor = color;
            _preset.HoverColor = color * 1.15f;
            _preset.PressedColor = color * 0.85f;
        }
    }
}
