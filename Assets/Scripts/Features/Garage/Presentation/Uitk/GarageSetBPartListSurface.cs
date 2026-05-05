using System;
using System.Collections.Generic;
using Shared.Ui;
using UnityEngine;
using UnityEngine.UIElements;

namespace Features.Garage.Presentation
{
    internal sealed class GarageSetBPartListSurface : BaseSurface<VisualElement>
    {
        private const float FocusSwipeThreshold = 42f;
        private const float FocusSwipeAxisBias = 1.25f;
        private static readonly GarageEditorFocus[] FocusOrder =
        {
            GarageEditorFocus.Mobility,
            GarageEditorFocus.Frame,
            GarageEditorFocus.Firepower
        };

        private readonly Button _frameTabButton;
        private readonly Button _firepowerTabButton;
        private readonly Button _mobilityTabButton;
        private readonly VisualElement _swipeHost;
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
        private GarageEditorFocus _lastFocusedPart = GarageEditorFocus.Mobility;
        private string _lastSearchText = string.Empty;
        private int _visibleStartIndex;
        private int _swipePointerId = -1;
        private Vector2 _swipeStartPosition;
        private bool _isSwipeActive;
        private bool _suppressNextPartRowClick;
        private readonly Action _frameTabClicked;
        private readonly Action _firepowerTabClicked;
        private readonly Action _mobilityTabClicked;
        private EventCallback<ChangeEvent<string>> _searchCallback;
        private EventCallback<PointerDownEvent> _swipePointerDown;
        private EventCallback<PointerMoveEvent> _swipePointerMove;
        private EventCallback<PointerUpEvent> _swipePointerUp;
        private EventCallback<PointerCancelEvent> _swipePointerCancel;

        public GarageSetBPartListSurface(VisualElement root)
            : base(root)
        {
            _frameTabButton = UitkElementUtility.Required<Button>(root, "FrameTabButton");
            _firepowerTabButton = UitkElementUtility.Required<Button>(root, "FirepowerTabButton");
            _mobilityTabButton = UitkElementUtility.Required<Button>(root, "MobilityTabButton");
            _swipeHost = UitkElementUtility.Required<VisualElement>(root, "PartSelectionPane");
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
            for (int i = 0; i < GarageUitkConstants.Parts.InitialRowCount; i++)
            {
                int rowNumber = i + 1;
                _partRows.Add(new PartRowBinding(
                    UitkElementUtility.Required<Button>(root, $"PartRow{rowNumber:00}"),
                    UitkElementUtility.Required<Label>(root, $"PartRow{rowNumber:00}NameLabel"),
                    UitkElementUtility.Required<Label>(root, $"PartRow{rowNumber:00}MetaLabel"),
                    UitkElementUtility.Required<Label>(root, $"PartRow{rowNumber:00}BadgeLabel")));
            }

            _frameTabClicked = SelectFrameFocus;
            _firepowerTabClicked = SelectFirepowerFocus;
            _mobilityTabClicked = SelectMobilityFocus;
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
            if (IsDisposed)
                return;

            _frameTabButton.clicked += _frameTabClicked;
            _firepowerTabButton.clicked += _firepowerTabClicked;
            _mobilityTabButton.clicked += _mobilityTabClicked;

            _searchCallback = evt => SearchChanged?.Invoke(evt.newValue ?? string.Empty);
            _partSearchField.RegisterValueChangedCallback(_searchCallback);

            _swipePointerDown = BeginFocusSwipe;
            _swipePointerMove = UpdateFocusSwipe;
            _swipePointerUp = EndFocusSwipe;
            _swipePointerCancel = CancelFocusSwipe;
            _swipeHost.RegisterCallback(_swipePointerDown);
            _swipeHost.RegisterCallback(_swipePointerMove);
            _swipeHost.RegisterCallback(_swipePointerUp);
            _swipeHost.RegisterCallback(_swipePointerCancel);

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
            EnsurePartRowCapacity(partList?.Options?.Count ?? 0);
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
            _partPreviewMetaLabel.text = BuildPartPreviewMeta(slot, partList?.SelectedMetaText);
            _partPreviewLabel.text = BuildPartPreviewPlaceholderText(slot);
            RenderPartEnergy(partList?.SelectedEnergyText);
            RenderPartPreviewStats(partList?.SelectedStats);
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
            binding.Clicked ??= () => SelectPartRow(binding);
            binding.Row.clicked += binding.Clicked;
        }

        private void SelectFrameFocus()
        {
            FocusSelected?.Invoke(GarageEditorFocus.Frame);
        }

