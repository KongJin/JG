using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Features.Garage.Presentation
{
    /// <summary>
    /// Garage Set B UI 어댑터 코어.
    /// Unity MonoBehaviour에 의존하지 않는 POCO 클래스.
    /// 테스트 가능한 UI 로직을 담당.
    /// </summary>
    internal sealed class GarageSetBUitkAdapterCore : IGarageSetBUitkAdapter
    {
        private readonly GarageSetBUitkElementBindings _bindings;
        private readonly GarageSetBUitkRenderCoordinator _renderCoordinator;
        private readonly GarageSetBUitkPreviewController _previewController;
        private readonly GarageSetBSlotSurface _slotSurface;
        private readonly GarageSetBPartListSurface _partListSurface;
        private readonly GarageStatRadarElement _statRadar;
        private bool _isDisposed;

        private readonly Action<int> _slotSelected;
        private readonly Action<int> _slotClearRequested;
        private readonly Action<int, int> _slotMoveRequested;
        private readonly Action<GarageEditorFocus> _partFocusSelected;
        private readonly Action<string> _partSearchChanged;
        private readonly Action<GarageNovaPartSelection> _partOptionSelected;
        private readonly Action _saveRequested;
        private readonly Action _settingsRequested;

        public event Action<int> SlotSelected;
        public event Action<int> SlotClearRequested;
        public event Action<int, int> SlotMoveRequested;
        public event Action<GarageEditorFocus> PartFocusSelected;
        public event Action<string> PartSearchChanged;
        public event Action<GarageNovaPartSelection> PartOptionSelected;
        public event Action SaveRequested;
        public event Action SettingsRequested;

        public GarageSetBUitkAdapterCore(
            GarageSetBUitkElementBindings bindings,
            Transform rendererParent,
            GarageSetBUitkPreviewRenderer previewRenderer,
            GarageSetBUitkPreviewRenderer partPreviewRenderer)
        {
            _bindings = bindings ?? throw new ArgumentNullException(nameof(bindings));

            _slotSurface = new GarageSetBSlotSurface(bindings.SurfaceRoot);
            _partListSurface = new GarageSetBPartListSurface(bindings.SurfaceRoot);

            _previewController = new GarageSetBUitkPreviewController(
                rendererParent,
                previewRenderer,
                partPreviewRenderer,
                _partListSurface,
                bindings.PreviewTitleLabel,
                bindings.PreviewTagRow,
                bindings.UnitPreviewHost,
                bindings.UnitPreviewLabel);

            _statRadar = new GarageStatRadarElement { name = "StatRadarGraph" };
            _statRadar.AddToClassList("stat-radar-graph");
            // csharp-guardrails: allow-null-defense
            bindings.PreviewCard?.Add(_statRadar);

            _renderCoordinator = new GarageSetBUitkRenderCoordinator(
                bindings,
                _slotSurface,
                _partListSurface,
                _previewController,
                _statRadar);

            _slotSelected = slotIndex => SlotSelected?.Invoke(slotIndex);
            _slotClearRequested = slotIndex => SlotClearRequested?.Invoke(slotIndex);
            _slotMoveRequested = (sourceSlotIndex, targetSlotIndex) => SlotMoveRequested?.Invoke(sourceSlotIndex, targetSlotIndex);
            _partFocusSelected = focus => PartFocusSelected?.Invoke(focus);
            _partSearchChanged = value => PartSearchChanged?.Invoke(value);
            _partOptionSelected = selection => PartOptionSelected?.Invoke(selection);
            _saveRequested = () => SaveRequested?.Invoke();
            _settingsRequested = () => SettingsRequested?.Invoke();
            BindCallbacks();
        }

        public bool Bind(VisualElement root)
        {
            if (root == null)
                return false;

            return _bindings.TryBind(root);
        }

        public void Render(
            IReadOnlyList<GarageSlotViewModel> slots,
            GarageNovaPartsPanelViewModel partList,
            GarageEditorViewModel editor,
            GarageResultViewModel result,
            GarageEditorFocus focusedPart,
            bool isSaving)
        {
            if (!_bindings.IsBound)
                return;

            _renderCoordinator.Render(slots, partList, editor, result, focusedPart, isSaving);
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            UnbindCallbacks();
            // csharp-guardrails: allow-null-defense
            _renderCoordinator?.Dispose();
            // csharp-guardrails: allow-null-defense
            _bindings?.Clear();
        }

        private void BindCallbacks()
        {
            // csharp-guardrails: allow-null-defense
            if (_slotSurface != null)
            {
                _slotSurface.SlotSelected += _slotSelected;
                _slotSurface.SlotClearRequested += _slotClearRequested;
                _slotSurface.SlotMoveRequested += _slotMoveRequested;
            }

            // csharp-guardrails: allow-null-defense
            if (_partListSurface != null)
            {
                _partListSurface.FocusSelected += _partFocusSelected;
                _partListSurface.SearchChanged += _partSearchChanged;
                _partListSurface.OptionSelected += _partOptionSelected;
            }

            // csharp-guardrails: allow-null-defense
            if (_bindings.SaveButton != null)
            {
                _bindings.SaveButton.clicked += _saveRequested;
            }

            // csharp-guardrails: allow-null-defense
            if (_bindings.SettingsButton != null)
            {
                _bindings.SettingsButton.clicked += _settingsRequested;
            }
        }

        private void UnbindCallbacks()
        {
            // csharp-guardrails: allow-null-defense
            if (_slotSurface != null)
            {
                _slotSurface.SlotSelected -= _slotSelected;
                _slotSurface.SlotClearRequested -= _slotClearRequested;
                _slotSurface.SlotMoveRequested -= _slotMoveRequested;
            }

            // csharp-guardrails: allow-null-defense
            if (_partListSurface != null)
            {
                _partListSurface.FocusSelected -= _partFocusSelected;
                _partListSurface.SearchChanged -= _partSearchChanged;
                _partListSurface.OptionSelected -= _partOptionSelected;
            }

            // csharp-guardrails: allow-null-defense
            if (_bindings.SaveButton != null)
            {
                _bindings.SaveButton.clicked -= _saveRequested;
            }

            // csharp-guardrails: allow-null-defense
            if (_bindings.SettingsButton != null)
            {
                _bindings.SettingsButton.clicked -= _settingsRequested;
            }
        }
    }
}
