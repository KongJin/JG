using Shared.Attributes;
using UnityEngine;
using UnityEngine.UI;

namespace Features.Unit.Presentation
{
    /// <summary>
    /// 배치 실패 시 에러 메시지를 표시하는 UI View.
    /// Screen Space Canvas 하단에 텍스트로 표시된다.
    /// </summary>
    public sealed class PlacementErrorView : MonoBehaviour
    {
        [Header("References")]
        [Required, SerializeField]
        private Text _errorText;

        [Header("Animation")]
        [Required, SerializeField]
        private CanvasGroup _canvasGroup;
        [SerializeField] private Image _backgroundImage;

        [Header("Settings")]
        [SerializeField]
        private float _errorDuration = 1.75f;
        [SerializeField] private float _infoDuration = 1.35f;
        [SerializeField] private Color _errorBackgroundColor = new(0.57f, 0.13f, 0.15f, 0.92f);
        [SerializeField] private Color _infoBackgroundColor = new(0.08f, 0.22f, 0.33f, 0.92f);
        [SerializeField] private Color _errorTextColor = Color.white;
        [SerializeField] private Color _infoTextColor = new(0.86f, 0.94f, 1f, 1f);
        private Coroutine _hideCoroutine;

        private void Awake()
        {
            ApplyPresentationDefaults();
            // 초기에는 숨김 처리
            HideImmediate();
        }

        /// <summary>
        /// 에러 메시지를 표시한다.
        /// </summary>
        public void Show(string message = "배치 영역 밖입니다!")
        {
            ShowError(message);
        }

        public void ShowError(string message)
        {
            ShowInternal(message, _errorBackgroundColor, _errorTextColor, _errorDuration);
        }

        public void ShowInfo(string message)
        {
            ShowInternal(message, _infoBackgroundColor, _infoTextColor, _infoDuration);
        }

        /// <summary>
        /// 에러 메시지를 즉시 숨긴다.
        /// </summary>
        public void Hide()
        {
            if (_hideCoroutine != null)
            {
                StopCoroutine(_hideCoroutine);
                _hideCoroutine = null;
            }

            HideImmediate();
        }

        private void HideImmediate()
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
        }

        private void ShowInternal(string message, Color backgroundColor, Color textColor, float duration)
        {
            if (_errorText != null)
            {
                _errorText.text = message;
                _errorText.color = textColor;
            }

            var background = ResolveBackgroundImage();
            if (background != null)
            {
                background.color = backgroundColor;
            }

            if (_hideCoroutine != null)
            {
                StopCoroutine(_hideCoroutine);
                _hideCoroutine = null;
            }

            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = false;
            _hideCoroutine = StartCoroutine(HideAfterDelay(duration));
        }

        private Image ResolveBackgroundImage()
        {
            if (_backgroundImage != null)
                return _backgroundImage;

            _backgroundImage = GetComponent<Image>();
            return _backgroundImage;
        }

        private System.Collections.IEnumerator HideAfterDelay(float duration)
        {
            yield return new WaitForSeconds(duration);
            HideImmediate();
        }

        private void ApplyPresentationDefaults()
        {
            if (_errorText != null)
            {
                _errorText.alignment = TextAnchor.MiddleLeft;
                _errorText.fontStyle = FontStyle.Bold;
                _errorText.fontSize = 18;
            }

            var background = ResolveBackgroundImage();
            if (background != null)
            {
                background.raycastTarget = false;
            }
        }
    }
}
