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

        [Header("Settings")]
        [SerializeField]
        private float _showDuration = 2f;
        private Coroutine _hideCoroutine;

        private void Awake()
        {
            // 초기에는 숨김 처리
            HideImmediate();
        }

        /// <summary>
        /// 에러 메시지를 표시한다.
        /// </summary>
        public void Show(string message = "배치 영역 밖입니다!")
        {
            if (_errorText != null)
            {
                _errorText.text = message;
            }

            // 이미 표시 중인 타이머 취소
            if (_hideCoroutine != null)
            {
                StopCoroutine(_hideCoroutine);
                _hideCoroutine = null;
            }

            // 표시
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;

            // 일정 시간 후 숨김
            _hideCoroutine = StartCoroutine(HideAfterDelay());
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

        private System.Collections.IEnumerator HideAfterDelay()
        {
            yield return new WaitForSeconds(_showDuration);
            HideImmediate();
        }
    }
}
