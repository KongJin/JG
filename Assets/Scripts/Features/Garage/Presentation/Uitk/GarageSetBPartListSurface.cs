using System;
using Shared.Ui;
using UnityEngine;
using UnityEngine.UIElements;

namespace Features.Garage.Presentation
{
    internal sealed class GarageSetBPartListSurface : BaseSurface<VisualElement>
    {
        private readonly GarageSetBPartRowListSurface _rowListSurface;
        private readonly GarageSetBPartPreviewSurface _previewSurface;
        private readonly GarageSetBFocusSwipeHandler _focusSwipeHandler;
        private readonly Action<GarageEditorFocus> _focusSelected;
        private readonly Action<string> _searchChanged;
        private readonly Action<GarageNovaPartSelection> _optionSelected;

        public GarageSetBPartListSurface(VisualElement root)
            : base(root)
        {
            _rowListSurface = new GarageSetBPartRowListSurface(root);
            _previewSurface = new GarageSetBPartPreviewSurface(root);
            _focusSwipeHandler = new GarageSetBFocusSwipeHandler(
                root,
                _rowListSurface.SearchField,
                _rowListSurface.SuppressNextPartRowClick);

            _focusSelected = focus => FocusSelected?.Invoke(focus);
            _searchChanged = value => SearchChanged?.Invoke(value);
            _optionSelected = selection => OptionSelected?.Invoke(selection);

            _focusSwipeHandler.FocusSelected += _focusSelected;
            _rowListSurface.SearchChanged += _searchChanged;
            _rowListSurface.OptionSelected += _optionSelected;
        }

        public event Action<GarageEditorFocus> FocusSelected;
        public event Action<string> SearchChanged;
        public event Action<GarageNovaPartSelection> OptionSelected;

        public void Render(GarageNovaPartsPanelViewModel partList, GarageEditorFocus focusedPart)
        {
            _rowListSurface.Render(partList);
            _previewSurface.Render(partList);
            _focusSwipeHandler.Render(focusedPart);
        }

        public bool ScrollVisibleOptions(int deltaRows)
        {
            return _rowListSurface.ScrollVisibleOptions(deltaRows);
        }

        public void SetPreviewTexture(Texture texture, bool isVisible)
        {
            _previewSurface.SetPreviewTexture(texture, isVisible);
        }

        internal bool TrySelectFocusFromHorizontalDrag(Vector2 dragDelta)
        {
            return _focusSwipeHandler.TrySelectFocusFromHorizontalDrag(dragDelta);
        }

        protected override void DisposeSurface()
        {
            _focusSwipeHandler.FocusSelected -= _focusSelected;

            _rowListSurface.SearchChanged -= _searchChanged;
            _rowListSurface.OptionSelected -= _optionSelected;

            _focusSwipeHandler.Dispose();
            _rowListSurface.Dispose();
            _previewSurface.Dispose();
        }
    }
}
