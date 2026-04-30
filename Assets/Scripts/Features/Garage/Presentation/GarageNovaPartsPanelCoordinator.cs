using System;
using System.Collections.Generic;

namespace Features.Garage.Presentation
{
    public sealed class GarageNovaPartsPanelCoordinator
    {
        private const int MaxVisibleRows = 8;

        private readonly GarageNovaPartsPanelView _view;
        private GarageNovaPartPanelSlot _activeSlot = GarageNovaPartPanelSlot.Frame;
        private string _searchText = string.Empty;
        private string _selectedFrameId;
        private string _selectedFirepowerId;
        private string _selectedMobilityId;
        private GaragePanelCatalog _catalog;
        private GarageNovaPartsDraftSelection _draftSelection;

        public GarageNovaPartsPanelCoordinator(GarageNovaPartsPanelView view)
        {
            _view = view;
        }

        public event Action<GarageNovaPartSelection> ApplyRequested;

        public void Bind()
        {
            if (_view == null)
                return;

            _view.Bind();
            _view.SlotFilterRequested += slot =>
            {
                _activeSlot = slot;
                RenderCached();
            };
            _view.SearchChanged += value =>
            {
                _searchText = value ?? string.Empty;
                RenderCached();
            };
            _view.OptionSelected += SelectOption;
            _view.ApplyRequested += ApplySelectedOption;
        }

        public void Render(
            GaragePanelCatalog catalog,
            GarageNovaPartsDraftSelection draftSelection,
            GarageEditorFocus currentFocus)
        {
            if (_view == null || catalog == null)
                return;

            _catalog = catalog;
            _draftSelection = draftSelection;
            _activeSlot = ToPanelSlot(currentFocus);
            RenderCached();
        }

        private void RenderCached()
        {
            if (_view == null || _catalog == null)
                return;

            EnsureSelectedFromDraft(_draftSelection);

            var allOptions = BuildOptions(_catalog, _activeSlot);
            var filteredOptions = FilterOptions(allOptions, _searchText);
            var selectedId = GetSelectedId(_activeSlot);
            if (string.IsNullOrWhiteSpace(selectedId) && filteredOptions.Count > 0)
            {
                selectedId = filteredOptions[0].Id;
                SetSelectedId(_activeSlot, selectedId);
            }

            var visibleOptions = new List<GarageNovaPartOptionViewModel>(MaxVisibleRows);
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
                    option.NeedsNameReview);

                if (isSelected)
                    selected = viewModel;

                if (visibleOptions.Count < MaxVisibleRows)
                    visibleOptions.Add(viewModel);
            }

            selected ??= visibleOptions.Count > 0 ? visibleOptions[0] : null;

            _view.Render(new GarageNovaPartsPanelViewModel(
                _activeSlot,
                _searchText,
                BuildCountText(filteredOptions.Count, allOptions.Count),
                selected != null ? selected.DisplayName : "No matching part",
                selected != null ? BuildSelectedDetailText(selected) : "Try another search term.",
                selected != null,
                selected?.PreviewPrefab,
                selected?.Alignment,
                visibleOptions));
        }

        private void SelectOption(GarageNovaPartSelection selection)
        {
            _activeSlot = selection.Slot;
            SetSelectedId(selection.Slot, selection.PartId);
            RenderCached();
        }

        private void ApplySelectedOption()
        {
            var selectedId = GetSelectedId(_activeSlot);
            if (string.IsNullOrWhiteSpace(selectedId))
                return;

            ApplyRequested?.Invoke(new GarageNovaPartSelection(_activeSlot, selectedId));
        }

        private void EnsureSelectedFromDraft(GarageNovaPartsDraftSelection draftSelection)
        {
            _selectedFrameId = (draftSelection.FrameId);
            _selectedFirepowerId = (draftSelection.FirepowerId);
            _selectedMobilityId = (draftSelection.MobilityId);
        }

        private string GetSelectedId(GarageNovaPartPanelSlot slot)
        {
            return slot switch
            {
                GarageNovaPartPanelSlot.Frame => _selectedFrameId,
                GarageNovaPartPanelSlot.Firepower => _selectedFirepowerId,
                _ => _selectedMobilityId,
            };
        }

        private void SetSelectedId(GarageNovaPartPanelSlot slot, string id)
        {
            switch (slot)
            {
                case GarageNovaPartPanelSlot.Frame:
                    _selectedFrameId = id;
                    break;
                case GarageNovaPartPanelSlot.Firepower:
                    _selectedFirepowerId = id;
                    break;
                case GarageNovaPartPanelSlot.Mobility:
                    _selectedMobilityId = id;
                    break;
            }
        }

        private static List<Candidate> BuildOptions(GaragePanelCatalog catalog, GarageNovaPartPanelSlot slot)
        {
            var options = new List<Candidate>();
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
                            part.NeedsNameReview));
                    }
                    break;
                case GarageNovaPartPanelSlot.Firepower:
                    for (int i = 0; i < catalog.Firepower.Count; i++)
                    {
                        var part = catalog.Firepower[i];
                        options.Add(new Candidate(
                            slot,
                            part.Id,
                            part.DisplayName,
                            $"ATK {part.AttackDamage:0} | RNG {part.Range:0.0} | T{part.Tier}",
                            part.SourcePath,
                            part.NeedsNameReview));
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
                            part.NeedsNameReview));
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

        private static string BuildCountText(int filteredCount, int totalCount)
        {
            return filteredCount == totalCount
                ? $"{totalCount} parts"
                : $"{filteredCount}/{totalCount} parts";
        }

        private static string BuildSelectedDetailText(GarageNovaPartOptionViewModel selected)
        {
            string source = string.IsNullOrWhiteSpace(selected.SourcePath) ? selected.Id : selected.SourcePath;
            return $"{selected.DetailText}\n{source}";
        }

        private static GarageNovaPartPanelSlot ToPanelSlot(GarageEditorFocus focus)
        {
            return focus switch
            {
                GarageEditorFocus.Firepower => GarageNovaPartPanelSlot.Firepower,
                GarageEditorFocus.Mobility => GarageNovaPartPanelSlot.Mobility,
                _ => GarageNovaPartPanelSlot.Frame,
            };
        }

        private readonly struct Candidate
        {
            public Candidate(
                GarageNovaPartPanelSlot slot,
                string id,
                string displayName,
                string detailText,
                string sourcePath,
                bool needsNameReview)
            {
                Slot = slot;
                Id = id;
                DisplayName = displayName;
                DetailText = detailText;
                SourcePath = sourcePath;
                NeedsNameReview = needsNameReview;
            }

            public GarageNovaPartPanelSlot Slot { get; }
            public string Id { get; }
            public string DisplayName { get; }
            public string DetailText { get; }
            public string SourcePath { get; }
            public bool NeedsNameReview { get; }
        }
    }
}
