using UnityEngine;

namespace Features.Garage.Presentation
{
    public sealed class GaragePageChromeBindings : MonoBehaviour
    {
        [Header("Layout")]
        [SerializeField] private Transform _mobileBodyHost;

        public Transform MobileBodyHost => _mobileBodyHost;
        public string MobileSaveStateText { get; private set; } = string.Empty;

        public void SetMobileSaveState(string value)
        {
            MobileSaveStateText = value ?? string.Empty;
        }

        internal GaragePageChromeController CreateController()
        {
            return new GaragePageChromeController();
        }
    }
}
