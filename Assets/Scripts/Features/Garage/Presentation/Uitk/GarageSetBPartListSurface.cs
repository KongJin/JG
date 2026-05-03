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
        private const float PartRowHeight = 52f;
        private const float PartStatFillWidth = 12f;

        private readonly Button _frameTabButton;
        private readonly Button _firepowerTabButton;
        private readonly Button _mobilityTabButton;
        private readonly Label _partListTitleLabel;
        private readonly Label _partListCountLabel;
        private readonly TextField _partSearchField;
        private readonly Label _partPreviewKickerLabel;
        private readonly Label _partPreviewTitleLabel;
        private readonly Label _partEnergyLabel;
        private readonly Label _partPreviewMetaLabel;
        private readonly Label _partPreviewLabel;
        private readonly Image _partPreviewImage;
        private readonly PartStatBarBinding[] _partStatBars;
        private readonly VisualElement _partListRows;
        private readonly List<PartRowBinding> _partRows = new();
        private GarageNovaPartsPanelViewModel _lastPartList;
        private GarageNovaPartPanelSlot _lastSlot;
        private string _lastSearchText = string.Empty;
        private int _visibleStartIndex;

        public GarageSetBPartListSurface(VisualElement root)
        {
            _frameTabButton = UitkElementUtility.Required<Button>(root, "FrameTabButton");
            _firepowerTabButton = UitkElementUtility.Required<Button>(root, "FirepowerTabButton");
            _mobilityTabButton = UitkElementUtility.Required<Button>(root, "MobilityTabButton");
            _partListTitleLabel = UitkElementUtility.Required<Label>(root, "PartListTitleLabel");
            _partListCountLabel = UitkElementUtility.Required<Label>(root, "PartListCountLabel");
            _partSearchField = UitkElementUtility.Required<TextField>(root, "PartSearchField");
            _partPreviewKickerLabel = UitkElementUtility.Required<Label>(root, "SelectedPartPreviewKickerLabel");
            _partPreviewTitleLabel = UitkElementUtility.Required<Label>(root, "SelectedPartPreviewTitleLabel");
            _partEnergyLabel = UitkElementUtility.Required<Label>(root, "SelectedPartEnergyLabel");
            _partPreviewMetaLabel = UitkElementUtility.Required<Label>(root, "SelectedPartPreviewMetaLabel");
            var partPreviewHost = UitkElementUtility.Required<VisualElement>(root, "SelectedPartPreviewHost");
            _partPreviewLabel = UitkElementUtility.Required<Label>(root, "SelectedPartPreviewLabel");
            _partPreviewImage = UitkElementUtility.CreateAbsoluteImage("SelectedPartPreviewImage");
            partPreviewHost.Insert(0, _partPreviewImage);
            _partStatBars = new[]
            {
                new PartStatBarBinding(
                    UitkElementUtility.Required<VisualElement>(root, "SelectedPartStatRow01"),
                    UitkElementUtility.Required<Label>(root, "SelectedPartStat01Label"),
                    UitkElementUtility.Required<VisualElement>(root, "SelectedPartStat01Fill"),
                    UitkElementUtility.Required<Label>(root, "SelectedPartStat01Value")),
                new PartStatBarBinding(
                    UitkElementUtility.Required<VisualElement>(root, "SelectedPartStatRow02"),
                    UitkElementUtility.Required<Label>(root, "SelectedPartStat02Label"),
                    UitkElementUtility.Required<VisualElement>(root, "SelectedPartStat02Fill"),
                    UitkElementUtility.Required<Label>(root, "SelectedPartStat02Value")),
                new PartStatBarBinding(
                    UitkElementUtility.Required<VisualElement>(root, "SelectedPartStatRow03"),
                    UitkElementUtility.Required<Label>(root, "SelectedPartStat03Label"),
                    UitkElementUtility.Required<VisualElement>(root, "SelectedPartStat03Fill"),
                    UitkElementUtility.Required<Label>(root, "SelectedPartStat03Value")),
            };
            _partListRows = UitkElementUtility.Required<ScrollView>(root, "PartListRows").contentContainer;
            for (int i = 0; i < InitialPartRowCount; i++)
            {
                int rowNumber = i + 1;
                _partRows.Add(new PartRowBinding(
                    UitkElementUtility.Required<VisualElement>(root, $"PartRow{rowNumber:00}"),
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

        public bool ScrollVisibleOptions(int deltaRows)
        {
            if (_lastPartList?.Options == null || _lastPartList.Options.Count <= _partRows.Count)
                return false;

            int maxStart = Math.Max(0, _lastPartList.Options.Count - _partRows.Count);
            int nextStart = Mathf.Clamp(_visibleStartIndex + deltaRows, 0, maxStart);
            if (nextStart == _visibleStartIndex)
                return false;

            _visibleStartIndex = nextStart;
            RenderVisiblePartRows(_lastPartList);
            return true;
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
            ResetScrollIfListChanged(partList);
            _lastPartList = partList;
            _partListTitleLabel.text = BuildPartListTitle(partList?.ActiveSlot ?? GarageNovaPartPanelSlot.Frame);
            _partListCountLabel.text = partList?.CountText ?? "부품 0개";
            _partSearchField.SetValueWithoutNotify(partList?.SearchText ?? string.Empty);
            EnsurePartRowCapacity(Math.Min(InitialPartRowCount, partList?.Options?.Count ?? 0));
            RenderVisiblePartRows(partList);
        }

        private void RenderVisiblePartRows(GarageNovaPartsPanelViewModel partList)
        {
            for (int i = 0; i < _partRows.Count; i++)
            {
                var optionIndex = _visibleStartIndex + i;
                var option = partList != null && partList.Options != null && optionIndex < partList.Options.Count
                    ? partList.Options[optionIndex]
                    : null;
                RenderPartRow(_partRows[i], option);
            }
        }

        private void ResetScrollIfListChanged(GarageNovaPartsPanelViewModel partList)
        {
            var nextSlot = partList?.ActiveSlot ?? GarageNovaPartPanelSlot.Frame;
            var nextSearch = partList?.SearchText ?? string.Empty;
            if (nextSlot == _lastSlot && string.Equals(nextSearch, _lastSearchText, StringComparison.Ordinal))
                return;

            _lastSlot = nextSlot;
            _lastSearchText = nextSearch;
            _visibleStartIndex = 0;
        }

        private void RenderPartPreviewInfo(GarageNovaPartsPanelViewModel partList)
        {
            var slot = partList?.ActiveSlot ?? GarageNovaPartPanelSlot.Mobility;
            _partPreviewKickerLabel.text = BuildSelectedPartKicker(partList);
            _partPreviewTitleLabel.text = string.IsNullOrWhiteSpace(partList?.SelectedNameText)
                ? BuildPartListTitle(slot)
                : partList.SelectedNameText;
            _partPreviewMetaLabel.text = BuildPartPreviewMeta(slot, partList?.SelectedDetailText);
            _partPreviewLabel.text = BuildPartPreviewPlaceholderText(slot);
            RenderPartEnergy(partList?.SelectedDetailText);
            RenderPartPreviewStats(partList?.SelectedDetailText);
        }

        private void EnsurePartRowCapacity(int requiredCount)
        {
            while (_partRows.Count < requiredCount)
            {
                int rowNumber = _partRows.Count + 1;
                var row = new VisualElement { name = $"PartRow{rowNumber:00}" };
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
            ConfigurePartRowLayout(binding.Row);
            binding.Row.AddManipulator(new Clickable(() => SelectPartRow(binding)));
            binding.Row.RegisterCallback<ClickEvent>(_ => SelectPartRow(binding));
        }

        private void SelectPartRow(PartRowBinding binding)
        {
            var option = binding.Option;
            if (option != null)
                OptionSelected?.Invoke(new GarageNovaPartSelection(option.Slot, option.Id));
        }

        private static void ConfigurePartRowLayout(VisualElement row)
        {
            row.style.height = PartRowHeight;
            row.style.minHeight = PartRowHeight;
            row.style.maxHeight = PartRowHeight;
            row.style.flexShrink = 0f;
            row.style.overflow = Overflow.Hidden;
            row.style.marginBottom = 6f;
            row.style.borderTopWidth = 1f;
            row.style.borderRightWidth = 1f;
            row.style.borderBottomWidth = 1f;
            row.style.borderLeftWidth = 1f;
            row.style.borderTopLeftRadius = 4f;
            row.style.borderTopRightRadius = 4f;
            row.style.borderBottomLeftRadius = 4f;
            row.style.borderBottomRightRadius = 4f;
            row.focusable = false;
        }

        private static void RenderPartRow(PartRowBinding binding, GarageNovaPartOptionViewModel option)
        {
            binding.Option = option;
            binding.Row.style.display = option != null ? DisplayStyle.Flex : DisplayStyle.None;
            if (option == null)
                return;

            binding.NameLabel.text = string.IsNullOrWhiteSpace(option.DisplayName) ? option.Id : option.DisplayName;
            binding.MetaLabel.text = StripTierText(option.DetailText);
            binding.BadgeLabel.text = BuildPartBadgeText(option);
            binding.BadgeLabel.style.display = string.IsNullOrWhiteSpace(binding.BadgeLabel.text)
                ? DisplayStyle.None
                : DisplayStyle.Flex;
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

            return string.Empty;
        }

        private static string BuildSelectedPartKicker(GarageNovaPartsPanelViewModel partList)
        {
            if (partList?.Options == null)
                return "현재 선택";

            for (int i = 0; i < partList.Options.Count; i++)
            {
                if (partList.Options[i].IsSelected)
                    return "현재 장착";
            }

            return "선택 후보";
        }

        private static string BuildPartPreviewMeta(GarageNovaPartPanelSlot slot, string selectedDetailText)
        {
            if (string.IsNullOrWhiteSpace(selectedDetailText))
                return BuildPartListTitle(slot);

            int lineBreak = selectedDetailText.IndexOf('\n');
            var firstLine = lineBreak >= 0 ? selectedDetailText.Substring(0, lineBreak) : selectedDetailText;
            return StripTierText(firstLine);
        }

        private void RenderPartPreviewStats(string selectedDetailText)
        {
            var stats = BuildStats(selectedDetailText);
            for (int i = 0; i < _partStatBars.Length; i++)
            {
                bool hasStat = i < stats.Count;
                var bar = _partStatBars[i];
                bar.Row.style.display = hasStat ? DisplayStyle.Flex : DisplayStyle.None;
                if (!hasStat)
                    continue;

                var stat = stats[i];
                bar.Label.text = stat.Label;
                bar.Value.text = stat.ValueText;
                bar.Fill.style.width = PartStatFillWidth;
                bar.Fill.style.height = Length.Percent(stat.Percent);
            }
        }

        private void RenderPartEnergy(string selectedDetailText)
        {
            if (_partEnergyLabel == null)
                return;

            bool hasEnergy = TryFindEnergy(selectedDetailText, out var energyText);
            _partEnergyLabel.text = hasEnergy ? energyText : string.Empty;
            _partEnergyLabel.style.display = hasEnergy ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private static List<PartStatViewModel> BuildStats(string detailText)
        {
            var stats = new List<PartStatViewModel>(3);
            var firstLine = StripSourceLine(detailText);
            if (string.IsNullOrWhiteSpace(firstLine))
                return stats;

            var tokens = firstLine.Split('|');
            for (int i = 0; i < tokens.Length && stats.Count < 3; i++)
            {
                if (TryBuildStat(tokens[i], out var stat))
                    stats.Add(stat);
            }

            return stats;
        }

        private static bool TryBuildStat(string token, out PartStatViewModel stat)
        {
            stat = default;
            token = StripTierText(token);
            if (string.IsNullOrWhiteSpace(token))
                return false;

            token = token.Trim();
            int valueStart = IndexOfNumber(token);
            if (valueStart < 0)
                return false;

            string label = NormalizeStatLabel(token.Substring(0, valueStart));
            string valueText = token.Substring(valueStart).Trim();
            if (string.Equals(label, "EN", StringComparison.Ordinal))
                return false;

            if (string.IsNullOrWhiteSpace(label) || !float.TryParse(valueText, out var value))
                return false;

            if (Mathf.Approximately(value, 0f))
                return false;

            stat = new PartStatViewModel(label, valueText, Mathf.Clamp(value / GetStatMax(label) * 100f, 4f, 100f));
            return true;
        }

        private static int IndexOfNumber(string text)
        {
            for (int i = 0; i < text.Length; i++)
            {
                if (char.IsDigit(text[i]) || text[i] == '-' || text[i] == '.')
                    return i;
            }

            return -1;
        }

        private static string NormalizeStatLabel(string label)
        {
            label = (label ?? string.Empty).Trim().TrimEnd('+').Trim();
            return label.Length == 0 ? string.Empty : label.ToUpperInvariant();
        }

        private static float GetStatMax(string label)
        {
            return label switch
            {
                "ATK" => 1000f,
                "RNG" => 16f,
                "MOV" => 6f,
                "HP" => 900f,
                "DEF" => 10f,
                "ASPD" => 2f,
                _ => 100f,
            };
        }

        private static bool TryFindEnergy(string detailText, out string energyText)
        {
            energyText = string.Empty;
            var firstLine = StripSourceLine(detailText);
            if (string.IsNullOrWhiteSpace(firstLine))
                return false;

            var tokens = firstLine.Split('|');
            for (int i = 0; i < tokens.Length; i++)
            {
                var token = StripTierText(tokens[i]).Trim();
                if (string.IsNullOrWhiteSpace(token))
                    continue;

                int valueStart = IndexOfNumber(token);
                if (valueStart < 0)
                    continue;

                string label = NormalizeStatLabel(token.Substring(0, valueStart));
                string valueText = token.Substring(valueStart).Trim();
                if (!string.Equals(label, "EN", StringComparison.Ordinal) ||
                    !float.TryParse(valueText, out var value) ||
                    Mathf.Approximately(value, 0f))
                    continue;

                energyText = $"EN {valueText}";
                return true;
            }

            return false;
        }

        private static string StripSourceLine(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            int lineBreak = text.IndexOf('\n');
            return lineBreak >= 0 ? text.Substring(0, lineBreak) : text;
        }

        private static string StripTierText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var tokens = text.Split('|');
            var visible = new List<string>(tokens.Length);
            for (int i = 0; i < tokens.Length; i++)
            {
                var token = tokens[i].Trim();
                if (IsTierToken(token) || IsZeroNonEnergyStatToken(token))
                    continue;

                visible.Add(token);
            }

            return string.Join(" | ", visible);
        }

        private static bool IsTierToken(string token)
        {
            return string.Equals(token, "T1", StringComparison.Ordinal)
                || string.Equals(token, "T2", StringComparison.Ordinal)
                || string.Equals(token, "T3", StringComparison.Ordinal)
                || string.Equals(token, "T4", StringComparison.Ordinal);
        }

        private static bool IsZeroNonEnergyStatToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return false;

            int valueStart = IndexOfNumber(token);
            if (valueStart < 0)
                return false;

            string label = NormalizeStatLabel(token.Substring(0, valueStart));
            string valueText = token.Substring(valueStart).Trim();
            return !string.Equals(label, "EN", StringComparison.Ordinal)
                && float.TryParse(valueText, out var value)
                && Mathf.Approximately(value, 0f);
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
                VisualElement row,
                Label nameLabel,
                Label metaLabel,
                Label badgeLabel)
            {
                Row = row;
                NameLabel = nameLabel;
                MetaLabel = metaLabel;
                BadgeLabel = badgeLabel;
            }

            public VisualElement Row { get; }
            public Label NameLabel { get; }
            public Label MetaLabel { get; }
            public Label BadgeLabel { get; }
            public GarageNovaPartOptionViewModel Option { get; set; }
        }

        private readonly struct PartStatViewModel
        {
            public PartStatViewModel(string label, string valueText, float percent)
            {
                Label = label;
                ValueText = valueText;
                Percent = percent;
            }

            public string Label { get; }
            public string ValueText { get; }
            public float Percent { get; }
        }

        private readonly struct PartStatBarBinding
        {
            public PartStatBarBinding(
                VisualElement row,
                Label label,
                VisualElement fill,
                Label value)
            {
                Row = row;
                Label = label;
                Fill = fill;
                Value = value;
            }

            public VisualElement Row { get; }
            public Label Label { get; }
            public VisualElement Fill { get; }
            public Label Value { get; }
        }
    }
}
