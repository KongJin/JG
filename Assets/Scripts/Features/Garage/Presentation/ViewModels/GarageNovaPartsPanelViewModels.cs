using System;
using System.Collections.Generic;
using Features.Unit.Domain;
using UnityEngine;

namespace Features.Garage.Presentation
{
    public enum GarageNovaPartPanelSlot
    {
        Frame,
        Firepower,
        Mobility,
    }

    public readonly struct GarageNovaPartSelection
    {
        public GarageNovaPartSelection(GarageNovaPartPanelSlot slot, string partId)
        {
            Slot = slot;
            PartId = partId;
        }

        public GarageNovaPartPanelSlot Slot { get; }
        public string PartId { get; }
    }

    public readonly struct GarageNovaPartsDraftSelection
    {
        public GarageNovaPartsDraftSelection(string frameId, string firepowerId, string mobilityId)
        {
            FrameId = frameId;
            FirepowerId = firepowerId;
            MobilityId = mobilityId;
        }

        public string FrameId { get; }
        public string FirepowerId { get; }
        public string MobilityId { get; }
    }

    public readonly struct GarageNovaPartStatViewModel
    {
        public GarageNovaPartStatViewModel(string label, string valueText, float percent)
        {
            Label = label ?? string.Empty;
            ValueText = valueText ?? string.Empty;
            Percent = Mathf.Clamp(percent, 4f, 100f);
        }

        public string Label { get; }
        public string ValueText { get; }
        public float Percent { get; }
    }

    public sealed class GarageNovaPartOptionViewModel
    {
        public GarageNovaPartOptionViewModel(
            GarageNovaPartPanelSlot slot,
            string id,
            string displayName,
            string detailText,
            string sourcePath,
            bool isSelected,
            bool needsNameReview,
            GameObject previewPrefab = null,
            GaragePanelCatalog.PartAlignment alignment = null,
            string metaText = null,
            string energyText = null,
            IReadOnlyList<GarageNovaPartStatViewModel> stats = null)
        {
            Slot = slot;
            Id = id;
            DisplayName = displayName;
            DetailText = detailText;
            SourcePath = sourcePath;
            IsSelected = isSelected;
            NeedsNameReview = needsNameReview;
            PreviewPrefab = previewPrefab;
            Alignment = alignment;
            MetaText = string.IsNullOrWhiteSpace(metaText) ? detailText : metaText;
            EnergyText = energyText ?? string.Empty;
            Stats = stats ?? Array.Empty<GarageNovaPartStatViewModel>();
        }

        public GarageNovaPartPanelSlot Slot { get; }
        public string Id { get; }
        public string DisplayName { get; }
        public string DetailText { get; }
        public string SourcePath { get; }
        public bool IsSelected { get; }
        public bool NeedsNameReview { get; }
        public GameObject PreviewPrefab { get; }
        public GaragePanelCatalog.PartAlignment Alignment { get; }
        public string MetaText { get; }
        public string EnergyText { get; }
        public IReadOnlyList<GarageNovaPartStatViewModel> Stats { get; }
    }

    public sealed class GarageNovaPartsPanelViewModel
    {
        public GarageNovaPartsPanelViewModel(
            GarageNovaPartPanelSlot activeSlot,
            string searchText,
            string countText,
            string selectedNameText,
            string selectedDetailText,
            GameObject selectedPreviewPrefab,
            GaragePanelCatalog.PartAlignment selectedAlignment,
            IReadOnlyList<GarageNovaPartOptionViewModel> options,
            string selectedPartId = null,
            string selectedEnergyText = null,
            string selectedMetaText = null,
            IReadOnlyList<GarageNovaPartStatViewModel> selectedStats = null)
        {
            ActiveSlot = activeSlot;
            SearchText = searchText;
            CountText = countText;
            SelectedNameText = selectedNameText;
            SelectedDetailText = selectedDetailText;
            SelectedPreviewPrefab = selectedPreviewPrefab;
            SelectedAlignment = selectedAlignment;
            Options = options;
            SelectedPartId = selectedPartId ?? string.Empty;
            SelectedEnergyText = selectedEnergyText ?? string.Empty;
            SelectedMetaText = selectedMetaText ?? string.Empty;
            SelectedStats = selectedStats ?? Array.Empty<GarageNovaPartStatViewModel>();
        }

