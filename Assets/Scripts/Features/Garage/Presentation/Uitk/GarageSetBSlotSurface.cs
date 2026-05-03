using System;
using System.Collections.Generic;
using Shared.Ui;
using UnityEngine;
using UnityEngine.UIElements;

namespace Features.Garage.Presentation
{
    internal sealed class GarageSetBSlotSurface
    {
        private const int SlotCount = 8;

        private readonly SlotBinding[] _slots = new SlotBinding[SlotCount];

        public GarageSetBSlotSurface(VisualElement root)
        {
            for (int i = 0; i < SlotCount; i++)
            {
                int slotNumber = i + 1;
                _slots[i] = new SlotBinding(
                    UitkElementUtility.Required<Button>(root, $"SlotCard{slotNumber:00}"),
                    UitkElementUtility.Required<Label>(root, $"SlotCode{slotNumber:00}Label"),
                    UitkElementUtility.Required<VisualElement>(root, $"SlotIcon{slotNumber:00}"),
                    UitkElementUtility.Required<VisualElement>(root, $"SlotIcon{slotNumber:00}Glyph"),
                    UitkElementUtility.Required<Label>(root, $"SlotName{slotNumber:00}Label"));
                _slots[i].IconHost.Insert(0, _slots[i].PreviewImage);
            }

            BindCallbacks();
        }

        public event Action<int> SlotSelected;

        public void Render(IReadOnlyList<GarageSlotViewModel> slots)
        {
            Render(slots, null);
        }

        public void Render(IReadOnlyList<GarageSlotViewModel> slots, IReadOnlyList<Texture> slotPreviewTextures)
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                var slot = slots != null && i < slots.Count ? slots[i] : null;
                var previewTexture = slotPreviewTextures != null && i < slotPreviewTextures.Count
                    ? slotPreviewTextures[i]
                    : null;
                RenderSlot(_slots[i], slot, previewTexture, i);
            }
        }

        private void BindCallbacks()
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                int slotIndex = i;
                _slots[i].Card.clicked += () => SlotSelected?.Invoke(slotIndex);
            }
        }

        private static void RenderSlot(SlotBinding binding, GarageSlotViewModel slot, Texture previewTexture, int slotIndex)
        {
            bool isEmpty = slot == null || slot.IsEmpty;
            bool isSelected = slot != null && slot.IsSelected;
            bool hasPreview = !isEmpty && previewTexture != null;

            binding.CodeLabel.text = slot?.SlotLabel ?? $"UNIT_{slotIndex + 1:00}";
            binding.CodeLabel.style.display = DisplayStyle.None;
            UitkIconRegistry.Apply(binding.IconGlyph, BuildSlotIconId(slot));
            binding.NameLabel.text = BuildSlotName(slot, slotIndex);
            binding.NameLabel.style.display = DisplayStyle.Flex;
            binding.PreviewImage.image = hasPreview ? previewTexture : null;
            binding.PreviewImage.style.display = hasPreview ? DisplayStyle.Flex : DisplayStyle.None;
            binding.IconGlyph.style.display = hasPreview ? DisplayStyle.None : DisplayStyle.Flex;

            UitkElementUtility.SetClass(binding.Card, "slot-card--active", isSelected);
            UitkElementUtility.SetClass(binding.Card, "slot-card--empty", isEmpty);
            UitkElementUtility.SetClass(binding.CodeLabel, "slot-code--active", isSelected);
            UitkElementUtility.SetClass(binding.CodeLabel, "slot-code--empty", isEmpty);
            UitkElementUtility.SetClass(binding.IconHost, "slot-icon--active", isSelected);
            UitkElementUtility.SetClass(binding.IconHost, "slot-icon--empty", isEmpty);
            UitkElementUtility.SetClass(binding.IconGlyph, "slot-icon-glyph--active", isSelected);
            UitkElementUtility.SetClass(binding.IconGlyph, "slot-icon-glyph--empty", isEmpty);
            UitkElementUtility.SetClass(binding.NameLabel, "slot-name--active", isSelected);
            UitkElementUtility.SetClass(binding.NameLabel, "slot-name--empty", isEmpty);
        }

        private static string BuildSlotIconId(GarageSlotViewModel slot)
        {
            if (slot == null || slot.IsEmpty)
                return "add";

            var role = (slot.RoleLabel ?? string.Empty).ToLowerInvariant();
            var status = (slot.StatusBadgeText ?? string.Empty).ToLowerInvariant();
            var display = (slot.Title ?? string.Empty).ToLowerInvariant();
            var combined = string.Concat(role, " ", status, " ", display);

            if (combined.Contains("방어") || combined.Contains("defense") || combined.Contains("guard") || combined.Contains("고정"))
                return "security";

            if (combined.Contains("지원") || combined.Contains("support") || combined.Contains("정비"))
                return "precision_manufacturing";

            return "smart_toy";
        }

        private static string BuildSlotName(GarageSlotViewModel slot, int slotIndex)
        {
            if (slot == null || slot.IsEmpty || string.IsNullOrWhiteSpace(slot.FirepowerId))
                return slot?.SlotLabel ?? $"A-{slotIndex + 1:00}";

            string summary = slot.Summary ?? string.Empty;
            int roleSeparatorIndex = summary.LastIndexOf('|');
            if (roleSeparatorIndex >= 0 && roleSeparatorIndex + 1 < summary.Length)
                summary = summary.Substring(roleSeparatorIndex + 1);

            int separatorIndex = summary.IndexOf('/');
            string weaponName = separatorIndex >= 0
                ? summary.Substring(0, separatorIndex)
                : summary;

            return weaponName.Trim();
        }

        private readonly struct SlotBinding
        {
            public SlotBinding(
                Button card,
                Label codeLabel,
                VisualElement iconHost,
                VisualElement iconGlyph,
                Label nameLabel)
            {
                Card = card;
                CodeLabel = codeLabel;
                IconHost = iconHost;
                IconGlyph = iconGlyph;
                PreviewImage = UitkElementUtility.CreateAbsoluteImage($"Slot{card.name.Substring("SlotCard".Length)}PreviewImage");
                PreviewImage.scaleMode = ScaleMode.ScaleAndCrop;
                NameLabel = nameLabel;
            }

            public Button Card { get; }
            public Label CodeLabel { get; }
            public VisualElement IconHost { get; }
            public VisualElement IconGlyph { get; }
            public Image PreviewImage { get; }
            public Label NameLabel { get; }
        }
    }
}
