using System;
using System.Collections.Generic;
using Shared.Ui;
using UnityEngine;
using UnityEngine.UIElements;

namespace Features.Garage.Presentation
{
    public sealed class GarageSetBUitkSurface
    {
        private const int SlotCount = 4;
        private const int InitialPartRowCount = 8;

        private readonly Label _commandStatusLabel;
        private readonly Button _settingsButton;
        private readonly SlotBinding[] _slots = new SlotBinding[SlotCount];
        private readonly Button _frameTabButton;
        private readonly Button _firepowerTabButton;
        private readonly Button _mobilityTabButton;
        private readonly Label _partListTitleLabel;
        private readonly Label _partListCountLabel;
        private readonly TextField _partSearchField;
        private readonly Label _partPreviewTitleLabel;
        private readonly Label _partPreviewMetaLabel;
        private readonly VisualElement _partPreviewHost;
        private readonly Label _partPreviewLabel;
        private readonly Image _partPreviewImage;
        private readonly VisualElement _partListRows;
        private readonly List<PartRowBinding> _partRows = new();
        private readonly Label _focusedPartBadgeLabel;
        private readonly Label _focusedPartTitleLabel;
        private readonly Label _focusedPartDescriptionLabel;
        private readonly VisualElement _focusedPartIconGlyph;
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
                    Required<VisualElement>(root, $"SlotIcon{slotNumber:00}Glyph"),
                    Required<Label>(root, $"SlotName{slotNumber:00}Label"));
            }

            _frameTabButton = Required<Button>(root, "FrameTabButton");
            _firepowerTabButton = Required<Button>(root, "FirepowerTabButton");
            _mobilityTabButton = Required<Button>(root, "MobilityTabButton");
            _partListTitleLabel = Required<Label>(root, "PartListTitleLabel");
            _partListCountLabel = Required<Label>(root, "PartListCountLabel");
            _partSearchField = Required<TextField>(root, "PartSearchField");
            _partPreviewTitleLabel = Required<Label>(root, "SelectedPartPreviewTitleLabel");
            _partPreviewMetaLabel = Required<Label>(root, "SelectedPartPreviewMetaLabel");
            _partPreviewHost = Required<VisualElement>(root, "SelectedPartPreviewHost");
            _partPreviewLabel = Required<Label>(root, "SelectedPartPreviewLabel");
            _partPreviewImage = CreatePreviewImage("SelectedPartPreviewImage");
            _partPreviewHost.Insert(0, _partPreviewImage);
            _partListRows = Required<ScrollView>(root, "PartListRows").contentContainer;
            for (int i = 0; i < InitialPartRowCount; i++)
            {
                int rowNumber = i + 1;
                _partRows.Add(new PartRowBinding(
                    Required<Button>(root, $"PartRow{rowNumber:00}"),
                    Required<Label>(root, $"PartRow{rowNumber:00}NameLabel"),
                    Required<Label>(root, $"PartRow{rowNumber:00}MetaLabel"),
                    Required<Label>(root, $"PartRow{rowNumber:00}BadgeLabel")));
            }

            _focusedPartBadgeLabel = Required<Label>(root, "FocusedPartBadgeLabel");
            _focusedPartTitleLabel = Required<Label>(root, "FocusedPartTitleLabel");
            _focusedPartDescriptionLabel = Required<Label>(root, "FocusedPartDescriptionLabel");
            _focusedPartIconGlyph = Required<VisualElement>(root, "FocusedPartIconGlyph");
            _previewTitleLabel = Required<Label>(root, "PreviewTitleLabel");
            _unitPreviewHost = Required<VisualElement>(root, "UnitPreviewHost");
            _unitPreviewLabel = Required<Label>(root, "UnitPreviewLabel");
            _unitPreviewImage = CreatePreviewImage();
            _unitPreviewHost.Insert(0, _unitPreviewImage);
            _saveButton = Required<Button>(root, "SaveButton");

            BindCallbacks();
            SetPreviewTexture(null, false);
            SetPartPreviewTexture(null, false);
        }

        public event Action<int> SlotSelected;
        public event Action<GarageEditorFocus> PartFocusSelected;
        public event Action<string> PartSearchChanged;
        public event Action<GarageNovaPartSelection> PartOptionSelected;
        public event Action SaveRequested;
        public event Action SettingsRequested;

        public void Render(
            IReadOnlyList<GarageSlotViewModel> slots,
            GarageNovaPartsPanelViewModel partList,
            GarageEditorViewModel editor,
            GarageResultViewModel result,
            GarageEditorFocus focusedPart,
            bool isSaving)
        {
            RenderSlots(slots);
            RenderPartList(partList);
            RenderPartPreviewInfo(partList);
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

        public void SetPartPreviewTexture(Texture texture, bool isVisible)
        {
            if (_partPreviewImage == null || _partPreviewLabel == null)
                return;

            _partPreviewImage.image = isVisible ? texture : null;
            _partPreviewImage.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
            _partPreviewLabel.style.display = isVisible ? DisplayStyle.None : DisplayStyle.Flex;
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
            _partSearchField.RegisterValueChangedCallback(evt => PartSearchChanged?.Invoke(evt.newValue ?? string.Empty));
            for (int i = 0; i < _partRows.Count; i++)
            {
                BindPartRow(_partRows[i]);
            }

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
            UitkIconRegistry.Apply(binding.IconGlyph, BuildSlotIconId(slot));
            binding.NameLabel.text = BuildSlotName(slot);

            SetClass(binding.Card, "slot-card--active", isSelected);
            SetClass(binding.Card, "slot-card--empty", isEmpty);
            SetClass(binding.CodeLabel, "slot-code--active", isSelected);
            SetClass(binding.CodeLabel, "slot-code--empty", isEmpty);
            SetClass(binding.IconHost, "slot-icon--active", isSelected);
            SetClass(binding.IconHost, "slot-icon--empty", isEmpty);
            SetClass(binding.IconGlyph, "slot-icon-glyph--active", isSelected);
            SetClass(binding.IconGlyph, "slot-icon-glyph--empty", isEmpty);
            SetClass(binding.NameLabel, "slot-name--active", isSelected);
            SetClass(binding.NameLabel, "slot-name--empty", isEmpty);
        }

        private void RenderPartList(GarageNovaPartsPanelViewModel partList)
        {
            _partListTitleLabel.text = BuildPartListTitle(partList?.ActiveSlot ?? GarageNovaPartPanelSlot.Frame);
            _partListCountLabel.text = partList?.CountText ?? "0 PARTS";
            _partSearchField.SetValueWithoutNotify(partList?.SearchText ?? string.Empty);
            EnsurePartRowCapacity(partList?.Options?.Count ?? 0);

            for (int i = 0; i < _partRows.Count; i++)
            {
                var option = partList != null && partList.Options != null && i < partList.Options.Count
                    ? partList.Options[i]
                    : null;
                RenderPartRow(_partRows[i], option);
            }
        }

        private void RenderPartPreviewInfo(GarageNovaPartsPanelViewModel partList)
        {
            var slot = partList?.ActiveSlot ?? GarageNovaPartPanelSlot.Mobility;
            _partPreviewTitleLabel.text = string.IsNullOrWhiteSpace(partList?.SelectedNameText)
                ? BuildPartListTitle(slot)
                : partList.SelectedNameText;
            _partPreviewMetaLabel.text = BuildPartPreviewMeta(slot, partList?.SelectedDetailText);
            _partPreviewLabel.text = BuildPartPreviewFallbackText(slot);
        }

        private void EnsurePartRowCapacity(int requiredCount)
        {
            while (_partRows.Count < requiredCount)
            {
                int rowNumber = _partRows.Count + 1;
                var row = new Button { name = $"PartRow{rowNumber:00}" };
                row.AddToClassList("part-row");

                var main = new VisualElement();
                main.AddToClassList("part-row-main");

                var nameLabel = new Label { name = $"PartRow{rowNumber:00}NameLabel" };
                nameLabel.AddToClassList("part-row-name");

                var metaLabel = new Label { name = $"PartRow{rowNumber:00}MetaLabel" };
                metaLabel.AddToClassList("part-row-meta");

                var badgeLabel = new Label { name = $"PartRow{rowNumber:00}BadgeLabel" };
                badgeLabel.AddToClassList("part-row-badge");

                main.Add(nameLabel);
                main.Add(metaLabel);
                row.Add(main);
                row.Add(badgeLabel);

                var binding = new PartRowBinding(row, nameLabel, metaLabel, badgeLabel);
                BindPartRow(binding);
                _partRows.Add(binding);
                _partListRows.Add(row);
            }
        }

        private void BindPartRow(PartRowBinding binding)
        {
            binding.Row.clicked += () =>
            {
                var option = binding.Option;
                if (option != null)
                    PartOptionSelected?.Invoke(new GarageNovaPartSelection(option.Slot, option.Id));
            };
        }

        private static void RenderPartRow(PartRowBinding binding, GarageNovaPartOptionViewModel option)
        {
            binding.Option = option;
            binding.Row.style.display = option != null ? DisplayStyle.Flex : DisplayStyle.None;
            if (option == null)
                return;

            binding.NameLabel.text = string.IsNullOrWhiteSpace(option.DisplayName) ? option.Id : option.DisplayName;
            binding.MetaLabel.text = option.DetailText ?? string.Empty;
            binding.BadgeLabel.text = BuildPartBadgeText(option);
            SetClass(binding.Row, "part-row--selected", option.IsSelected);
            SetClass(binding.BadgeLabel, "part-row-badge--selected", option.IsSelected);
            SetClass(binding.BadgeLabel, "part-row-badge--review", option.NeedsNameReview && !option.IsSelected);
        }

        private void RenderFocusedPart(GarageEditorViewModel editor, GarageEditorFocus focusedPart)
        {
            var part = FocusedPartText.From(editor, focusedPart);
            _focusedPartBadgeLabel.text = part.Badge;
            _focusedPartTitleLabel.text = part.Title;
            _focusedPartDescriptionLabel.text = part.Description;
            UitkIconRegistry.Apply(_focusedPartIconGlyph, part.IconId);
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

        private static string BuildPartListTitle(GarageNovaPartPanelSlot slot)
        {
            return slot switch
            {
                GarageNovaPartPanelSlot.Firepower => "무장 선택",
                GarageNovaPartPanelSlot.Mobility => "기동 선택",
                _ => "프레임 선택",
            };
        }

        private static string BuildPartBadgeText(GarageNovaPartOptionViewModel option)
        {
            if (option.IsSelected)
                return "장착중";

            if (option.NeedsNameReview)
                return "검토";

            var detailText = option.DetailText ?? string.Empty;
            int tierIndex = detailText.LastIndexOf("| T", StringComparison.Ordinal);
            return tierIndex >= 0
                ? detailText.Substring(tierIndex + 2).Trim()
                : "부품";
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

        private static Image CreatePreviewImage(string name = "RuntimeUnitPreviewImage")
        {
            var image = new Image
            {
                name = name,
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

        private static string BuildPartPreviewMeta(GarageNovaPartPanelSlot slot, string selectedDetailText)
        {
            if (string.IsNullOrWhiteSpace(selectedDetailText))
                return BuildPartListTitle(slot);

            int lineBreak = selectedDetailText.IndexOf('\n');
            return lineBreak >= 0 ? selectedDetailText.Substring(0, lineBreak) : selectedDetailText;
        }

        private static string BuildPartPreviewFallbackText(GarageNovaPartPanelSlot slot)
        {
            return slot switch
            {
                GarageNovaPartPanelSlot.Firepower => "무장",
                GarageNovaPartPanelSlot.Frame => "프레임",
                _ => "기동",
            };
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
                VisualElement iconGlyph,
                Label nameLabel)
            {
                Card = card;
                CodeLabel = codeLabel;
                IconHost = iconHost;
                IconGlyph = iconGlyph;
                NameLabel = nameLabel;
            }

            public Button Card { get; }
            public Label CodeLabel { get; }
            public VisualElement IconHost { get; }
            public VisualElement IconGlyph { get; }
            public Label NameLabel { get; }
        }

        private sealed class PartRowBinding
        {
            public PartRowBinding(
                Button row,
                Label nameLabel,
                Label metaLabel,
                Label badgeLabel)
            {
                Row = row;
                NameLabel = nameLabel;
                MetaLabel = metaLabel;
                BadgeLabel = badgeLabel;
            }

            public Button Row { get; }
            public Label NameLabel { get; }
            public Label MetaLabel { get; }
            public Label BadgeLabel { get; }
            public GarageNovaPartOptionViewModel Option { get; set; }
        }

        private readonly struct FocusedPartText
        {
            public FocusedPartText(string badge, string title, string description, string iconId)
            {
                Badge = badge;
                Title = title;
                Description = description;
                IconId = iconId;
            }

            public string Badge { get; }
            public string Title { get; }
            public string Description { get; }
            public string IconId { get; }

            public static FocusedPartText From(GarageEditorViewModel editor, GarageEditorFocus focusedPart)
            {
                if (editor == null)
                    return new FocusedPartText("편성", "Garage", "런타임 데이터 대기", "garage");

                return focusedPart switch
                {
                    GarageEditorFocus.Firepower => new FocusedPartText(
                        "주무장",
                        editor.FirepowerValueText,
                        editor.FirepowerHintText,
                        "swords"),
                    GarageEditorFocus.Mobility => new FocusedPartText(
                        "기동",
                        editor.MobilityValueText,
                        editor.MobilityHintText,
                        "speed"),
                    _ => new FocusedPartText(
                        "프레임",
                        editor.FrameValueText,
                        editor.FrameHintText,
                        "security"),
                };
            }
        }
    }
}
