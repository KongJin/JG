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

        [SerializeField]
        private GarageSetBUitkPreviewRenderer _partPreviewRenderer;

        private GarageSetBUitkSurface _surface;
        private VisualElement _surfaceRoot;
        private VisualElement _hostScreenRoot;
        private bool _isHostBound;
        private IReadOnlyList<GarageSlotViewModel> _lastSlots;
        private GarageNovaPartsPanelViewModel _lastPartList;
        private GarageEditorViewModel _lastEditor;
        private GarageResultViewModel _lastResult;
        private GarageEditorFocus _lastFocusedPart;
        private bool _lastIsSaving;
        private bool _hasLastRender;

        public event Action<int> SlotSelected;
        public event Action<GarageEditorFocus> PartFocusSelected;
        public event Action<string> PartSearchChanged;
        public event Action<GarageNovaPartSelection> PartOptionSelected;
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

            _isHostBound = false;
            _hostScreenRoot = null;
            return Bind(root);
        }

        public bool BindToHost(VisualElement host)
        {
            if (host == null)
                return false;

            if (host.Q<VisualElement>("GarageSetBScreen") == null)
            {
                var source = _document != null ? _document.visualTreeAsset : null;
                if (source == null)
                    return false;

                host.Clear();
                source.CloneTree(host);
            }

            var screenRoot = host.Q<VisualElement>("GarageSetBScreen");
            if (screenRoot == null)
                return false;

            _isHostBound = true;
            _hostScreenRoot = screenRoot;
            _hostScreenRoot.style.display = DisplayStyle.Flex;
            HideStandaloneDocumentRoot();
            return Bind(host);
        }

        public bool SetDocumentRootVisible(bool isVisible)
        {
            if (_isHostBound)
            {
                HideStandaloneDocumentRoot();
                if (_hostScreenRoot != null)
                    _hostScreenRoot.style.display = DisplayStyle.Flex;
                return true;
            }

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

        private void HideStandaloneDocumentRoot()
        {
            if (_document == null)
                return;

            if (!_document.gameObject.activeSelf)
                _document.gameObject.SetActive(true);

            _document.sortingOrder = 10;
            var root = _document.rootVisualElement;
            if (root != null)
                root.style.display = DisplayStyle.None;
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
            _surface.PartSearchChanged += value => PartSearchChanged?.Invoke(value);
            _surface.PartOptionSelected += selection => PartOptionSelected?.Invoke(selection);
            _surface.SaveRequested += () => SaveRequested?.Invoke();
            _surface.SettingsRequested += () => SettingsRequested?.Invoke();

            if (_hasLastRender)
                RenderToSurface();

            return true;
        }

        public void Render(
            IReadOnlyList<GarageSlotViewModel> slots,
            GarageNovaPartsPanelViewModel partList,
            GarageEditorViewModel editor,
            GarageResultViewModel result,
            GarageEditorFocus focusedPart,
            bool isSaving)
        {
            _lastSlots = slots;
            _lastPartList = partList;
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
            var selectedSlot=FindSelectedSlot(slots);
            if(_previewRenderer==null)
                _surface.SetPreviewTexture(null,false);
            else
            {
                bool hasPreview=_previewRenderer.Render(selectedSlot);
                _surface.SetPreviewTexture(_previewRenderer.PreviewTexture,hasPreview);
            }
            RenderPartPreview(selectedSlot);
        }

        private void RenderPartPreview(GarageSlotViewModel selectedSlot)
        {
            if(_partPreviewRenderer==null) {_surface.SetPartPreviewTexture(null,false); return; }
            bool hasPreview=selectedSlot!=null&&_partPreviewRenderer.Render(selectedSlot)||_partPreviewRenderer.RenderPart(_lastPartList);
            _surface.SetPartPreviewTexture(_partPreviewRenderer.PreviewTexture,hasPreview);
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
                _lastPartList,
                _lastEditor,
                _lastResult,
                _lastFocusedPart,
                _lastIsSaving);
            RenderPreview(_lastSlots);
        }
    }
}