        private void SelectFirepowerFocus()
        {
            FocusSelected?.Invoke(GarageEditorFocus.Firepower);
        }

        private void SelectMobilityFocus()
        {
            FocusSelected?.Invoke(GarageEditorFocus.Mobility);
        }

        private void SelectPartRow(PartRowBinding binding)
        {
            if (_suppressNextPartRowClick)
            {
                _suppressNextPartRowClick = false;
                return;
            }

            var option = binding.Option;
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
            binding.MetaLabel.text = option.MetaText ?? string.Empty;
            binding.BadgeLabel.text = BuildPartBadgeText(option);
            binding.BadgeLabel.style.display = string.IsNullOrWhiteSpace(binding.BadgeLabel.text)
                ? DisplayStyle.None
                : DisplayStyle.Flex;
            UitkElementUtility.SetClass(binding.Row, GarageUitkConstants.Classes.Part.RowSelected, option.IsSelected);
            UitkElementUtility.SetClass(binding.BadgeLabel, GarageUitkConstants.Classes.Part.BadgeSelected, option.IsEquipped);
            UitkElementUtility.SetClass(binding.BadgeLabel, GarageUitkConstants.Classes.Part.BadgeReview, option.NeedsNameReview && !option.IsEquipped);
        }

        private void RenderFocusTabs(GarageEditorFocus focusedPart)
        {
            _lastFocusedPart = focusedPart;
            UitkElementUtility.SetClass(_frameTabButton, "focus-tab--active", focusedPart == GarageEditorFocus.Frame);
            UitkElementUtility.SetClass(_firepowerTabButton, "focus-tab--active", focusedPart == GarageEditorFocus.Firepower);
            UitkElementUtility.SetClass(_mobilityTabButton, "focus-tab--active", focusedPart == GarageEditorFocus.Mobility);
        }

        internal bool TrySelectFocusFromHorizontalDrag(Vector2 dragDelta)
        {
            float absoluteX = Mathf.Abs(dragDelta.x);
            float absoluteY = Mathf.Abs(dragDelta.y);
            if (absoluteX < FocusSwipeThreshold || absoluteX < absoluteY * FocusSwipeAxisBias)
                return false;

            int direction = dragDelta.x < 0f ? 1 : -1;
            if (!TryResolveAdjacentFocus(_lastFocusedPart, direction, out var nextFocus))
                return false;

            FocusSelected?.Invoke(nextFocus);
            return true;
        }

        private void BeginFocusSwipe(PointerDownEvent evt)
        {
            if (evt == null ||
                _swipePointerId >= 0 ||
                IsInsideElement(evt.target as VisualElement, _partSearchField))
                return;

            _swipePointerId = evt.pointerId;
            _swipeStartPosition = ToVector2(evt.position);
            _isSwipeActive = false;
            _swipeHost.CapturePointer(evt.pointerId);
        }

        private void UpdateFocusSwipe(PointerMoveEvent evt)
        {
            if (evt == null || evt.pointerId != _swipePointerId)
                return;

            var delta = ToVector2(evt.position) - _swipeStartPosition;
            if (!_isSwipeActive &&
                Mathf.Abs(delta.x) >= FocusSwipeThreshold &&
                Mathf.Abs(delta.x) >= Mathf.Abs(delta.y) * FocusSwipeAxisBias)
            {
                _isSwipeActive = true;
                SuppressNextPartRowClick();
            }

            if (_isSwipeActive)
                evt.StopPropagation();
        }

        private void EndFocusSwipe(PointerUpEvent evt)
        {
            if (evt == null || evt.pointerId != _swipePointerId)
                return;

            var delta = ToVector2(evt.position) - _swipeStartPosition;
            ReleaseSwipePointer(evt.pointerId);
            ResetFocusSwipe();

            if (!TrySelectFocusFromHorizontalDrag(delta))
                return;

            SuppressNextPartRowClick();
            evt.StopImmediatePropagation();
        }

        private void CancelFocusSwipe(PointerCancelEvent evt)
        {
            if (evt == null || evt.pointerId != _swipePointerId)
                return;

            ReleaseSwipePointer(evt.pointerId);
            ResetFocusSwipe();
        }

        private void ReleaseSwipePointer(int pointerId)
        {
            if (_swipeHost.HasPointerCapture(pointerId))
                _swipeHost.ReleasePointer(pointerId);
        }

        private void ResetFocusSwipe()
        {
            _swipePointerId = -1;
            _swipeStartPosition = Vector2.zero;
            _isSwipeActive = false;
        }

        private void SuppressNextPartRowClick()
        {
            _suppressNextPartRowClick = true;
            _swipeHost.schedule.Execute(() => _suppressNextPartRowClick = false).ExecuteLater(0);
        }

