using Shared.Attributes;
using Shared.ErrorHandling;
using Shared.EventBus;
using Shared.Lifecycle;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shared.Ui
{
    public sealed class SceneErrorPresenter : MonoBehaviour
    {
        [Header("Banner")]
        [Required, SerializeField]
        private CanvasGroup _bannerGroup;

        [Required, SerializeField]
        private TMP_Text _bannerMessageText;

        [Header("Modal")]
        [Required, SerializeField]
        private CanvasGroup _modalGroup;

        [Required, SerializeField]
        private TMP_Text _modalMessageText;

        [Required, SerializeField]
        private Button _modalDismissButton;

        private IEventSubscriber _eventBus;
        private Coroutine _bannerCoroutine;
        private DisposableScope _disposables = new DisposableScope();

        private void Awake()
        {
            HideGroup(_bannerGroup);
            HideGroup(_modalGroup);
        }

        public void Initialize(IEventSubscriber eventBus)
        {
            if (eventBus == null)
            {
                Debug.LogError("[SceneErrorPresenter] EventBus is missing.", this);
                return;
            }

            HideGroup(_bannerGroup);
            HideGroup(_modalGroup);

            if (_eventBus != null)
                _disposables.Dispose();

            _eventBus = eventBus;
            _disposables = new DisposableScope();
            _disposables.Add(EventBusSubscription.ForOwner(_eventBus, this));
            _eventBus.Subscribe(this, new System.Action<UiErrorRequestedEvent>(OnUiErrorRequested));

            _modalDismissButton.onClick.RemoveListener(HideModal);
            _modalDismissButton.onClick.AddListener(HideModal);
            _disposables.Add(() => _modalDismissButton.onClick.RemoveListener(HideModal));
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
            _bannerMessageText.text = error.Message;
            ShowBannerGroup(_bannerGroup);

            if (_bannerCoroutine != null)
                StopCoroutine(_bannerCoroutine);
            _bannerCoroutine = StartCoroutine(HideBannerAfterDelay(error.DurationSeconds));
        }

        private System.Collections.IEnumerator HideBannerAfterDelay(float durationSeconds)
        {
            var delay = durationSeconds > 0f ? durationSeconds : 3f;
            yield return new WaitForSeconds(delay);
            HideGroup(_bannerGroup);
            _bannerCoroutine = null;
        }

        private void ShowModal(UiErrorMessage error)
        {
            _modalMessageText.text = error.Message;
            _modalDismissButton.gameObject.SetActive(error.CanDismiss);
            HideGroup(_bannerGroup);
            ShowGroup(_modalGroup);
        }

        private void HideModal()
        {
            HideGroup(_modalGroup);
        }

        private static void ShowGroup(CanvasGroup group)
        {
            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;
            group.gameObject.SetActive(true);
        }

        private static void ShowBannerGroup(CanvasGroup group)
        {
            group.alpha = 1f;
            group.interactable = false;
            group.blocksRaycasts = false;
            group.gameObject.SetActive(true);
        }

        private static void HideGroup(CanvasGroup group)
        {
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            group.gameObject.SetActive(false);
        }
    }
}
