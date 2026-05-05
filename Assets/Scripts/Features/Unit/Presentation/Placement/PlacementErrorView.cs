using UnityEngine;

namespace Features.Unit.Presentation
{
    public sealed class PlacementErrorView : MonoBehaviour
    {
        [SerializeField] private float _errorDuration = 1.75f;
        [SerializeField] private float _infoDuration = 1.35f;

        private Coroutine _hideCoroutine;

        public string Message { get; private set; } = string.Empty;
        public bool IsVisible { get; private set; }
        public bool IsError { get; private set; }

        public void Show(string message = "배치 영역 밖입니다!")
        {
            ShowError(message);
        }

        public void ShowError(string message)
        {
            ShowInternal(message, true, _errorDuration);
        }

        public void ShowInfo(string message)
        {
            ShowInternal(message, false, _infoDuration);
        }

        public void Hide()
        {
// csharp-guardrails: allow-null-defense
            if (_hideCoroutine != null)
            {
                StopCoroutine(_hideCoroutine);
                _hideCoroutine = null;
            }

            IsVisible = false;
        }

        private void ShowInternal(string message, bool isError, float duration)
        {
            Message = message ?? string.Empty;
            IsError = isError;
            IsVisible = true;

// csharp-guardrails: allow-null-defense
            if (_hideCoroutine != null)
                StopCoroutine(_hideCoroutine);

            _hideCoroutine = StartCoroutine(HideAfterDelay(duration));
        }

        private System.Collections.IEnumerator HideAfterDelay(float duration)
        {
            yield return new WaitForSeconds(duration);
            IsVisible = false;
            _hideCoroutine = null;
        }
    }
}
