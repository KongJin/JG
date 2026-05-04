using System;
using System.Collections.Generic;
using Shared.Ui;
using UnityEngine;
using UnityEngine.UIElements;

namespace Features.Garage.Presentation
{
    internal sealed class GarageSetBSlotSurface : BaseSurface<VisualElement>
    {
        private readonly SlotBinding[] _slots = new SlotBinding[GarageUitkConstants.Slots.MaxCount];
        private readonly Action[] _slotClicked = new Action[GarageUitkConstants.Slots.MaxCount];
        private readonly Action[] _slotClearClicked = new Action[GarageUitkConstants.Slots.MaxCount];

        public GarageSetBSlotSurface(VisualElement root)
            : base(root)
        {
            for (int i = 0; i < GarageUitkConstants.Slots.MaxCount; i++)
            {
                int slotNumber = i + 1;
                _slots[i] = new SlotBinding(
                    UitkElementUtility.Required<Button>(root, GarageUitkConstants.Slots.BuildCardName(slotNumber)),
                    UitkElementUtility.Required<Label>(root, GarageUitkConstants.Slots.BuildCodeLabelName(slotNumber)),
                    UitkElementUtility.Required<VisualElement>(root, GarageUitkConstants.Slots.BuildIconName(slotNumber)),
                    UitkElementUtility.Required<VisualElement>(root, GarageUitkConstants.Slots.BuildIconGlyphName(slotNumber)),
                    UitkElementUtility.Required<Button>(root, GarageUitkConstants.Slots.BuildClearButtonName(slotNumber)),
                    UitkElementUtility.Required<Label>(root, GarageUitkConstants.Slots.BuildNameLabelName(slotNumber)));
                _slots[i].IconHost.Insert(0, _slots[i].PreviewImage);
            }

            BindCallbacks();
        }

        public event Action<int> SlotSelected;
        public event Action<int> SlotClearRequested;

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
            if (IsDisposed)
                return;

            for (int i = 0; i < _slots.Length; i++)
            {
                int slotIndex = i;
                _slotClicked[i] ??= () => SlotSelected?.Invoke(slotIndex);
                _slotClearClicked[i] ??= () => SlotClearRequested?.Invoke(slotIndex);
                _slots[i].Card.clicked += _slotClicked[i];
                _slots[i].ClearButton.clicked += _slotClearClicked[i];
            }
        }

        protected override void DisposeSurface()
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slotClicked[i] != null)
                    _slots[i].Card.clicked -= _slotClicked[i];

                if (_slotClearClicked[i] != null)
                    _slots[i].ClearButton.clicked -= _slotClearClicked[i];
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
            binding.ClearButton.style.display = isSelected && !isEmpty ? DisplayStyle.Flex : DisplayStyle.None;
            binding.ClearButton.SetEnabled(isSelected && !isEmpty);

            UitkElementUtility.SetClass(binding.Card, GarageUitkConstants.Classes.Slot.CardActive, isSelected);
            UitkElementUtility.SetClass(binding.Card, GarageUitkConstants.Classes.Slot.CardEmpty, isEmpty);
            UitkElementUtility.SetClass(
                binding.ClearButton,
                GarageUitkConstants.Classes.Slot.ClearButtonVisible,
                isSelected && !isEmpty);
            UitkElementUtility.SetClass(binding.CodeLabel, GarageUitkConstants.Classes.Slot.CodeActive, isSelected);
            UitkElementUtility.SetClass(binding.CodeLabel, GarageUitkConstants.Classes.Slot.CodeEmpty, isEmpty);
            UitkElementUtility.SetClass(binding.IconHost, GarageUitkConstants.Classes.Slot.IconActive, isSelected);
            UitkElementUtility.SetClass(binding.IconHost, GarageUitkConstants.Classes.Slot.IconEmpty, isEmpty);
            UitkElementUtility.SetClass(binding.IconGlyph, GarageUitkConstants.Classes.Slot.IconGlyphActive, isSelected);
            UitkElementUtility.SetClass(binding.IconGlyph, GarageUitkConstants.Classes.Slot.IconGlyphEmpty, isEmpty);
            UitkElementUtility.SetClass(binding.NameLabel, GarageUitkConstants.Classes.Slot.NameActive, isSelected);
            UitkElementUtility.SetClass(binding.NameLabel, GarageUitkConstants.Classes.Slot.NameEmpty, isEmpty);
        }

        private static string BuildSlotIconId(GarageSlotViewModel slot)
        {
            if (slot == null || slot.IsEmpty)
                return GarageUitkConstants.Icons.Add;

            var role = (slot.RoleLabel ?? string.Empty).ToLowerInvariant();
            var status = (slot.StatusBadgeText ?? string.Empty).ToLowerInvariant();
            var display = (slot.Title ?? string.Empty).ToLowerInvariant();
            var combined = string.Concat(role, " ", status, " ", display);

            if (combined.Contains("방어") || combined.Contains("defense") || combined.Contains("guard") || combined.Contains("고정"))
                return GarageUitkConstants.Icons.Security;

            if (combined.Contains("지원") || combined.Contains("support") || combined.Contains("정비"))
                return GarageUitkConstants.Icons.PrecisionManufacturing;

            return GarageUitkConstants.Icons.SmartToy;
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
                Button clearButton,
                Label nameLabel)
            {
                Card = card;
                CodeLabel = codeLabel;
                IconHost = iconHost;
                IconGlyph = iconGlyph;
                ClearButton = clearButton;
                PreviewImage = UitkElementUtility.CreateAbsoluteImage($"Slot{card.name.Substring("SlotCard".Length)}PreviewImage");
                PreviewImage.scaleMode = ScaleMode.ScaleAndCrop;
                NameLabel = nameLabel;
            }

            public Button Card { get; }
            public Label CodeLabel { get; }
            public VisualElement IconHost { get; }
            public VisualElement IconGlyph { get; }
            public Button ClearButton { get; }
            public Image PreviewImage { get; }
            public Label NameLabel { get; }
        }
    }
}
