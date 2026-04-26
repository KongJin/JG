using Shared.Attributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Features.Garage.Presentation
{
    public sealed class GaragePageChromeBindings : MonoBehaviour
    {
        [Header("Layout")]
        [Required, SerializeField] private GameObject _mobileContentRoot;
        [Required, SerializeField] private Transform _mobileBodyHost;
        [Required, SerializeField] private Transform _mobileSlotHost;
        [Required, SerializeField] private GameObject _rightRailRoot;
        [Required, SerializeField] private GameObject _previewCard;
        [Required, SerializeField] private GameObject _resultPane;
        [Required, SerializeField] private GameObject _mobileTabBar;
        [Required, SerializeField] private Button _mobileEditTabButton;
        [Required, SerializeField] private TMP_Text _mobileEditTabLabel;
        [Required, SerializeField] private Button _mobileFirepowerTabButton;
        [Required, SerializeField] private TMP_Text _mobileFirepowerTabLabel;
        [Required, SerializeField] private Button _mobileSummaryTabButton;
        [Required, SerializeField] private TMP_Text _mobileSummaryTabLabel;
        [Required, SerializeField] private TMP_Text _garageHeaderSummaryText;
        [Required, SerializeField] private Button _settingsOpenButton;
        [Required, SerializeField] private TMP_Text _settingsOpenButtonLabel;
        [Required, SerializeField] private GameObject _settingsOverlayRoot;
        [Required, SerializeField] private Button _settingsCloseButton;
        [Required, SerializeField] private TMP_Text _settingsCloseButtonLabel;
        [Required, SerializeField] private GameObject _mobileSaveDockRoot;
        [Required, SerializeField] private Button _mobileSaveButton;
        [Required, SerializeField] private TMP_Text _mobileSaveButtonLabel;
        [Required, SerializeField] private TMP_Text _mobileSaveStateText;

        public Transform MobileBodyHost => _mobileBodyHost;
        public Button MobileEditTabButton => _mobileEditTabButton;
        public Button MobileFirepowerTabButton => _mobileFirepowerTabButton;
        public Button MobileSummaryTabButton => _mobileSummaryTabButton;
        public Button SettingsOpenButton => _settingsOpenButton;
        public Button SettingsCloseButton => _settingsCloseButton;
        public Button MobileSaveButton => _mobileSaveButton;

        internal GaragePageChromeController CreateController()
        {
            return new GaragePageChromeController(
                _mobileContentRoot,
                _mobileSlotHost,
                _rightRailRoot,
                _previewCard,
                _resultPane,
                _mobileTabBar,
                _mobileEditTabButton,
                _mobileEditTabLabel,
                _mobileFirepowerTabButton,
                _mobileFirepowerTabLabel,
                _mobileSummaryTabButton,
                _mobileSummaryTabLabel,
                _garageHeaderSummaryText,
                _settingsOpenButton,
                _settingsOpenButtonLabel,
                _settingsOverlayRoot,
                _settingsCloseButton,
                _settingsCloseButtonLabel,
                _mobileSaveDockRoot,
                _mobileSaveButton,
                _mobileSaveButtonLabel,
                _mobileSaveStateText);
        }
    }
}
