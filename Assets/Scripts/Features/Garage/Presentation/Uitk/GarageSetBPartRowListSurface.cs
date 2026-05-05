using System;
using System.Collections.Generic;
using Shared.Ui;
using UnityEngine;
using UnityEngine.UIElements;

namespace Features.Garage.Presentation
{
    internal sealed class GarageSetBPartRowListSurface : BaseSurface<VisualElement>
    {
        private readonly Label _partListTitleLabel;
        private readonly Label _partListCountLabel;
        private readonly TextField _partSearchField;
        private readonly VisualElement _partListRows;
        private readonly List<PartRowBinding> _partRows = new();
        private GarageNovaPartsPanelViewModel _lastPartList;
        private GarageNovaPartPanelSlot _lastSlot;
        private string _lastSearchText = string.Empty;
        private int _visibleStartIndex;
        private bool _suppressNextPartRowClick;
        private EventCallback<ChangeEvent<string>> _searchCallback;

        public GarageSetBPartRowListSurface(VisualElement root)
            : base(root)
        {
            _partListTitleLabel = UitkElementUtility.Required<Label>(root, "PartListTitleLabel");
            _partListCountLabel = UitkElementUtility.Required<Label>(root, "PartListCountLabel");
            _partSearchField = UitkElementUtility.Required<TextField>(root, "PartSearchField");
            _partListRows = UitkElementUtility.Required<ScrollView>(root, "PartListRows").contentContainer;

            for (int i = 0; i < GarageUitkConstants.Parts.InitialRowCount; i++)
            {
                int rowNumber = i + 1;
                _partRows.Add(new PartRowBinding(
                    UitkElementUtility.Required<Button>(root, $"PartRow{rowNumber:00}"),
                    UitkElementUtility.Required<Label>(root, $"PartRow{rowNumber:00}NameLabel"),
                    UitkElementUtility.Required<Label>(root, $"PartRow{rowNumber:00}MetaLabel"),
                    UitkElementUtility.Required<Label>(root, $"PartRow{rowNumber:00}BadgeLabel")));
            }

            BindCallbacks();
        }

        public event Action<string> SearchChanged;
        public event Action<GarageNovaPartSelection> OptionSelected;

        public TextField SearchField => _partSearchField;

        public void Render(GarageNovaPartsPanelViewModel partList)
        {
            ResetScrollIfListChanged(partList);
            _lastPartList = partList;
// csharp-guardrails: allow-null-defense
            _partListTitleLabel.text = BuildPartListTitle(partList?.ActiveSlot ?? GarageNovaPartPanelSlot.Frame);
// csharp-guardrails: allow-null-defense
            _partListCountLabel.text = partList?.CountText ?? "부품 0개";
// csharp-guardrails: allow-null-defense
            _partSearchField.SetValueWithoutNotify(partList?.SearchText ?? string.Empty);
// csharp-guardrails: allow-null-defense
            EnsurePartRowCapacity(partList?.Options?.Count ?? 0);
            RenderVisiblePartRows(partList);
        }