        public GarageNovaPartPanelSlot ActiveSlot { get; }
        public string SearchText { get; }
        public string CountText { get; }
        public string SelectedNameText { get; }
        public string SelectedDetailText { get; }
        public GameObject SelectedPreviewPrefab { get; }
        public GaragePanelCatalog.PartAlignment SelectedAlignment { get; }
        public IReadOnlyList<GarageNovaPartOptionViewModel> Options { get; }
        public string SelectedPartId { get; }
        public string SelectedEnergyText { get; }
        public string SelectedMetaText { get; }
        public IReadOnlyList<GarageNovaPartStatViewModel> SelectedStats { get; }

        public static GarageNovaPartsPanelViewModel Empty => new(
            GarageNovaPartPanelSlot.Mobility,
            string.Empty,
            "부품 0개",
            "선택 대기",
            string.Empty,
            null,
            null,
            Array.Empty<GarageNovaPartOptionViewModel>());
    }

    public static class GarageNovaPartsPanelViewModelFactory
    {
        public static GarageNovaPartsPanelViewModel Build(
            GaragePanelCatalog catalog,
            GarageNovaPartsDraftSelection draftSelection,
            GarageEditorFocus currentFocus,
            string searchText)
        {
            return Build(catalog, draftSelection, GarageEditorFocusMapping.ToPanelSlot(currentFocus), searchText);
        }

        public static GarageNovaPartsPanelViewModel Build(
            GaragePanelCatalog catalog,
            GarageNovaPartsDraftSelection draftSelection,
            GarageNovaPartPanelSlot activeSlot,
            string searchText)
        {
            var normalizedSearch = searchText ?? string.Empty;
            var allOptions = GarageNovaPartsPanelOptionBuilder.BuildOptions(catalog, activeSlot, draftSelection);
            var filteredOptions = GarageNovaPartsPanelOptionBuilder.FilterOptions(allOptions, normalizedSearch);
            string selectedId = GetSelectedId(draftSelection, activeSlot);
            var visibleOptions = new List<GarageNovaPartOptionViewModel>(filteredOptions.Count);
            GarageNovaPartOptionViewModel selected = null;

            for (int i = 0; i < filteredOptions.Count; i++)
            {
                var option = filteredOptions[i];
                if (option.IsSelected)
                    selected = option;

                visibleOptions.Add(option);
            }

            selected ??= FindFirstSelected(allOptions, selectedId);

            return new GarageNovaPartsPanelViewModel(
                activeSlot,
                normalizedSearch,
                BuildCountText(filteredOptions.Count, allOptions.Count),
                selected != null ? selected.DisplayName : "선택 대기",
                selected != null ? BuildSelectedDetailText(selected) : "탭 아래 리스트에서 부품을 선택하세요.",
                selected?.PreviewPrefab,
                selected?.Alignment,
                visibleOptions,
                selected?.Id,
                selected?.EnergyText,
                selected?.MetaText,
                selected?.Stats);
        }

        public static GarageNovaPartPanelSlot ToPanelSlot(GarageEditorFocus focus)
            => GarageEditorFocusMapping.ToPanelSlot(focus);

        public static GarageEditorFocus ToEditorFocus(GarageNovaPartPanelSlot slot)
            => GarageEditorFocusMapping.ToEditorFocus(slot);

        private static string GetSelectedId(GarageNovaPartsDraftSelection draftSelection, GarageNovaPartPanelSlot slot)
        {
            return slot switch
            {
                GarageNovaPartPanelSlot.Frame => draftSelection.FrameId,
                GarageNovaPartPanelSlot.Firepower => draftSelection.FirepowerId,
                _ => draftSelection.MobilityId,
            };
        }

        private static GarageNovaPartOptionViewModel FindFirstSelected(
            List<GarageNovaPartOptionViewModel> options,
            string selectedId)
        {
            if (string.IsNullOrWhiteSpace(selectedId))
                return null;

            for (int i = 0; i < options.Count; i++)
            {
                var option = options[i];
                if (option.Id != selectedId)
                    continue;

                return option;
            }

            return null;
        }

        private static string BuildCountText(int filteredCount, int totalCount)
        {
            return filteredCount == totalCount
                ? $"부품 {totalCount}개"
                : $"부품 {filteredCount}/{totalCount}개";
        }

        private static string BuildSelectedDetailText(GarageNovaPartOptionViewModel selected)
        {
            string source = string.IsNullOrWhiteSpace(selected.SourcePath) ? selected.Id : selected.SourcePath;
            return $"{selected.DetailText}\n{source}";
        }

    }
}
