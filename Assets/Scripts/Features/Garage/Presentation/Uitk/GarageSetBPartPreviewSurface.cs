using System;
using System.Collections.Generic;
using Shared.Ui;
using UnityEngine;
using UnityEngine.UIElements;

namespace Features.Garage.Presentation
{
    internal sealed class GarageSetBPartPreviewSurface : BaseSurface<VisualElement>
    {
        private readonly Label _partPreviewKickerLabel;
        private readonly Label _partPreviewTitleLabel;
        private readonly Label _partEnergyLabel;
        private readonly Label _partPreviewMetaLabel;
        private readonly Label _partPreviewLabel;
        private readonly Image _partPreviewImage;
        private readonly PartStatBarBinding[] _partStatBars;

        public GarageSetBPartPreviewSurface(VisualElement root)
            : base(root)
        {
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

            SetPreviewTexture(null, false);
        }

        public void Render(GarageNovaPartsPanelViewModel partList)
        {
            var slot = GarageNovaPartPanelSlot.Mobility;
            var selectedNameText = string.Empty;
            var selectedMetaText = string.Empty;
            var selectedEnergyText = string.Empty;
            IReadOnlyList<GarageNovaPartStatViewModel> selectedStats = null;

            if (partList != null)
            {
                slot = partList.ActiveSlot;
                selectedNameText = partList.SelectedNameText;
                selectedMetaText = partList.SelectedMetaText;
                selectedEnergyText = partList.SelectedEnergyText;
                selectedStats = partList.SelectedStats;
            }

            _partPreviewKickerLabel.text = BuildSelectedPartKicker(partList);
            _partPreviewTitleLabel.text = string.IsNullOrWhiteSpace(selectedNameText)
                ? BuildPartListTitle(slot)
                : selectedNameText;
            _partPreviewMetaLabel.text = BuildPartPreviewMeta(slot, selectedMetaText);
            _partPreviewLabel.text = BuildPartPreviewPlaceholderText(slot);
            RenderPartEnergy(selectedEnergyText);
            RenderPartPreviewStats(selectedStats);
        }

        public void SetPreviewTexture(Texture texture, bool isVisible)
        {
            _partPreviewImage.image = isVisible ? texture : null;
            _partPreviewImage.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
            _partPreviewLabel.style.display = isVisible ? DisplayStyle.None : DisplayStyle.Flex;
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
            var hasEnergy = !string.IsNullOrWhiteSpace(energyText);
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

        private static string BuildPartListTitle(GarageNovaPartPanelSlot slot)
        {
            return slot switch
            {
                GarageNovaPartPanelSlot.Firepower => "무장 선택",
                GarageNovaPartPanelSlot.Mobility => "기동 선택",
                _ => "프레임 선택",
            };
        }

        protected override void DisposeSurface()
        {
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
