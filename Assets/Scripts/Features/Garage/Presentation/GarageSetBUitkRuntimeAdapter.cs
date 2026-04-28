using System;
using System.Collections.Generic;
using Shared.Runtime;
using Shared.Runtime.Pooling;
using UnityEngine;
using UnityEngine.UIElements;

namespace Features.Garage.Presentation
{
    public sealed class GarageSetBUitkRuntimeAdapter : MonoBehaviour
    {
        [SerializeField]
        private UIDocument _document;

        [SerializeField]
        private GarageSetBUitkPreviewRenderer _previewRenderer;

        private GarageSetBUitkSurface _surface;

        public event Action<int> SlotSelected;
        public event Action<GarageEditorFocus> PartFocusSelected;
        public event Action SaveRequested;
        public event Action SettingsRequested;

        public bool Bind()
        {
            if (_surface != null)
                return true;

            if (_document == null)
                return false;

            var root = _document.rootVisualElement;
            if (root == null)
                return false;

            _surface = new GarageSetBUitkSurface(root);
            _surface.SlotSelected += slotIndex => SlotSelected?.Invoke(slotIndex);
            _surface.PartFocusSelected += focus => PartFocusSelected?.Invoke(focus);
            _surface.SaveRequested += () => SaveRequested?.Invoke();
            _surface.SettingsRequested += () => SettingsRequested?.Invoke();
            return true;
        }

        public void Render(
            IReadOnlyList<GarageSlotViewModel> slots,
            GarageEditorViewModel editor,
            GarageResultViewModel result,
            GarageEditorFocus focusedPart,
            bool isSaving)
        {
            if (!Bind())
                return;

            _surface.Render(slots, editor, result, focusedPart, isSaving);
            RenderPreview(slots);
        }

        private void OnEnable()
        {
            Bind();
        }

        private void Reset()
        {
            if (_document == null)
                _document = ComponentAccess.Get<UIDocument>(gameObject);
        }

        private void RenderPreview(IReadOnlyList<GarageSlotViewModel> slots)
        {
            if (_previewRenderer == null)
            {
                _surface.SetPreviewTexture(null, false);
                return;
            }

            var selectedSlot = FindSelectedSlot(slots);
            bool hasPreview = _previewRenderer.Render(selectedSlot);
            _surface.SetPreviewTexture(_previewRenderer.PreviewTexture, hasPreview);
        }

        private static GarageSlotViewModel FindSelectedSlot(IReadOnlyList<GarageSlotViewModel> slots)
        {
            if (slots == null)
                return null;

            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].IsSelected)
                    return slots[i];
            }

            return slots.Count > 0 ? slots[0] : null;
        }
    }
}