        private static bool TryResolveAdjacentFocus(
            GarageEditorFocus currentFocus,
            int direction,
            out GarageEditorFocus nextFocus)
        {
            nextFocus = currentFocus;
            int currentIndex = 0;
            for (int i = 0; i < FocusOrder.Length; i++)
            {
                if (FocusOrder[i] == currentFocus)
                {
                    currentIndex = i;
                    break;
                }
            }

            int nextIndex = currentIndex + direction;
            if (nextIndex < 0 || nextIndex >= FocusOrder.Length)
                return false;

            nextFocus = FocusOrder[nextIndex];
            return true;
        }

        private static bool IsInsideElement(VisualElement target, VisualElement ancestor)
        {
            while (target != null)
            {
                if (ReferenceEquals(target, ancestor))
                    return true;

                target = target.parent;
            }

            return false;
        }

        private static Vector2 ToVector2(Vector3 position)
        {
            return new Vector2(position.x, position.y);
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
            if (option.IsEquipped)
                return "장착중";

            if (option.NeedsNameReview)
                return "검토";

            return string.Empty;
        }

        private static string BuildSelectedPartKicker(GarageNovaPartsPanelViewModel partList)
        {
            if (partList == null)
                return "현재 선택";

            return partList.SelectedPartIsEquipped ? "현재 장착" : "선택 후보";
        }

        private static string BuildPartPreviewMeta(GarageNovaPartPanelSlot slot, string selectedDetailText)
        {
            if (string.IsNullOrWhiteSpace(selectedDetailText))
                return BuildPartListTitle(slot);

            int lineBreak = selectedDetailText.IndexOf('\n');
            var firstLine = lineBreak >= 0 ? selectedDetailText.Substring(0, lineBreak) : selectedDetailText;
            return StripTierText(firstLine);
        }

        private void RenderPartPreviewStats(IReadOnlyList<GarageNovaPartStatViewModel> stats)
        {
            for (int i = 0; i < _partStatBars.Length; i++)
            {
                bool hasStat = stats != null && i < stats.Count;
                var bar = _partStatBars[i];
                bar.Row.style.display = hasStat ? DisplayStyle.Flex : DisplayStyle.None;
                if (!hasStat)
                    continue;

                var stat = stats[i];
                bar.Label.text = stat.Label;
                bar.Value.text = stat.ValueText;
                bar.Fill.style.height = Length.Percent(stat.Percent);
            }
        }

        private void RenderPartEnergy(string energyText)
        {
            if (_partEnergyLabel == null)
                return;

            bool hasEnergy = !string.IsNullOrWhiteSpace(energyText);
            _partEnergyLabel.text = hasEnergy ? energyText.Trim() : string.Empty;
            _partEnergyLabel.style.display = hasEnergy ? DisplayStyle.Flex : DisplayStyle.None;
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
                if (IsTierToken(token))
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

        private static string BuildPartPreviewPlaceholderText(GarageNovaPartPanelSlot slot)
        {
            return slot switch
            {
                GarageNovaPartPanelSlot.Firepower => "무장",
                GarageNovaPartPanelSlot.Frame => "프레임",
                _ => "기동",
            };
        }

        protected override void DisposeSurface()
        {
            if (_frameTabButton != null)
                _frameTabButton.clicked -= _frameTabClicked;

            if (_firepowerTabButton != null)
                _firepowerTabButton.clicked -= _firepowerTabClicked;

            if (_mobilityTabButton != null)
                _mobilityTabButton.clicked -= _mobilityTabClicked;

            if (_partSearchField != null && _searchCallback != null)
                _partSearchField.UnregisterValueChangedCallback(_searchCallback);

            if (_swipeHost != null && _swipePointerDown != null)
                _swipeHost.UnregisterCallback(_swipePointerDown);

            if (_swipeHost != null && _swipePointerMove != null)
                _swipeHost.UnregisterCallback(_swipePointerMove);

            if (_swipeHost != null && _swipePointerUp != null)
                _swipeHost.UnregisterCallback(_swipePointerUp);

            if (_swipeHost != null && _swipePointerCancel != null)
                _swipeHost.UnregisterCallback(_swipePointerCancel);

            for (int i = 0; i < _partRows.Count; i++)
                UnbindPartRow(_partRows[i]);

            _searchCallback = null;
            _swipePointerDown = null;
            _swipePointerMove = null;
            _swipePointerUp = null;
            _swipePointerCancel = null;
        }

        private void UnbindPartRow(PartRowBinding binding)
        {
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
