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
        private VisualElement _surfaceRoot;
        private IReadOnlyList<GarageSlotViewModel> _lastSlots;
        private GarageEditorViewModel _lastEditor;
        private GarageResultViewModel _lastResult;
        private GarageEditorFocus _lastFocusedPart;
        private bool _lastIsSaving;
        private bool _hasLastRender;

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

            return Bind(root);
        }

        public bool BindToHost(VisualElement host)
        {
            if (host == null)
                return false;

            if (host.Q<VisualElement>("GarageSetBScreen") == null)
            {
                host.Clear();
                var source = _document != null ? _document.visualTreeAsset : null;
                if (source == null)
                    return false;

                source.CloneTree(host);
            }

            return Bind(host);
        }

        public bool SetDocumentRootVisible(bool isVisible)
        {
            if (_document == null)
                return false;

            if (!_document.gameObject.activeSelf)
                _document.gameObject.SetActive(true);

            _document.sortingOrder = 10;
            var root = _document.rootVisualElement;
            if (root != null)
                root.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;

            return true;
        }

        private bool Bind(VisualElement root)
        {
            if (root == null)
                return false;

            if (_surface != null && _surfaceRoot == root)
                return true;

            _surfaceRoot = root;
            _surface = new GarageSetBUitkSurface(root);
            _surface.SlotSelected += slotIndex => SlotSelected?.Invoke(slotIndex);
            _surface.PartFocusSelected += focus => PartFocusSelected?.Invoke(focus);
            _surface.SaveRequested += () => SaveRequested?.Invoke();
            _surface.SettingsRequested += () => SettingsRequested?.Invoke();

            if (_hasLastRender)
                RenderToSurface();

            return true;
        }

        public void Render(
            IReadOnlyList<GarageSlotViewModel> slots,
            GarageEditorViewModel editor,
            GarageResultViewModel result,
            GarageEditorFocus focusedPart,
            bool isSaving)
        {
            _lastSlots = slots;
            _lastEditor = editor;
            _lastResult = result;
            _lastFocusedPart = focusedPart;
            _lastIsSaving = isSaving;
            _hasLastRender = true;

            if (!Bind())
                return;

            RenderToSurface();
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

        private void RenderToSurface()
        {
            if (_surface == null)
                return;

            _surface.Render(
                _lastSlots,
                _lastEditor,
                _lastResult,
                _lastFocusedPart,
                _lastIsSaving);
            RenderPreview(_lastSlots);
        }
    }
}
