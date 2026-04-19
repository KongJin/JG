using System;
using Features.Garage.Presentation.Theme;
using Shared.Attributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Features.Garage.Presentation
{
    public sealed class GaragePartSelectorView : MonoBehaviour
    {
        [Required, SerializeField] private Button _prevButton;
        [Required, SerializeField] private Button _nextButton;
        [Required, SerializeField] private TMP_Text _titleText;
        [Required, SerializeField] private TMP_Text _valueText;
        [Required, SerializeField] private TMP_Text _hintText;

        [Header("Layout")]
        [SerializeField] private float _titleFontSize = 16f;
        [SerializeField] private float _valueFontSize = 17f;
        [SerializeField] private float _hintFontSize = 11f;

        private bool _callbacksHooked;

        public event Action<int> CycleRequested;

        /// <summary>호버 시 부품 비교 툴팁 표시용 이벤트 (delta: -1=이전, +1=다음)</summary>
        public event Action<int> PartHoverRequested;

        private void Awake()
        {
            ApplyMobileTypography();
            ApplyButtonStyles();
        }

        private void OnEnable()
        {
            ApplyMobileTypography();
        }

        public void Bind()
        {
            if (_callbacksHooked)
                return;

            _callbacksHooked = true;
            _prevButton.onClick.AddListener(() => CycleRequested?.Invoke(-1));
            _nextButton.onClick.AddListener(() => CycleRequested?.Invoke(1));

            // 호버 시 툴팁 이벤트 연결
            AttachHoverForwarder(_prevButton, -1);
            AttachHoverForwarder(_nextButton, 1);
        }

        private void AttachHoverForwarder(Button button, int delta)
        {
            if (button == null) return;
            var forwarder = button.gameObject.GetComponent<ButtonHoverForwarder>();
            if (forwarder == null)
                forwarder = button.gameObject.AddComponent<ButtonHoverForwarder>();
            forwarder.HoverEntered += () => PartHoverRequested?.Invoke(delta);
        }

        public void Render(string valueText, string hintText)
        {
            _valueText.text = valueText;
            bool isEmpty = string.IsNullOrEmpty(valueText) || valueText.StartsWith("< ");
            _valueText.color = isEmpty ? ThemeColors.TextMuted : ThemeColors.TextPrimary;

            _hintText.text = hintText;
            _hintText.color = ThemeColors.TextSecondary;
            _titleText.color = ThemeColors.TextPrimary;
        }

        public void ApplyMobileTypography()
        {
            float titleFontSize = _titleFontSize + 2f;
            float valueFontSize = _valueFontSize + 3f;
            float hintFontSize = _hintFontSize + 1f;

            _titleText.fontSize = titleFontSize;
            _titleText.alignment = TextAlignmentOptions.TopLeft;
            _titleText.textWrappingMode = TextWrappingModes.NoWrap;
            _titleText.overflowMode = TextOverflowModes.Ellipsis;

            _valueText.fontSize = valueFontSize;
            _valueText.enableAutoSizing = true;
            _valueText.fontSizeMin = Mathf.Max(11f, valueFontSize - 4f);
            _valueText.fontSizeMax = valueFontSize;
            _valueText.alignment = TextAlignmentOptions.Center;
            _valueText.textWrappingMode = TextWrappingModes.NoWrap;
            _valueText.overflowMode = TextOverflowModes.Ellipsis;

            _hintText.fontSize = hintFontSize;
            _hintText.enableAutoSizing = false;
            _hintText.fontSizeMax = hintFontSize;
            _hintText.alignment = TextAlignmentOptions.TopLeft;
            _hintText.textWrappingMode = TextWrappingModes.NoWrap;
            _hintText.overflowMode = TextOverflowModes.Ellipsis;
        }

        /// <summary>
        /// < > 버튼에 Secondary 버튼 스타일 적용.
        /// </summary>
        private void ApplyButtonStyles()
        {
            _prevButton.Apply(ButtonStyles.Secondary);
            _nextButton.Apply(ButtonStyles.Secondary);
        }
    }

    /// <summary>
    /// 버튼 호버 시 이벤트를 발행하는 헬퍼 컴포넌트.
    /// </summary>
    internal sealed class ButtonHoverForwarder : MonoBehaviour,
        UnityEngine.EventSystems.IPointerEnterHandler
    {
        public event Action HoverEntered;

        public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData eventData)
        {
            HoverEntered?.Invoke();
        }
    }
}
