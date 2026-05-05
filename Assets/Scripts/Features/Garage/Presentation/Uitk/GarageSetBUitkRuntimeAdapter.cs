using System;
using System.Collections.Generic;
using Shared.Runtime;
using Shared.Attributes;
using UnityEngine;
using UnityEngine.UIElements;

namespace Features.Garage.Presentation
{
    /// <summary>
    /// Garage Set B UI Runtime Adapter.
    /// Unity 생명주기 연결만 담당하며, 실제 UI 로직은 Core에 위임.
    /// 테스트 가능성을 위해 인터페이스를 구현.
    /// </summary>
    public sealed class GarageSetBUitkRuntimeAdapter : MonoBehaviour
    {
        [Required, SerializeField] private UIDocument _document;
        [Required, SerializeField] private GarageSetBUitkPreviewRenderer _previewRenderer;
        [Required, SerializeField] private GarageSetBUitkPreviewRenderer _partPreviewRenderer;

        private IGarageSetBUitkAdapter _core;
        private VisualElement _boundRoot;
        private GarageSetBUitkDocumentHost _documentHost;
        private Action<int> _coreSlotSelected;
        private Action<int> _coreSlotClearRequested;
        private Action<int, int> _coreSlotMoveRequested;
        private Action<GarageEditorFocus> _corePartFocusSelected;
        private Action<string> _corePartSearchChanged;
        private Action<GarageNovaPartSelection> _corePartOptionSelected;
        private Action _coreSaveRequested;
        private Action _coreSettingsRequested;

        public event Action<int> SlotSelected;
        public event Action<int> SlotClearRequested;
        public event Action<int, int> SlotMoveRequested;
        public event Action<GarageEditorFocus> PartFocusSelected;
        public event Action<string> PartSearchChanged;
        public event Action<GarageNovaPartSelection> PartOptionSelected;
        public event Action SaveRequested;
        public event Action SettingsRequested;

        public bool Bind()
        {
// csharp-guardrails: allow-null-defense
            if (_core != null)
                return true;

// csharp-guardrails: allow-null-defense
            if (_document == null)
                return false;

            var root = _document.rootVisualElement;
            // csharp-guardrails: allow-null-defense
            return root != null && Bind(root);
        }

        public bool BindToHost(VisualElement host)
        {
            return DocumentHost.BindToHost(host);
        }

        public bool SetDocumentRootVisible(bool isVisible)
        {
            return DocumentHost.SetDocumentRootVisible(isVisible);
        }

        public void Render(
            IReadOnlyList<GarageSlotViewModel> slots,
            GarageNovaPartsPanelViewModel partList,
            GarageEditorViewModel editor,
            GarageResultViewModel result,
            GarageEditorFocus focusedPart,
            bool isSaving)
        {
            if (!Bind())
                return;

// csharp-guardrails: allow-null-defense
            _core?.Render(slots, partList, editor, result, focusedPart, isSaving);
        }

        private void OnEnable()
        {
            Bind();
        }

        private void Reset()
        {
// csharp-guardrails: allow-null-defense
            if (_document == null)
                _document = ComponentAccess.Get<UIDocument>(gameObject);
        }

        private void OnDestroy()
        {
            DisposeCore();
        }

        internal bool BindRoot(VisualElement root)
        {
            if (root == null)
                return false;

// csharp-guardrails: allow-null-defense
            if (_core != null)
            {
                if (ReferenceEquals(_boundRoot, root))
                    return true;

                DisposeCore();
            }

            var bindings = new GarageSetBUitkElementBindings();
            if (!bindings.TryBind(root))
                return false;

            _core = new GarageSetBUitkAdapterCore(
                bindings,
                transform,
                _previewRenderer,
                _partPreviewRenderer);
            _boundRoot = root;
            ForwardCoreEvents();
            return true;
        }

        private GarageSetBUitkDocumentHost DocumentHost
        {
            get
            {
                // csharp-guardrails: allow-null-defense
                _documentHost ??= new GarageSetBUitkDocumentHost(_document, this);
                return _documentHost;
            }
        }

        private bool Bind(VisualElement root)
        {
            return BindRoot(root);
        }

        private void ForwardCoreEvents()
        {
// csharp-guardrails: allow-null-defense
            if (_core == null)
                return;

            _coreSlotSelected = slotIndex => SlotSelected?.Invoke(slotIndex);
            _coreSlotClearRequested = slotIndex => SlotClearRequested?.Invoke(slotIndex);
            _coreSlotMoveRequested = (sourceSlotIndex, targetSlotIndex) => SlotMoveRequested?.Invoke(sourceSlotIndex, targetSlotIndex);
            _corePartFocusSelected = focus => PartFocusSelected?.Invoke(focus);
            _corePartSearchChanged = value => PartSearchChanged?.Invoke(value);
            _corePartOptionSelected = selection => PartOptionSelected?.Invoke(selection);
            _coreSaveRequested = () => SaveRequested?.Invoke();
            _coreSettingsRequested = () => SettingsRequested?.Invoke();

            _core.SlotSelected += _coreSlotSelected;
            _core.SlotClearRequested += _coreSlotClearRequested;
            _core.SlotMoveRequested += _coreSlotMoveRequested;
            _core.PartFocusSelected += _corePartFocusSelected;
            _core.PartSearchChanged += _corePartSearchChanged;
            _core.PartOptionSelected += _corePartOptionSelected;
            _core.SaveRequested += _coreSaveRequested;
            _core.SettingsRequested += _coreSettingsRequested;
        }

        private void DisposeCore()
        {
// csharp-guardrails: allow-null-defense
            if (_core == null)
            {
                _boundRoot = null;
                return;
            }

            UnforwardCoreEvents();
            _core.Dispose();
            _core = null;
            _boundRoot = null;
        }

        private void UnforwardCoreEvents()
        {
// csharp-guardrails: allow-null-defense
            if (_core == null)
                return;

// csharp-guardrails: allow-null-defense
            if (_coreSlotSelected != null)
                _core.SlotSelected -= _coreSlotSelected;
// csharp-guardrails: allow-null-defense
            if (_coreSlotClearRequested != null)
                _core.SlotClearRequested -= _coreSlotClearRequested;
// csharp-guardrails: allow-null-defense
            if (_coreSlotMoveRequested != null)
                _core.SlotMoveRequested -= _coreSlotMoveRequested;
// csharp-guardrails: allow-null-defense
            if (_corePartFocusSelected != null)
                _core.PartFocusSelected -= _corePartFocusSelected;
// csharp-guardrails: allow-null-defense
            if (_corePartSearchChanged != null)
                _core.PartSearchChanged -= _corePartSearchChanged;
// csharp-guardrails: allow-null-defense
            if (_corePartOptionSelected != null)
                _core.PartOptionSelected -= _corePartOptionSelected;
// csharp-guardrails: allow-null-defense
            if (_coreSaveRequested != null)
                _core.SaveRequested -= _coreSaveRequested;
// csharp-guardrails: allow-null-defense
            if (_coreSettingsRequested != null)
                _core.SettingsRequested -= _coreSettingsRequested;

            _coreSlotSelected = null;
            _coreSlotClearRequested = null;
            _coreSlotMoveRequested = null;
            _corePartFocusSelected = null;
            _corePartSearchChanged = null;
            _corePartOptionSelected = null;
            _coreSaveRequested = null;
            _coreSettingsRequested = null;
        }
    }
}
