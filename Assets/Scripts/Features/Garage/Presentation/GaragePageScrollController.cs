using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Features.Garage.Presentation
{
    internal sealed class GaragePageScrollController
    {
        public void ScrollBodyToTop(Transform mobileBodyHost)
        {
            if (mobileBodyHost == null)
            {
                return;
            }

            ScrollRect scrollRect = mobileBodyHost.GetComponentInParent<ScrollRect>();
            if (scrollRect == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            if (mobileBodyHost.TryGetComponent<RectTransform>(out var contentRoot))
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);
            }

            scrollRect.StopMovement();
            scrollRect.normalizedPosition = new Vector2(scrollRect.normalizedPosition.x, 1f);
            scrollRect.verticalNormalizedPosition = 1f;
        }
    }

    internal enum GaragePageKeyboardActionKind
    {
        None,
        Save,
        SelectSlot,
        CyclePart,
    }

    internal readonly struct GaragePageKeyboardAction
    {
        private GaragePageKeyboardAction(GaragePageKeyboardActionKind kind, int slotIndex, int delta)
        {
            Kind = kind;
            SlotIndex = slotIndex;
            Delta = delta;
        }

        public GaragePageKeyboardActionKind Kind { get; }
        public int SlotIndex { get; }
        public int Delta { get; }

        public static GaragePageKeyboardAction Save()
        {
            return new GaragePageKeyboardAction(GaragePageKeyboardActionKind.Save, -1, 0);
        }

        public static GaragePageKeyboardAction SelectSlot(int slotIndex)
        {
            return new GaragePageKeyboardAction(GaragePageKeyboardActionKind.SelectSlot, slotIndex, 0);
        }

        public static GaragePageKeyboardAction CyclePart(int delta)
        {
            return new GaragePageKeyboardAction(GaragePageKeyboardActionKind.CyclePart, -1, delta);
        }
    }

    internal static class GaragePageKeyboardShortcuts
    {
        public static bool TryGetCurrentAction(out GaragePageKeyboardAction action)
        {
            action = default;

            var keyboard = Keyboard.current;
            if (keyboard == null)
                return false;

            if ((keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed) &&
                keyboard.sKey.wasPressedThisFrame)
            {
                action = GaragePageKeyboardAction.Save();
                return true;
            }

            if (TryGetSlotIndex(keyboard, out var slotIndex))
            {
                action = GaragePageKeyboardAction.SelectSlot(slotIndex);
                return true;
            }

            var delta = GetCycleDelta(keyboard);
            if (delta == 0)
                return false;

            action = GaragePageKeyboardAction.CyclePart(delta);
            return true;
        }

        private static bool TryGetSlotIndex(Keyboard keyboard, out int slotIndex)
        {
            if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame) { slotIndex = 0; return true; }
            if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame) { slotIndex = 1; return true; }
            if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame) { slotIndex = 2; return true; }
            if (keyboard.digit4Key.wasPressedThisFrame || keyboard.numpad4Key.wasPressedThisFrame) { slotIndex = 3; return true; }
            if (keyboard.digit5Key.wasPressedThisFrame || keyboard.numpad5Key.wasPressedThisFrame) { slotIndex = 4; return true; }
            if (keyboard.digit6Key.wasPressedThisFrame || keyboard.numpad6Key.wasPressedThisFrame) { slotIndex = 5; return true; }

            slotIndex = -1;
            return false;
        }

        private static int GetCycleDelta(Keyboard keyboard)
        {
            if (keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame)
                return -1;

            return keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame
                ? 1
                : 0;
        }
    }
}
