using Shared.ErrorHandling;
using Shared.EventBus;
using Shared.Lifecycle;
using UnityEngine;

namespace Shared.Ui
{
    public sealed class SceneErrorPresenter : MonoBehaviour
    {
        private IEventSubscriber _eventBus;
        private Coroutine _bannerCoroutine;
        private DisposableScope _disposables = new DisposableScope();

        public UiErrorMessage CurrentBanner { get; private set; }
        public UiErrorMessage CurrentModal { get; private set; }

        public void Initialize(IEventSubscriber eventBus)
        {
            if (eventBus == null)
            {
                Debug.LogError("[SceneErrorPresenter] EventBus is missing.", this);
                return;
            }

            CurrentBanner = default;
            CurrentModal = default;

// csharp-guardrails: allow-null-defense
            if (_eventBus != null)
                _disposables.Dispose();

            _eventBus = eventBus;
            _disposables = new DisposableScope();
            _disposables.Add(EventBusSubscription.ForOwner(_eventBus, this));
            _eventBus.Subscribe(this, new System.Action<UiErrorRequestedEvent>(OnUiErrorRequested));
        }

        private void OnDestroy()
        {
            _disposables.Dispose();
        }

        private void OnUiErrorRequested(UiErrorRequestedEvent e)
        {
            Debug.LogWarning($"[{e.Error.SourceFeature}] {e.Error.Message}", this);

            if (e.Error.DisplayMode == ErrorDisplayMode.Modal)
            {
                ShowModal(e.Error);
                return;
            }

            ShowBanner(e.Error);
        }

        private void ShowBanner(UiErrorMessage error)
        {
            EnsurePresenterActive();
            CurrentBanner = error;
            CurrentModal = default;

// csharp-guardrails: allow-null-defense
            if (_bannerCoroutine != null)
                StopCoroutine(_bannerCoroutine);
            _bannerCoroutine = StartCoroutine(HideBannerAfterDelay(error.DurationSeconds));
        }

        private System.Collections.IEnumerator HideBannerAfterDelay(float durationSeconds)
        {
            var delay = durationSeconds > 0f ? durationSeconds : 3f;
            yield return new WaitForSeconds(delay);
            CurrentBanner = default;
            _bannerCoroutine = null;
        }

        private void ShowModal(UiErrorMessage error)
        {
            EnsurePresenterActive();
            CurrentBanner = default;
            CurrentModal = error;
        }

        public void HideModal()
        {
            CurrentModal = default;
        }

        private void EnsurePresenterActive()
        {
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            if (!enabled)
                enabled = true;
        }
    }
}