        public bool ScrollVisibleOptions(int deltaRows)
        {
// csharp-guardrails: allow-null-defense
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

        public void SuppressNextPartRowClick()
        {
            _suppressNextPartRowClick = true;
            _partListRows.schedule.Execute(() => _suppressNextPartRowClick = false).ExecuteLater(0);
        }

        private void BindCallbacks()
        {
            if (IsDisposed)
                return;

            _searchCallback = evt => SearchChanged?.Invoke(evt.newValue ?? string.Empty);
            _partSearchField.RegisterValueChangedCallback(_searchCallback);

            for (int i = 0; i < _partRows.Count; i++)
                BindPartRow(_partRows[i]);
        }

        private void RenderVisiblePartRows(GarageNovaPartsPanelViewModel partList)
        {
            for (int i = 0; i < _partRows.Count; i++)
            {
                var optionIndex = _visibleStartIndex + i;
// csharp-guardrails: allow-null-defense
                var option = partList != null && partList.Options != null && optionIndex < partList.Options.Count
                    ? partList.Options[optionIndex]
                    : null;
                RenderPartRow(_partRows[i], option);
            }
        }

        private void ResetScrollIfListChanged(GarageNovaPartsPanelViewModel partList)
        {
// csharp-guardrails: allow-null-defense
            var nextSlot = partList?.ActiveSlot ?? GarageNovaPartPanelSlot.Frame;
// csharp-guardrails: allow-null-defense
            var nextSearch = partList?.SearchText ?? string.Empty;
            if (nextSlot == _lastSlot && string.Equals(nextSearch, _lastSearchText, StringComparison.Ordinal))
                return;

            _lastSlot = nextSlot;
            _lastSearchText = nextSearch;
            _visibleStartIndex = 0;
        }

        private void EnsurePartRowCapacity(int requiredCount)
        {
            while (_partRows.Count < requiredCount)
            {
                int rowNumber = _partRows.Count + 1;
                var row = new Button { name = GarageUitkConstants.Parts.BuildRowName(rowNumber) };
                row.AddToClassList("part-row");

                var main = new VisualElement();
                main.AddToClassList("part-row-main");

                var nameLabel = new Label { name = GarageUitkConstants.Parts.BuildRowNameLabelName(rowNumber) };
                nameLabel.AddToClassList("part-row-name");

                var metaLabel = new Label { name = GarageUitkConstants.Parts.BuildRowMetaLabelName(rowNumber) };
                metaLabel.AddToClassList("part-row-meta");

                var badgeLabel = new Label { name = GarageUitkConstants.Parts.BuildRowBadgeLabelName(rowNumber) };
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
            binding.Row.focusable = false;
// csharp-guardrails: allow-null-defense
            binding.Clicked ??= () => SelectPartRow(binding);
            binding.Row.clicked += binding.Clicked;
        }

        private void SelectPartRow(PartRowBinding binding)
        {
            if (_suppressNextPartRowClick)
            {
                _suppressNextPartRowClick = false;
                return;
            }

            var option = binding.Option;
// csharp-guardrails: allow-null-defense
            if (option != null)
                OptionSelected?.Invoke(new GarageNovaPartSelection(option.Slot, option.Id));
        }

        private static void RenderPartRow(PartRowBinding binding, GarageNovaPartOptionViewModel option)
        {
            binding.Option = option;
            binding.Row.style.display = option != null ? DisplayStyle.Flex : DisplayStyle.None;
            if (option == null)
                return;

            binding.NameLabel.text = string.IsNullOrWhiteSpace(option.DisplayName) ? option.Id : option.DisplayName;
// csharp-guardrails: allow-null-defense
            binding.MetaLabel.text = option.MetaText ?? string.Empty;
            binding.BadgeLabel.text = BuildPartBadgeText(option);
            binding.BadgeLabel.style.display = string.IsNullOrWhiteSpace(binding.BadgeLabel.text)
                ? DisplayStyle.None
                : DisplayStyle.Flex;
            UitkElementUtility.SetClass(binding.Row, GarageUitkConstants.Classes.Part.RowSelected, option.IsSelected);
            UitkElementUtility.SetClass(binding.BadgeLabel, GarageUitkConstants.Classes.Part.BadgeSelected, option.IsEquipped);
            UitkElementUtility.SetClass(binding.BadgeLabel, GarageUitkConstants.Classes.Part.BadgeReview, option.NeedsNameReview && !option.IsEquipped);
        }

        private static string BuildPartListTitle(GarageNovaPartPanelSlot slot)
        {
            return slot switch
            {
                GarageNovaPartPanelSlot.Firepower => "무장 선택",
                GarageNovaPartPanelSlot.Mobility => "이동 선택",
                _ => "프레임 선택",
            };
        }

        private static string BuildPartBadgeText(GarageNovaPartOptionViewModel option)
        {
            if (option.IsEquipped)
                return "장착중";

            if (option.NeedsNameReview)
                return "검토";

            return string.Empty;
        }

        protected override void DisposeSurface()
        {
            // csharp-guardrails: allow-null-defense
            if (_partSearchField != null && _searchCallback != null)
                _partSearchField.UnregisterValueChangedCallback(_searchCallback);

            for (int i = 0; i < _partRows.Count; i++)
                UnbindPartRow(_partRows[i]);

            _searchCallback = null;
        }

        private void UnbindPartRow(PartRowBinding binding)
        {
// csharp-guardrails: allow-null-defense
            if (binding.Row != null && binding.Clicked != null)
                binding.Row.clicked -= binding.Clicked;
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
            public Action Clicked { get; set; }
        }
    }
}
