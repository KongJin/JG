using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Features.Garage.Presentation
{
    public sealed class GarageSetBUitkSurface
    {
        private const int SlotCount = 4;

        private readonly Label _commandStatusLabel;
        private readonly Button _settingsButton;
        private readonly SlotBinding[] _slots = new SlotBinding[SlotCount];
        private readonly Button _frameTabButton;
        private readonly Button _firepowerTabButton;
        private readonly Button _mobilityTabButton;
        private readonly Label _focusedPartBadgeLabel;
        private readonly Label _focusedPartTitleLabel;
        private readonly Label _focusedPartDescriptionLabel;
        private readonly Label _focusedPartIconLabel;
        private readonly Label _previewTitleLabel;
        private readonly VisualElement _unitPreviewHost;
        private readonly Label _unitPreviewLabel;
        private readonly Image _unitPreviewImage;
        private readonly Button _saveButton;

        public GarageSetBUitkSurface(VisualElement root)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));

            _commandStatusLabel = Required<Label>(root, "CommandStatusLabel");
            _settingsButton = Required<Button>(root, "SettingsButton");
            for (int i = 0; i < SlotCount; i++)
            {
                int slotNumber = i + 1;
                _slots[i] = new SlotBinding(
                    Required<Button>(root, $"SlotCard{slotNumber:00}"),
                    Required<Label>(root, $"SlotCode{slotNumber:00}Label"),
                    Required<VisualElement>(root, $"SlotIcon{slotNumber:00}"),
                    Required<Label>(root, $"SlotIcon{slotNumber:00}Label"),
                    Required<Label>(root, $"SlotName{slotNumber:00}Label"));
            }

            _frameTabButton = Required<Button>(root, "FrameTabButton");
            _firepowerTabButton = Required<Button>(root, "FirepowerTabButton");
            _mobilityTabButton = Required<Button>(root, "MobilityTabButton");
            _focusedPartBadgeLabel = Required<Label>(root, "FocusedPartBadgeLabel");
            _focusedPartTitleLabel = Required<Label>(root, "FocusedPartTitleLabel");
            _focusedPartDescriptionLabel = Required<Label>(root, "FocusedPartDescriptionLabel");
            _focusedPartIconLabel = Required<Label>(root, "FocusedPartIconLabel");
            _previewTitleLabel = Required<Label>(root, "PreviewTitleLabel");
            _unitPreviewHost = Required<VisualElement>(root, "UnitPreviewHost");
            _unitPreviewLabel = Required<Label>(root, "UnitPreviewLabel");
            _unitPreviewImage = CreatePreviewImage();
            _unitPreviewHost.Insert(0, _unitPreviewImage);
            _saveButton = Required<Button>(root, "SaveButton");

            BindCallbacks();
            SetPreviewTexture(null, false);
        }

        public event Action<int> SlotSelected;
        public event Action<GarageEditorFocus> PartFocusSelected;
        public event Action SaveRequested;
        public event Action SettingsRequested;

        public void Render(
            IReadOnlyList<GarageSlotViewModel> slots,
            GarageEditorViewModel editor,
            GarageResultViewModel result,
            GarageEditorFocus focusedPart,
            bool isSaving)
        {
            RenderSlots(slots);
            RenderFocusedPart(editor, focusedPart);
            RenderResult(result, isSaving);
            RenderFocusTabs(focusedPart);
        }

        public void SetPreviewTexture(Texture texture, bool isVisible)
        {
            if (_unitPreviewImage == null || _unitPreviewLabel == null)
                return;

            _unitPreviewImage.image = isVisible ? texture : null;
            _unitPreviewImage.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
            _unitPreviewLabel.style.display = isVisible ? DisplayStyle.None : DisplayStyle.Flex;
            _previewTitleLabel.text = isVisible ? "UNIT PREVIEW" : "BLUEPRINT VIEW";
        }

        private void BindCallbacks()
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                int slotIndex = i;
                _slots[i].Card.clicked += () => SlotSelected?.Invoke(slotIndex);
            }

            _frameTabButton.clicked += () => PartFocusSelected?.Invoke(GarageEditorFocus.Frame);
            _firepowerTabButton.clicked += () => PartFocusSelected?.Invoke(GarageEditorFocus.Firepower);
            _mobilityTabButton.clicked += () => PartFocusSelected?.Invoke(GarageEditorFocus.Mobility);
            _saveButton.clicked += () => SaveRequested?.Invoke();
            _settingsButton.clicked += () => SettingsRequested?.Invoke();
        }

        private void RenderSlots(IReadOnlyList<GarageSlotViewModel> slots)
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                var slot = slots != null && i < slots.Count ? slots[i] : null;
                RenderSlot(_slots[i], slot, i);
            }
        }

        private static void RenderSlot(SlotBinding binding, GarageSlotViewModel slot, int slotIndex)
        {
            bool isEmpty = slot == null || slot.IsEmpty;
            bool isSelected = slot != null && slot.IsSelected;

            binding.CodeLabel.text = slot?.SlotLabel ?? $"UNIT_{slotIndex + 1:00}";
            binding.IconLabel.text = BuildSlotIcon(slot, slotIndex);
            binding.NameLabel.text = BuildSlotName(slot);

            SetClass(binding.Card, "slot-card--active", isSelected);
            SetClass(binding.Card, "slot-card--empty", isEmpty);
            SetClass(binding.CodeLabel, "slot-code--active", isSelected);
            SetClass(binding.CodeLabel, "slot-code--empty", isEmpty);
            SetClass(binding.IconHost, "slot-icon--active", isSelected);
            SetClass(binding.IconHost, "slot-icon--empty", isEmpty);
            SetClass(binding.IconLabel, "slot-icon-label--active", isSelected);
            SetClass(binding.IconLabel, "slot-icon-label--empty", isEmpty);
            SetClass(binding.NameLabel, "slot-name--active", isSelected);
            SetClass(binding.NameLabel, "slot-name--empty", isEmpty);
        }

        private void RenderFocusedPart(GarageEditorViewModel editor, GarageEditorFocus focusedPart)
        {
            var part = FocusedPartText.From(editor, focusedPart);
            _focusedPartBadgeLabel.text = part.Badge;
            _focusedPartTitleLabel.text = part.Title;
            _focusedPartDescriptionLabel.text = part.Description;
            _focusedPartIconLabel.text = part.Icon;
        }

        private void RenderResult(GarageResultViewModel result, bool isSaving)
        {
            _commandStatusLabel.text = result?.RosterStatusText ?? "COMMAND_STATUS: 대기";
            _saveButton.text = isSaving ? "저장 중..." : result?.PrimaryActionLabel ?? "저장 및 배치";
            _saveButton.SetEnabled(!isSaving && result?.CanSave == true);
        }

        private void RenderFocusTabs(GarageEditorFocus focusedPart)
        {
            SetClass(_frameTabButton, "focus-tab--active", focusedPart == GarageEditorFocus.Frame);
            SetClass(_firepowerTabButton, "focus-tab--active", focusedPart == GarageEditorFocus.Firepower);
            SetClass(_mobilityTabButton, "focus-tab--active", focusedPart == GarageEditorFocus.Mobility);
        }

        private static T Required<T>(VisualElement root, string name) where T : VisualElement
        {
            var element = root.Q<T>(name);
            if (element == null)
                throw new InvalidOperationException($"Garage SetB UITK element not found: {name}");

            return element;
        }

        private static void SetClass(VisualElement element, string className, bool enabled)
        {
            if (enabled)
                element.AddToClassList(className);
            else
                element.RemoveFromClassList(className);
        }

        private static Image CreatePreviewImage()
        {
            var image = new Image
            {
                name = "RuntimeUnitPreviewImage",
                pickingMode = PickingMode.Ignore,
                scaleMode = ScaleMode.ScaleToFit
            };

            image.style.position = Position.Absolute;
            image.style.left = 0;
            image.style.right = 0;
            image.style.top = 0;
            image.style.bottom = 0;
            return image;
        }

        private static string BuildSlotIcon(GarageSlotViewModel slot, int slotIndex)
        {
            if (slot == null || slot.IsEmpty)
                return "+";

            if (!string.IsNullOrWhiteSpace(slot.Callsign))
                return slot.Callsign.Substring(0, 1);

            return ((char)('A' + slotIndex)).ToString();
        }

        private static string BuildSlotName(GarageSlotViewModel slot)
        {
            if (slot == null || slot.IsEmpty)
                return "EMPTY";

            if (!string.IsNullOrWhiteSpace(slot.RoleLabel))
                return slot.RoleLabel;

            return !string.IsNullOrWhiteSpace(slot.StatusBadgeText)
                ? slot.StatusBadgeText
                : "UNIT";
        }

        private readonly struct SlotBinding
        {
            public SlotBinding(
                Button card,
                Label codeLabel,
                VisualElement iconHost,
                Label iconLabel,
                Label nameLabel)
            {
                Card = card;
                CodeLabel = codeLabel;
                IconHost = iconHost;
                IconLabel = iconLabel;
                NameLabel = nameLabel;
            }

            public Button Card { get; }
            public Label CodeLabel { get; }
            public VisualElement IconHost { get; }
            public Label IconLabel { get; }
            public Label NameLabel { get; }
        }

        private readonly struct FocusedPartText
        {
            public FocusedPartText(string badge, string title, string description, string icon)
            {
                Badge = badge;
                Title = title;
                Description = description;
                Icon = icon;
            }

            public string Badge { get; }
            public string Title { get; }
            public string Description { get; }
            public string Icon { get; }

            public static FocusedPartText From(GarageEditorViewModel editor, GarageEditorFocus focusedPart)
            {
                if (editor == null)
                    return new FocusedPartText("편성", "Garage", "런타임 데이터 대기", "G");

                return focusedPart switch
                {
                    GarageEditorFocus.Firepower => new FocusedPartText(
                        "주무장",
                        editor.FirepowerValueText,
                        editor.FirepowerHintText,
                        "W"),
                    GarageEditorFocus.Mobility => new FocusedPartText(
                        "기동",
                        editor.MobilityValueText,
                        editor.MobilityHintText,
                        "M"),
                    _ => new FocusedPartText(
                        "프레임",
                        editor.FrameValueText,
                        editor.FrameHintText,
                        "F"),
                };
            }
        }
    }
}
