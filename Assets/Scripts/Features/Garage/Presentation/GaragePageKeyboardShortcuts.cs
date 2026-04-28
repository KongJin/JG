using UnityEngine;

namespace Features.Garage.Presentation
{
    public enum GaragePageKeyboardActionKind
    {
        Save,
        SelectSlot,
        CyclePart,
    }

    public readonly struct GaragePageKeyboardAction
    {
        public GaragePageKeyboardActionKind Kind { get; }
        public int SlotIndex { get; }
        public int Delta { get; }

        private GaragePageKeyboardAction(GaragePageKeyboardActionKind kind, int slotIndex, int delta)
        {
            Kind = kind;
            SlotIndex = slotIndex;
            Delta = delta;
        }

        public static GaragePageKeyboardAction Save()
        {
            return new GaragePageKeyboardAction(GaragePageKeyboardActionKind.Save, 0, 0);
        }

        public static GaragePageKeyboardAction SelectSlot(int slotIndex)
        {
            return new GaragePageKeyboardAction(GaragePageKeyboardActionKind.SelectSlot, slotIndex, 0);
        }

        public static GaragePageKeyboardAction CyclePart(int delta)
        {
            return new GaragePageKeyboardAction(GaragePageKeyboardActionKind.CyclePart, 0, delta);
        }
    }

    public static class GaragePageKeyboardShortcuts
    {
        public static bool TryGetCurrentAction(out GaragePageKeyboardAction action)
        {
            if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) &&
                Input.GetKeyDown(KeyCode.S))
            {
                action = GaragePageKeyboardAction.Save();
                return true;
            }

            for (var i = 0; i < 4; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    action = GaragePageKeyboardAction.SelectSlot(i);
                    return true;
                }
            }

            if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.LeftArrow))
            {
                action = GaragePageKeyboardAction.CyclePart(-1);
                return true;
            }

            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                action = GaragePageKeyboardAction.CyclePart(1);
                return true;
            }

            action = default;
            return false;
        }
    }
}
