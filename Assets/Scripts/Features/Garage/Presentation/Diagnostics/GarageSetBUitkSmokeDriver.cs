#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

namespace Features.Garage.Presentation
{
    public sealed class GarageSetBUitkSmokeDriver : MonoBehaviour
    {
        [SerializeField] private GarageSetBUitkPageController _controller;

        [Header("Smoke State")]
        [SerializeField] private string _lastRenderStatus;
        [SerializeField] private string _lastInteractionStatus;
        [SerializeField] private int _selectedSlotIndex;
        [SerializeField] private GarageEditorFocus _focusedPart = GarageEditorFocus.Mobility;
        [SerializeField] private string _partSearchText = string.Empty;
        [SerializeField] private bool _isSettingsOpen;

        private bool _isSubscribed;

        private void OnEnable()
        {
            Subscribe();
            SyncSnapshot();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void SelectSlotForMcpSmoke(int slotIndex)
        {
            if (!CanDrive())
                return;

            _controller.SelectSlot(slotIndex);
            _lastInteractionStatus = $"slot:{slotIndex}";
            SyncSnapshot();
        }

        public void SelectFocusForMcpSmoke(string focus)
        {
            if (!CanDrive())
                return;

            if (System.Enum.TryParse(focus, ignoreCase: true, out GarageEditorFocus parsed))
            {
                _controller.SetFocusedPart(parsed);
                _lastInteractionStatus = $"focus:{parsed}";
                SyncSnapshot();
                return;
            }

            _lastInteractionStatus = $"focus-invalid:{focus}";
        }

        public void ToggleSettingsForMcpSmoke()
        {
            if (!CanDrive())
                return;

            _controller.ToggleSettings();
            _lastInteractionStatus = $"settings:{_controller.CurrentSnapshot.IsSettingsOpen}";
            SyncSnapshot();
        }

        public void RequestSaveForMcpSmoke()
        {
            if (!CanDrive())
                return;

            _controller.RequestSave();
            _lastInteractionStatus = "save-requested";
            SyncSnapshot();
        }

        public void SetPartSearchForMcpSmoke(string value)
        {
            if (!CanDrive())
                return;

            _controller.SetPartSearchText(value);
            _lastInteractionStatus = $"part-search:{value}";
            SyncSnapshot();
        }

        public void SelectVisiblePartForMcpSmoke(string slot, int visibleIndex)
        {
            if (!CanDrive())
                return;

            if (!System.Enum.TryParse(slot, ignoreCase: true, out GarageNovaPartPanelSlot parsedSlot))
            {
                _lastInteractionStatus = $"part-slot-invalid:{slot}";
                return;
            }

            bool selected = _controller.TrySelectVisiblePart(
                parsedSlot,
                visibleIndex,
                out var selection,
                out bool hasOptions);

            if (selected)
            {
                _lastInteractionStatus = $"part:{selection.Slot}:{selection.PartId}";
            }
            else if (hasOptions)
            {
                _lastInteractionStatus = $"part-not-ready:{parsedSlot}";
            }
            else
            {
                _lastInteractionStatus = $"part-empty:{parsedSlot}";
            }

            SyncSnapshot();
        }

        private void Subscribe()
        {
            if (_isSubscribed || _controller == null)
                return;

            _controller.Rendered += OnRendered;
            _controller.SaveCompleted += OnSaveCompleted;
            _isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_isSubscribed || _controller == null)
                return;

            _controller.Rendered -= OnRendered;
            _controller.SaveCompleted -= OnSaveCompleted;
            _isSubscribed = false;
        }

        private bool CanDrive()
        {
            if (_controller != null)
                return true;

            _lastInteractionStatus = "controller-missing";
            return false;
        }

        private void OnRendered(GarageSetBUitkPageSnapshot snapshot)
        {
            ApplySnapshot(snapshot);
        }

        private void OnSaveCompleted(GarageSaveFlowResultKind resultKind)
        {
            _lastInteractionStatus = resultKind == GarageSaveFlowResultKind.Saved
                ? "save:saved"
                : $"save:{resultKind}";
        }

        private void SyncSnapshot()
        {
            if (_controller == null)
                return;

            ApplySnapshot(_controller.CurrentSnapshot);
        }

        private void ApplySnapshot(GarageSetBUitkPageSnapshot snapshot)
        {
            _lastRenderStatus = snapshot.RenderStatus ?? string.Empty;
            _selectedSlotIndex = snapshot.SelectedSlotIndex;
            _focusedPart = snapshot.FocusedPart;
            _partSearchText = snapshot.PartSearchText ?? string.Empty;
            _isSettingsOpen = snapshot.IsSettingsOpen;
        }
    }
}
#else
using UnityEngine;

namespace Features.Garage.Presentation
{
    public sealed class GarageSetBUitkSmokeDriver : MonoBehaviour
    {
    }
}
#endif
