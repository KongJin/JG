using System;
using System.Collections.Generic;
using Features.Unit.Infrastructure;
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
            GaragePanelCatalog.PartAlignment alignment = null)
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
    }

    public sealed class GarageNovaPartsPanelViewModel
    {
        public GarageNovaPartsPanelViewModel(
            GarageNovaPartPanelSlot activeSlot,
            string searchText,
            string countText,
            string selectedNameText,
            string selectedDetailText,
            bool canApply,
            GameObject selectedPreviewPrefab,
            GaragePanelCatalog.PartAlignment selectedAlignment,
            IReadOnlyList<GarageNovaPartOptionViewModel> options)
        {
            ActiveSlot = activeSlot;
            SearchText = searchText;
            CountText = countText;
            SelectedNameText = selectedNameText;
            SelectedDetailText = selectedDetailText;
            CanApply = canApply;
            SelectedPreviewPrefab = selectedPreviewPrefab;
            SelectedAlignment = selectedAlignment;
            Options = options;
        }

        public GarageNovaPartPanelSlot ActiveSlot { get; }
        public string SearchText { get; }
        public string CountText { get; }
        public string SelectedNameText { get; }
        public string SelectedDetailText { get; }
        public bool CanApply { get; }
        public GameObject SelectedPreviewPrefab { get; }
        public GaragePanelCatalog.PartAlignment SelectedAlignment { get; }
        public IReadOnlyList<GarageNovaPartOptionViewModel> Options { get; }
    }

    public static class GarageNovaPartsPanelViewModelFactory
    {
        public static GarageNovaPartsPanelViewModel Build(
            GaragePanelCatalog catalog,
            GarageNovaPartsDraftSelection draftSelection,
            GarageEditorFocus currentFocus,
            string searchText)
        {
            return Build(catalog, draftSelection, ToPanelSlot(currentFocus), searchText);
        }

        public static GarageNovaPartsPanelViewModel Build(
            GaragePanelCatalog catalog,
            GarageNovaPartsDraftSelection draftSelection,
            GarageNovaPartPanelSlot activeSlot,
            string searchText)
        {
            var normalizedSearch = searchText ?? string.Empty;
            var allOptions = BuildOptions(catalog, activeSlot, draftSelection);
            var filteredOptions = FilterOptions(allOptions, normalizedSearch);
            string selectedId = GetSelectedId(draftSelection, activeSlot);
            var visibleOptions = new List<GarageNovaPartOptionViewModel>(filteredOptions.Count);
            GarageNovaPartOptionViewModel selected = null;

            for (int i = 0; i < filteredOptions.Count; i++)
            {
                var option = filteredOptions[i];
                bool isSelected = option.Id == selectedId;
                var viewModel = new GarageNovaPartOptionViewModel(
                    option.Slot,
                    option.Id,
                    option.DisplayName,
                    option.DetailText,
                    option.SourcePath,
                    isSelected,
                    option.NeedsNameReview,
                    option.PreviewPrefab,
                    option.Alignment);

                if (isSelected)
                    selected = viewModel;

                visibleOptions.Add(viewModel);
            }

            selected ??= FindFirstSelected(allOptions, selectedId);

            return new GarageNovaPartsPanelViewModel(
                activeSlot,
                normalizedSearch,
                BuildCountText(filteredOptions.Count, allOptions.Count),
                selected != null ? selected.DisplayName : "선택 대기",
                selected != null ? BuildSelectedDetailText(selected) : "탭 아래 리스트에서 부품을 선택하세요.",
                filteredOptions.Count > 0,
                selected?.PreviewPrefab,
                selected?.Alignment,
                visibleOptions);
        }

        public static GarageNovaPartPanelSlot ToPanelSlot(GarageEditorFocus focus)
        {
            return focus switch
            {
                GarageEditorFocus.Firepower => GarageNovaPartPanelSlot.Firepower,
                GarageEditorFocus.Mobility => GarageNovaPartPanelSlot.Mobility,
                _ => GarageNovaPartPanelSlot.Frame,
            };
        }

        public static GarageEditorFocus ToEditorFocus(GarageNovaPartPanelSlot slot)
        {
            return slot switch
            {
                GarageNovaPartPanelSlot.Firepower => GarageEditorFocus.Firepower,
                GarageNovaPartPanelSlot.Mobility => GarageEditorFocus.Mobility,
                _ => GarageEditorFocus.Frame,
            };
        }

        private static List<Candidate> BuildOptions(GaragePanelCatalog catalog, GarageNovaPartPanelSlot slot, GarageNovaPartsDraftSelection draftSelection)
        {
            var options = new List<Candidate>();
            if (catalog == null)
                return options;

            var selectedFrame = catalog.FindFrame(draftSelection.FrameId);
            switch (slot)
            {
                case GarageNovaPartPanelSlot.Frame:
                    for (int i = 0; i < catalog.Frames.Count; i++)
                    {
                        var part = catalog.Frames[i];
                        options.Add(new Candidate(
                            slot,
                            part.Id,
                            part.DisplayName,
                            $"HP {part.BaseHp:0} | ASPD {part.BaseAttackSpeed:0.00} | T{part.Tier}",
                            part.SourcePath,
                            part.NeedsNameReview,
                            part.PreviewPrefab,
                            part.Alignment));
                    }
                    break;
                case GarageNovaPartPanelSlot.Firepower:
                    for (int i = 0; i < catalog.Firepower.Count; i++)
                    {
                        var part = catalog.Firepower[i];
                        if (selectedFrame != null && !UnitPartCompatibility.AreAssemblyFormsCompatible(selectedFrame.AssemblyForm, part.AssemblyForm))
                            continue;

                        options.Add(new Candidate(
                            slot,
                            part.Id,
                            part.DisplayName,
                            $"ATK {part.AttackDamage:0} | RNG {part.Range:0.0} | T{part.Tier}",
                            part.SourcePath,
                            part.NeedsNameReview,
                            part.PreviewPrefab,
                            part.Alignment));
                    }
                    break;
                case GarageNovaPartPanelSlot.Mobility:
                    for (int i = 0; i < catalog.Mobility.Count; i++)
                    {
                        var part = catalog.Mobility[i];
                        options.Add(new Candidate(
                            slot,
                            part.Id,
                            part.DisplayName,
                            $"HP+ {part.HpBonus:0} | MOV {part.MoveRange:0.0} | T{part.Tier}",
                            part.SourcePath,
                            part.NeedsNameReview,
                            part.PreviewPrefab,
                            part.Alignment));
                    }
                    break;
            }

            return options;
        }

        private static List<Candidate> FilterOptions(List<Candidate> options, string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return options;

            var filtered = new List<Candidate>();
            for (int i = 0; i < options.Count; i++)
            {
                var option = options[i];
                if (Contains(option.Id, searchText) ||
                    Contains(option.DisplayName, searchText) ||
                    Contains(option.SourcePath, searchText))
                {
                    filtered.Add(option);
                }
            }

            return filtered;
        }

        private static bool Contains(string text, string searchText)
        {
            return !string.IsNullOrWhiteSpace(text) &&
                   text.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string GetSelectedId(GarageNovaPartsDraftSelection draftSelection, GarageNovaPartPanelSlot slot)
        {
            return slot switch
            {
                GarageNovaPartPanelSlot.Frame => draftSelection.FrameId,
                GarageNovaPartPanelSlot.Firepower => draftSelection.FirepowerId,
                _ => draftSelection.MobilityId,
            };
        }

        private static GarageNovaPartOptionViewModel FindFirstSelected(List<Candidate> options, string selectedId)
        {
            if (string.IsNullOrWhiteSpace(selectedId))
                return null;

            for (int i = 0; i < options.Count; i++)
            {
                var option = options[i];
                if (option.Id != selectedId)
                    continue;

                return new GarageNovaPartOptionViewModel(
                    option.Slot,
                    option.Id,
                    option.DisplayName,
                    option.DetailText,
                    option.SourcePath,
                    isSelected: true,
                    option.NeedsNameReview,
                    option.PreviewPrefab,
                    option.Alignment);
            }

            return null;
        }

        private static string BuildCountText(int filteredCount, int totalCount)
        {
            return filteredCount == totalCount
                ? $"{totalCount} PARTS"
                : $"{filteredCount}/{totalCount} PARTS";
        }

        private static string BuildSelectedDetailText(GarageNovaPartOptionViewModel selected)
        {
            string source = string.IsNullOrWhiteSpace(selected.SourcePath) ? selected.Id : selected.SourcePath;
            return $"{selected.DetailText}\n{source}";
        }

        private readonly struct Candidate
        {
            public Candidate(
                GarageNovaPartPanelSlot slot,
                string id,
                string displayName,
                string detailText,
                string sourcePath,
                bool needsNameReview,
                GameObject previewPrefab,
                GaragePanelCatalog.PartAlignment alignment)
            {
                Slot = slot;
                Id = id;
                DisplayName = displayName;
                DetailText = detailText;
                SourcePath = sourcePath;
                NeedsNameReview = needsNameReview;
                PreviewPrefab = previewPrefab;
                Alignment = alignment;
            }

            public GarageNovaPartPanelSlot Slot { get; }
            public string Id { get; }
            public string DisplayName { get; }
            public string DetailText { get; }
            public string SourcePath { get; }
            public bool NeedsNameReview { get; }
            public GameObject PreviewPrefab { get; }
            public GaragePanelCatalog.PartAlignment Alignment { get; }
        }
    }
}
