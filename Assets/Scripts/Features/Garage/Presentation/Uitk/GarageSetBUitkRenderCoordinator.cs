using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Features.Garage.Presentation
{
    internal sealed class GarageSetBUitkRenderCoordinator : System.IDisposable
    {
        private readonly GarageSetBUitkElementBindings _bindings;
        private readonly GarageSetBSlotSurface _slotSurface;
        private readonly GarageSetBPartListSurface _partListSurface;
        private readonly GarageSetBUitkPreviewController _previewController;
        private readonly GarageStatRadarElement _statRadar;
        private bool _isDisposed;

        public GarageSetBSlotSurface SlotSurface => _slotSurface;
        public GarageSetBPartListSurface PartListSurface => _partListSurface;

        public GarageSetBUitkRenderCoordinator(
            GarageSetBUitkElementBindings bindings,
            GarageSetBSlotSurface slotSurface,
            GarageSetBPartListSurface partListSurface,
            GarageSetBUitkPreviewController previewController,
            GarageStatRadarElement statRadar)
        {
            _bindings = bindings;
            _slotSurface = slotSurface;
            _partListSurface = partListSurface;
            _previewController = previewController;
            _statRadar = statRadar;
        }

        public void Render(
            IReadOnlyList<GarageSlotViewModel> slots,
            GarageNovaPartsPanelViewModel partList,
            GarageEditorViewModel editor,
            GarageResultViewModel result,
            GarageEditorFocus focusedPart,
            bool isSaving)
        {
            // csharp-guardrails: allow-null-defense
            if (!_bindings.IsBound || _partListSurface == null || _slotSurface == null)
                return;

            _partListSurface.Render(partList, focusedPart);
            RenderResult(result, isSaving);
// csharp-guardrails: allow-null-defense
            _previewController?.Render(slots, partList);
// csharp-guardrails: allow-null-defense
            _slotSurface.Render(slots, _previewController?.RenderSlotPreviews(slots));
        }

        private void RenderResult(GarageResultViewModel result, bool isSaving)
        {
            // csharp-guardrails: allow-null-defense
            if (_bindings.CommandStatusLabel != null)
// csharp-guardrails: allow-null-defense
                _bindings.CommandStatusLabel.text = result?.RosterStatusText ?? "편성 상태 대기";

// csharp-guardrails: allow-null-defense
            string validationText = result?.ValidationText ?? string.Empty;
            // csharp-guardrails: allow-null-defense
            if (_bindings.SaveValidationLabel != null)
                _bindings.SaveValidationLabel.text = validationText;

            _bindings.SaveValidationLabel.style.display = string.IsNullOrWhiteSpace(validationText)
                ? DisplayStyle.None
                : DisplayStyle.Flex;

            // csharp-guardrails: allow-null-defense
            if (_bindings.SaveButton != null)
            {
                _bindings.SaveButton.text = isSaving
                    ? "저장 중..."
// csharp-guardrails: allow-null-defense
                    : result?.PrimaryActionLabel ?? "저장 및 배치";
                _bindings.SaveButton.SetEnabled(!isSaving && result?.CanSave == true);
            }

            bool showSaveDock =
                result != null && (isSaving || result.CanSave || result.IsDirty || !result.IsReady);
// csharp-guardrails: allow-null-defense
            if (_bindings.SaveDock != null)
                _bindings.SaveDock.style.display = showSaveDock ? DisplayStyle.Flex : DisplayStyle.None;

// csharp-guardrails: allow-null-defense
            _statRadar?.Render(result?.Radar);

            bool showValidation =
                result != null
                && (result.IsDirty || result.CanSave)
                && !string.IsNullOrWhiteSpace(validationText);
            _bindings.SaveValidationLabel.style.display = showValidation
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            RenderPowerBar(result);
        }

        private void RenderPowerBar(GarageResultViewModel result)
        {
// csharp-guardrails: allow-null-defense
            bool hasPower = result?.Radar != null;
// csharp-guardrails: allow-null-defense
            if (_bindings.PreviewPowerBar != null)
                _bindings.PreviewPowerBar.style.display = hasPower ? DisplayStyle.Flex : DisplayStyle.None;

            // csharp-guardrails: allow-null-defense
            if (_bindings.PreviewPowerLabel != null)
                _bindings.PreviewPowerLabel.text = hasPower ? $"EN {result.Radar.SummonCost}" : string.Empty;

// csharp-guardrails: allow-null-defense
            if (_bindings.PreviewPowerFill != null)
                _bindings.PreviewPowerFill.style.width = Length.Percent(
                    hasPower ? Mathf.Clamp(result.Radar.SummonCost, 0, 100) : 0
                );
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            // csharp-guardrails: allow-null-defense
            _previewController?.Dispose();
            // csharp-guardrails: allow-null-defense
            _partListSurface?.Dispose();
            // csharp-guardrails: allow-null-defense
            _slotSurface?.Dispose();
            // csharp-guardrails: allow-null-defense
            _statRadar?.RemoveFromHierarchy();
        }
    }
}
