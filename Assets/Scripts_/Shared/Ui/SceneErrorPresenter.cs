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
        [SerializeField]
        private CanvasGroup _bannerGroup;

        [SerializeField]
        private TMP_Text _bannerMessageText;

        [Header("Modal")]
        [SerializeField]
        private CanvasGroup _modalGroup;

        [SerializeField]
        private TMP_Text _modalMessageText;

        [SerializeField]
        private Button _modalDismissButton;

        private IEventSubscriber _eventBus;
        private Coroutine _bannerCoroutine;
        private DisposableScope _disposables = new DisposableScope();

        private void Awake()
        {
            EnsureUiReferences();
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

            EnsureUiReferences();
            HideGroup(_bannerGroup);
            HideGroup(_modalGroup);

            if (_eventBus != null)
                _disposables.Dispose();

            _eventBus = eventBus;
            _disposables = new DisposableScope();
            _disposables.Add(EventBusSubscription.ForOwner(_eventBus, this));
            _eventBus.Subscribe(this, new System.Action<UiErrorRequestedEvent>(OnUiErrorRequested));

            if (_modalDismissButton == null)
            {
                Debug.LogError("[SceneErrorPresenter] ModalDismissButton is missing.", this);
                return;
            }

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

        private void EnsureUiReferences()
        {
            if (_bannerGroup == null || _bannerMessageText == null)
                CreateBannerUi();

            if (_modalGroup == null || _modalMessageText == null || _modalDismissButton == null)
                CreateModalUi();

            if (_bannerGroup == null)
                Debug.LogError("[SceneErrorPresenter] BannerGroup is missing.", this);
            if (_bannerMessageText == null)
                Debug.LogError("[SceneErrorPresenter] BannerMessageText is missing.", this);
            if (_modalGroup == null)
                Debug.LogError("[SceneErrorPresenter] ModalGroup is missing.", this);
            if (_modalMessageText == null)
                Debug.LogError("[SceneErrorPresenter] ModalMessageText is missing.", this);
            if (_modalDismissButton == null)
                Debug.LogError("[SceneErrorPresenter] ModalDismissButton is missing.", this);
        }

        private void CreateBannerUi()
        {
            var root = CreateUiObject("ErrorBannerRoot", transform as RectTransform);
            root.anchorMin = new Vector2(0f, 1f);
            root.anchorMax = new Vector2(1f, 1f);
            root.pivot = new Vector2(0.5f, 1f);
            root.anchoredPosition = Vector2.zero;
            root.sizeDelta = new Vector2(0f, 96f);

            var background = root.gameObject.AddComponent<Image>();
            background.color = new Color(0.75f, 0.15f, 0.15f, 0.92f);

            _bannerGroup = root.gameObject.AddComponent<CanvasGroup>();

            var message = CreateText(
                "Message",
                root,
                new Vector2(24f, -16f),
                new Vector2(-24f, -16f),
                TextAlignmentOptions.MidlineLeft
            );
            message.fontSize = 30f;
            _bannerMessageText = message;
        }

        private void CreateModalUi()
        {
            var overlay = CreateUiObject("ErrorModalRoot", transform as RectTransform);
            Stretch(overlay);

            var overlayImage = overlay.gameObject.AddComponent<Image>();
            overlayImage.color = new Color(0f, 0f, 0f, 0.7f);

            _modalGroup = overlay.gameObject.AddComponent<CanvasGroup>();

            var panel = CreateUiObject("Panel", overlay);
            panel.anchorMin = new Vector2(0.5f, 0.5f);
            panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(760f, 320f);
            panel.anchoredPosition = Vector2.zero;

            var panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.color = new Color(0.12f, 0.12f, 0.16f, 0.96f);

            var message = CreateText(
                "Message",
                panel,
                new Vector2(40f, -36f),
                new Vector2(-40f, -116f),
                TextAlignmentOptions.Midline
            );
            message.fontSize = 34f;
            _modalMessageText = message;

            var buttonRect = CreateUiObject("DismissButton", panel);
            buttonRect.anchorMin = new Vector2(0.5f, 0f);
            buttonRect.anchorMax = new Vector2(0.5f, 0f);
            buttonRect.pivot = new Vector2(0.5f, 0f);
            buttonRect.sizeDelta = new Vector2(220f, 64f);
            buttonRect.anchoredPosition = new Vector2(0f, 32f);

            var buttonImage = buttonRect.gameObject.AddComponent<Image>();
            buttonImage.color = new Color(0.82f, 0.82f, 0.82f, 1f);

            var button = buttonRect.gameObject.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            _modalDismissButton = button;

            var buttonLabel = CreateText(
                "Label",
                buttonRect,
                Vector2.zero,
                Vector2.zero,
                TextAlignmentOptions.Midline
            );
            buttonLabel.text = "OK";
            buttonLabel.color = new Color(0.12f, 0.12f, 0.16f, 1f);
            buttonLabel.fontSize = 30f;
        }

        private static RectTransform CreateUiObject(string name, RectTransform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.transform as RectTransform;
            rect.SetParent(parent, false);
            rect.localScale = Vector3.one;
            return rect;
        }

        private static TextMeshProUGUI CreateText(
            string name,
            RectTransform parent,
            Vector2 offsetMin,
            Vector2 offsetMax,
            TextAlignmentOptions alignment
        )
        {
            var rect = CreateUiObject(name, parent);
            Stretch(rect);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.enableWordWrapping = true;
            text.color = Color.white;
            text.alignment = alignment;
            text.text = string.Empty;
            return text;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
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
