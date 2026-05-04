using UnityEngine;
using UnityEngine.UIElements;

namespace Features.Lobby.Presentation
{
    internal static class LobbySlotRowRenderer
    {
        private const string SlotChipFilledClass = "lobby-slot-chip--filled";

        public static void Render(VisualElement host, int filledSlots, int totalSlots)
        {
            if (host == null)
                return;

            host.Clear();
            var clampedTotal = Mathf.Max(0, totalSlots);
            var clampedFilled = Mathf.Clamp(filledSlots, 0, clampedTotal);
            for (var i = 0; i < clampedTotal; i++)
            {
                var chip = new VisualElement();
                chip.AddToClassList("lobby-slot-chip");
                if (i < clampedFilled)
                    chip.AddToClassList(SlotChipFilledClass);
                host.Add(chip);
            }

            host.Add(LobbyUitkElementFactory.CreateTextLabel(
                $"{clampedFilled}/{clampedTotal}",
                "lobby-slot-text"));
        }
    }
}
