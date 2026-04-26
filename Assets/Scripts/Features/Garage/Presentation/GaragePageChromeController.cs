using Features.Garage.Presentation.Theme;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Features.Garage.Presentation
{
    internal sealed class GaragePageChromeController
    {
        private readonly GameObject _mobileContentRoot;
        private readonly Transform _mobileSlotHost;
        private readonly GameObject _rightRailRoot;
        private readonly GameObject _previewCard;
        private readonly GameObject _resultPane;
        private readonly GameObject _mobileTabBar;
        private readonly Button _mobileEditTabButton;
        private readonly TMP_Text _mobileEditTabLabel;
        private readonly Button _mobileFirepowerTabButton;
        private readonly TMP_Text _mobileFirepowerTabLabel;
        private readonly Button _mobileSummaryTabButton;
        private readonly TMP_Text _mobileSummaryTabLabel;
        private readonly TMP_Text _garageHeaderSummaryText;
        private readonly Button _settingsOpenButton;
        private readonly TMP_Text _settingsOpenButtonLabel;
        private readonly GameObject _settingsOverlayRoot;
        private readonly Button _settingsCloseButton;
        private readonly TMP_Text _settingsCloseButtonLabel;
        private readonly GameObject _mobileSaveDockRoot;
        private readonly Button _mobileSaveButton;
        private readonly TMP_Text _mobileSaveButtonLabel;
        private readonly TMP_Text _mobileSaveStateText;

        public GaragePageChromeController(
            GameObject mobileContentRoot,
            Transform mobileSlotHost,
            GameObject rightRailRoot,
            GameObject previewCard,
            GameObject resultPane,
            GameObject mobileTabBar,
            Button mobileEditTabButton,
            TMP_Text mobileEditTabLabel,
            Button mobileFirepowerTabButton,
            TMP_Text mobileFirepowerTabLabel,
            Button mobileSummaryTabButton,
            TMP_Text mobileSummaryTabLabel,
            TMP_Text garageHeaderSummaryText,
            Button settingsOpenButton,
            TMP_Text settingsOpenButtonLabel,
            GameObject settingsOverlayRoot,
            Button settingsCloseButton,
            TMP_Text settingsCloseButtonLabel,
            GameObject mobileSaveDockRoot,
            Button mobileSaveButton,
            TMP_Text mobileSaveButtonLabel,
            TMP_Text mobileSaveStateText)
        {
            _mobileContentRoot = mobileContentRoot;
            _mobileSlotHost = mobileSlotHost;
            _rightRailRoot = rightRailRoot;
            _previewCard = previewCard;
            _resultPane = resultPane;
            _mobileTabBar = mobileTabBar;
            _mobileEditTabButton = mobileEditTabButton;
            _mobileEditTabLabel = mobileEditTabLabel;
            _mobileFirepowerTabButton = mobileFirepowerTabButton;
            _mobileFirepowerTabLabel = mobileFirepowerTabLabel;
            _mobileSummaryTabButton = mobileSummaryTabButton;
            _mobileSummaryTabLabel = mobileSummaryTabLabel;
            _garageHeaderSummaryText = garageHeaderSummaryText;
            _settingsOpenButton = settingsOpenButton;
            _settingsOpenButtonLabel = settingsOpenButtonLabel;
            _settingsOverlayRoot = settingsOverlayRoot;
            _settingsCloseButton = settingsCloseButton;
            _settingsCloseButtonLabel = settingsCloseButtonLabel;
            _mobileSaveDockRoot = mobileSaveDockRoot;
            _mobileSaveButton = mobileSaveButton;
            _mobileSaveButtonLabel = mobileSaveButtonLabel;
            _mobileSaveStateText = mobileSaveStateText;
        }

        public void ApplyState(
            Transform pageRoot,
            bool isSettingsOverlayOpen,
            bool isSaving,
            bool isEditActive,
            bool isFirepowerActive,
            bool isSummaryActive,
            int selectedSlotIndex,
            int committedRosterCount,
            GarageResultViewModel resultViewModel)
        {
            ApplyLayout(pageRoot, isSettingsOverlayOpen);
            RefreshTabs(isEditActive, isFirepowerActive, isSummaryActive);
            RefreshHeaderSummary(BuildHeaderSummary(
                selectedSlotIndex,
                committedRosterCount,
                isEditActive,
                isFirepowerActive,
                resultViewModel));
            RefreshSettings(isSettingsOverlayOpen);
            RefreshSave(resultViewModel, isSaving);
        }

        public void HideSettingsOverlay()
        {
            SetActive(_settingsOverlayRoot, false);
        }

        private void ApplyLayout(Transform pageRoot, bool isSettingsOverlayOpen)
        {
            GameObject legacyContentRowRoot = pageRoot != null ? pageRoot.Find("GarageContentRow")?.gameObject : null;
            SetActive(_mobileContentRoot, true);
            SetActive(legacyContentRowRoot, false);
            SetActive(_mobileSlotHost != null ? _mobileSlotHost.gameObject : null, true);
            SetActive(_mobileTabBar, true);
            SetActive(_mobileSaveDockRoot, true);
            SetActive(_rightRailRoot, false);
            SetActive(_previewCard, true);
            SetActive(_resultPane, true);
            SetActive(_settingsOverlayRoot, isSettingsOverlayOpen);
        }

        private void RefreshTabs(bool isEditActive, bool isFirepowerActive, bool isSummaryActive)
        {
            ConfigureTabButton(_mobileEditTabButton, _mobileEditTabLabel, "프레임", isEditActive, true);
            ConfigureTabButton(_mobileFirepowerTabButton, _mobileFirepowerTabLabel, "무장", isFirepowerActive, true);
            ConfigureTabButton(_mobileSummaryTabButton, _mobileSummaryTabLabel, "기동", isSummaryActive, true);
        }

        private void RefreshHeaderSummary(string headerSummary)
        {
            if (_garageHeaderSummaryText == null)
            {
                return;
            }

            _garageHeaderSummaryText.text = headerSummary;
            _garageHeaderSummaryText.color = ThemeColors.TextSecondary;
        }

        private static string BuildHeaderSummary(
            int selectedSlotIndex,
            int committedRosterCount,
            bool isEditActive,
            bool isFirepowerActive,
            GarageResultViewModel resultViewModel)
        {
            string readySummary = resultViewModel != null && resultViewModel.IsReady
                ? "저장본 최신"
                : resultViewModel != null && resultViewModel.IsDirty
                    ? resultViewModel.CanSave
                        ? "저장 가능"
                        : "조립 진행 중"
                    : $"활성 {committedRosterCount}/6";
            string focusSummary = isEditActive
                ? "프레임 포커스"
                : isFirepowerActive
                    ? "무장 포커스"
                    : "기동 포커스";

            return $"UNIT {selectedSlotIndex + 1:00}  |  {focusSummary}  |  {readySummary}";
        }

        private void RefreshSettings(bool isSettingsOverlayOpen)
        {
            _settingsOpenButton.Apply(ButtonStyles.Ghost, _settingsOpenButtonLabel);
            _settingsOpenButton.interactable = !isSettingsOverlayOpen;
            _settingsOpenButtonLabel.text = "⚙";

            _settingsCloseButton.Apply(ButtonStyles.Secondary, _settingsCloseButtonLabel);
            _settingsCloseButton.interactable = isSettingsOverlayOpen;
            _settingsCloseButtonLabel.text = "닫기";
        }

        private void RefreshSave(GarageResultViewModel resultViewModel, bool isSaving)
        {
            bool canSave = resultViewModel != null && resultViewModel.CanSave && !isSaving;
            bool isDirty = resultViewModel != null && resultViewModel.IsDirty;
            bool isReady = resultViewModel != null && resultViewModel.IsReady;

            SetActive(_mobileSaveButton.gameObject, true);
            _mobileSaveButton.Apply(ButtonStyles.Primary, _mobileSaveButtonLabel);
            _mobileSaveButton.interactable = canSave;
            _mobileSaveButtonLabel.text = isSaving ? "저장 중..." : "저장 및 배치";

            if (_mobileSaveButton.TryGetComponent<Image>(out var background))
            {
                Color readyBaseline = Color.Lerp(ThemeColors.AccentOrange, ThemeColors.BackgroundCard, 0.32f);
                background.color = isSaving
                    ? ThemeColors.AccentOrange
                    : canSave
                        ? ThemeColors.AccentOrange
                        : isDirty
                            ? ThemeColors.BackgroundCard
                            : isReady
                                ? readyBaseline
                                : ThemeColors.StateDisabled;

                var feedback = _mobileSaveButton.GetComponent<ButtonFeedback>();
                if (feedback != null)
                {
                    feedback.UpdateBaseColor(background.color);
                }
            }

            RefreshSaveStateText(resultViewModel, isSaving);
        }

        private void RefreshSaveStateText(GarageResultViewModel resultViewModel, bool isSaving)
        {
            if (isSaving)
            {
                _mobileSaveStateText.text = "빌드 동기화 중...";
                _mobileSaveStateText.color = ThemeColors.TextPrimary;
                return;
            }

            if (resultViewModel == null)
            {
                _mobileSaveStateText.text = string.Empty;
                return;
            }

            if (resultViewModel.IsDirty && resultViewModel.CanSave)
            {
                _mobileSaveStateText.text = "현재 조합을 저장해 출격 편성에 반영";
                _mobileSaveStateText.color = ThemeColors.AccentAmber;
                return;
            }

            if (resultViewModel.IsDirty)
            {
                _mobileSaveStateText.text = resultViewModel.ValidationText;
                _mobileSaveStateText.color = ThemeColors.TextSecondary;
                return;
            }

            if (resultViewModel.IsReady)
            {
                _mobileSaveStateText.text = "저장본이 최신입니다 | 룸 패널에서 바로 출격 가능";
                _mobileSaveStateText.color = ThemeColors.AccentGreen;
                return;
            }

            _mobileSaveStateText.text = resultViewModel.ValidationText;
            _mobileSaveStateText.color = ThemeColors.TextSecondary;
        }

        private static void ConfigureTabButton(
            Button button,
            TMP_Text label,
            string title,
            bool isActive,
            bool isAvailable)
        {
            var preset = isActive ? ButtonStyles.Primary : ButtonStyles.Secondary;
            button.Apply(preset, label);
            button.interactable = isAvailable && !isActive;

            label.text = title;
            label.color = isAvailable
                ? isActive ? ThemeColors.TextPrimary : ThemeColors.TextSecondary
                : ThemeColors.TextMuted;

            if (button.TryGetComponent<Image>(out var background))
            {
                background.color = !isAvailable
                    ? ThemeColors.StateDisabled
                    : isActive
                        ? ThemeColors.AccentBlue
                        : ThemeColors.BackgroundCard;

                var feedback = button.GetComponent<ButtonFeedback>();
                if (feedback != null)
                {
                    feedback.UpdateBaseColor(background.color);
                }
            }
        }

        private static void SetActive(GameObject target, bool isActive)
        {
            if (target != null && target.activeSelf != isActive)
            {
                target.SetActive(isActive);
            }
        }
    }
}
