using System;
using System.Collections.Generic;
using Shared.Ui;
using UnityEngine;
using UnityEngine.UIElements;

namespace Features.Garage.Presentation
{
    internal sealed class GarageSetBPartListSurface
    {
        private const int InitialPartRowCount = 8;

        private readonly Button _frameTabButton;
        private readonly Button _firepowerTabButton;
        private readonly Button _mobilityTabButton;
        private readonly Label _partListTitleLabel;
        private readonly Label _partListCountLabel;
        private readonly TextField _partSearchField;
        private readonly Label _partPreviewTitleLabel;
        private readonly Label _partPreviewMetaLabel;
        private readonly Label _partPreviewLabel;
        private readonly Image _partPreviewImage;
        private readonly VisualElement _partListRows;
        private readonly List<PartRowBinding> _partRows = new();

        public GarageSetBPartListSurface(VisualElement root)
        {
            _frameTabButton = UitkElementUtility.Required<Button>(root, "FrameTabButton");
            _firepowerTabButton = UitkElementUtility.Required<Button>(root, "FirepowerTabButton");
            _mobilityTabButton = UitkElementUtility.Required<Button>(root, "MobilityTabButton");
            _partListTitleLabel = UitkElementUtility.Required<Label>(root, "PartListTitleLabel");
            _partListCountLabel = UitkElementUtility.Required<Label>(root, "PartListCountLabel");
            _partSearchField = UitkElementUtility.Required<TextField>(root, "PartSearchField");
            _partPreviewTitleLabel = UitkElementUtility.Required<Label>(root, "SelectedPartPreviewTitleLabel");
            _partPreviewMetaLabel = UitkElementUtility.Required<Label>(root, "SelectedPartPreviewMetaLabel");
            var partPreviewHost = UitkElementUtility.Required<VisualElement>(root, "SelectedPartPreviewHost");
            _partPreviewLabel = UitkElementUtility.Required<Label>(root, "SelectedPartPreviewLabel");
            _partPreviewImage = UitkElementUtility.CreateAbsoluteImage("SelectedPartPreviewImage");
            partPreviewHost.Insert(0, _partPreviewImage);
            _partListRows = UitkElementUtility.Required<ScrollView>(root, "PartListRows").contentContainer;
            for (int i = 0; i < InitialPartRowCount; i++)
            {
                int rowNumber = i + 1;
                _partRows.Add(new PartRowBinding(
                    UitkElementUtility.Required<Button>(root, $"PartRow{rowNumber:00}"),
                    UitkElementUtility.Required<Label>(root, $"PartRow{rowNumber:00}NameLabel"),
                    UitkElementUtility.Required<Label>(root, $"PartRow{rowNumber:00}MetaLabel"),
                    UitkElementUtility.Required<Label>(root, $"PartRow{rowNumber:00}BadgeLabel")));
            }

            BindCallbacks();
            SetPreviewTexture(null, false);
        }

        public event Action<GarageEditorFocus> FocusSelected;
        public event Action<string> SearchChanged;
        public event Action<GarageNovaPartSelection> OptionSelected;

        public void Render(GarageNovaPartsPanelViewModel partList, GarageEditorFocus focusedPart)
        {
            RenderPartList(partList);
            RenderPartPreviewInfo(partList);
            RenderFocusTabs(focusedPart);
        }

        public void SetPreviewTexture(Texture texture, bool isVisible)
        {
            if (_partPreviewImage == null || _partPreviewLabel == null)
                return;

            _partPreviewImage.image = isVisible ? texture : null;
            _partPreviewImage.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
            _partPreviewLabel.style.display = isVisible ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private void BindCallbacks()
        {
            _frameTabButton.clicked += () => FocusSelected?.Invoke(GarageEditorFocus.Frame);
            _firepowerTabButton.clicked += () => FocusSelected?.Invoke(GarageEditorFocus.Firepower);
            _mobilityTabButton.clicked += () => FocusSelected?.Invoke(GarageEditorFocus.Mobility);
            _partSearchField.RegisterValueChangedCallback(evt => SearchChanged?.Invoke(evt.newValue ?? string.Empty));
            for (int i = 0; i < _partRows.Count; i++)
                BindPartRow(_partRows[i]);
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
            _partPreviewLabel.text = BuildPartPreviewPlaceholderText(slot);
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
                    OptionSelected?.Invoke(new GarageNovaPartSelection(option.Slot, option.Id));
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
            UitkElementUtility.SetClass(binding.Row, "part-row--selected", option.IsSelected);
            UitkElementUtility.SetClass(binding.BadgeLabel, "part-row-badge--selected", option.IsSelected);
            UitkElementUtility.SetClass(binding.BadgeLabel, "part-row-badge--review", option.NeedsNameReview && !option.IsSelected);
        }

        private void RenderFocusTabs(GarageEditorFocus focusedPart)
        {
            UitkElementUtility.SetClass(_frameTabButton, "focus-tab--active", focusedPart == GarageEditorFocus.Frame);
            UitkElementUtility.SetClass(_firepowerTabButton, "focus-tab--active", focusedPart == GarageEditorFocus.Firepower);
            UitkElementUtility.SetClass(_mobilityTabButton, "focus-tab--active", focusedPart == GarageEditorFocus.Mobility);
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

        private static string BuildPartPreviewMeta(GarageNovaPartPanelSlot slot, string selectedDetailText)
        {
            if (string.IsNullOrWhiteSpace(selectedDetailText))
                return BuildPartListTitle(slot);

            int lineBreak = selectedDetailText.IndexOf('\n');
            return lineBreak >= 0 ? selectedDetailText.Substring(0, lineBreak) : selectedDetailText;
        }

        private static string BuildPartPreviewPlaceholderText(GarageNovaPartPanelSlot slot)
        {
            return slot switch
            {
                GarageNovaPartPanelSlot.Firepower => "무장",
                GarageNovaPartPanelSlot.Frame => "프레임",
                _ => "기동",
            };
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
    }
}
