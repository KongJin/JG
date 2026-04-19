using UnityEngine.InputSystem;

namespace Features.Garage.Presentation
{
    internal sealed class GarageKeyboardInputHandler
    {
        public void Process(
            Keyboard keyboard,
            GaragePageState state,
            System.Action onSaveRequested,
            System.Action<int> selectSlot,
            System.Action<int> cycleFrame,
            System.Action<int> cycleFirepower,
            System.Action<int> cycleMobility)
        {
            if (keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed)
            {
                if (keyboard.sKey.wasPressedThisFrame)
                {
                    onSaveRequested?.Invoke();
                    return;
                }
            }

            if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame) { selectSlot(0); return; }
            if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame) { selectSlot(1); return; }
            if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame) { selectSlot(2); return; }
            if (keyboard.digit4Key.wasPressedThisFrame || keyboard.numpad4Key.wasPressedThisFrame) { selectSlot(3); return; }
            if (keyboard.digit5Key.wasPressedThisFrame || keyboard.numpad5Key.wasPressedThisFrame) { selectSlot(4); return; }
            if (keyboard.digit6Key.wasPressedThisFrame || keyboard.numpad6Key.wasPressedThisFrame) { selectSlot(5); return; }

            int delta = 0;
            if (keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame) delta = -1;
            else if (keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame) delta = 1;

            if (delta == 0)
                return;

            if (!string.IsNullOrEmpty(state.EditingFrameId)) { cycleFrame(delta); return; }
            if (!string.IsNullOrEmpty(state.EditingFirepowerId)) { cycleFirepower(delta); return; }
            if (!string.IsNullOrEmpty(state.EditingMobilityId)) { cycleMobility(delta); return; }
            cycleFrame(delta);
        }
    }
}
