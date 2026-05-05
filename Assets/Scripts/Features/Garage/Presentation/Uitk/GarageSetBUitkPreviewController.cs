using System.Collections.Generic;
using Shared.Ui;
using UnityEngine;
using UnityEngine.UIElements;

namespace Features.Garage.Presentation
{
    internal sealed class GarageSetBUitkPreviewController
    {
        private const float UnitPreviewAssemblyFitScale = 1.12f;
        private const float UnitPreviewAssemblyHorizontalOffset = -0.26f;

        private readonly Transform _rendererParent;
        private readonly GarageSetBUitkPreviewRenderer _previewRenderer;
        private readonly GarageSetBUitkPreviewRenderer _partPreviewRenderer;
        private readonly GarageSetBPartListSurface _partListSurface;
        private readonly Label _previewTitleLabel;
        private readonly VisualElement _previewTagRow;
        private readonly Label _unitPreviewLabel;
        private readonly Image _unitPreviewImage;
        private GarageSetBUitkPreviewRenderer[] _slotPreviewRenderers;
        private Texture[] _slotPreviewTextures;
        private string _lastSelectedSlotLabel = "A-01";

        public GarageSetBUitkPreviewController(
            Transform rendererParent,
            GarageSetBUitkPreviewRenderer previewRenderer,
            GarageSetBUitkPreviewRenderer partPreviewRenderer,
            GarageSetBPartListSurface partListSurface,
            Label previewTitleLabel,
            VisualElement previewTagRow,
            VisualElement unitPreviewHost,
            Label unitPreviewLabel)
        {
            _rendererParent = rendererParent;
            _previewRenderer = previewRenderer;
            _partPreviewRenderer = partPreviewRenderer;
            _partListSurface = partListSurface;
            _previewTitleLabel = previewTitleLabel;
            _previewTagRow = previewTagRow;
            _unitPreviewLabel = unitPreviewLabel;
            _unitPreviewImage = UitkElementUtility.CreateAbsoluteImage();
            unitPreviewHost?.Insert(0, _unitPreviewImage);
            // csharp-guardrails: allow-null-defense
            _previewRenderer?.ConfigureTransparentBackground(true);
            // csharp-guardrails: allow-null-defense
            _previewRenderer?.ConfigureAssemblyFitScale(UnitPreviewAssemblyFitScale);
            // csharp-guardrails: allow-null-defense
            _previewRenderer?.ConfigureAssemblyHorizontalOffset(UnitPreviewAssemblyHorizontalOffset);
            // csharp-guardrails: allow-null-defense
            _partPreviewRenderer?.ConfigureTransparentBackground(true);

            SetPreviewTexture(null, false);
            SetPartPreviewTexture(null, false);
            HidePreviewChrome();
        }

        public void Render(
            IReadOnlyList<GarageSlotViewModel> slots,
            GarageNovaPartsPanelViewModel partList)
        {
            RenderPreview(slots);
            RenderPartPreview(partList);
        }

        public IReadOnlyList<Texture> RenderSlotPreviews(IReadOnlyList<GarageSlotViewModel> slots)
        {
            EnsureSlotPreviewRenderers();

            for (int i = 0; i < GarageUitkConstants.Rendering.SlotPreviewRendererCount; i++)
            {
                var renderer = _slotPreviewRenderers[i];
                var slot = slots != null && i < slots.Count ? slots[i] : null;
                // csharp-guardrails: allow-null-defense
                bool hasPreview = renderer != null && renderer.Render(slot);
                _slotPreviewTextures[i] = hasPreview ? renderer.PreviewTexture : null;
            }

            return _slotPreviewTextures;
        }

        public void Dispose()
        {
            // csharp-guardrails: allow-null-defense
            if (_slotPreviewRenderers == null)
                return;

            for (int i = 0; i < _slotPreviewRenderers.Length; i++)
            {
                var renderer = _slotPreviewRenderers[i];
                // csharp-guardrails: allow-null-defense
                if (renderer == null)
                    continue;

                DisposeUnityObject(renderer.gameObject);
            }

            _slotPreviewRenderers = null;
            _slotPreviewTextures = null;
        }

        private void RenderPreview(IReadOnlyList<GarageSlotViewModel> slots)
        {
            var selectedSlot = FindSelectedSlot(slots);
// csharp-guardrails: allow-null-defense
            _lastSelectedSlotLabel = selectedSlot?.SlotLabel ?? "A-01";
            // csharp-guardrails: allow-null-defense
            if (_previewRenderer == null)
            {
                SetPreviewTexture(null, false);
                return;
            }

            bool hasPreview = _previewRenderer.Render(selectedSlot);
            SetPreviewTexture(_previewRenderer.PreviewTexture, hasPreview);
        }

        private void RenderPartPreview(GarageNovaPartsPanelViewModel partList)
        {
            // csharp-guardrails: allow-null-defense
            if (_partPreviewRenderer == null)
            {
                SetPartPreviewTexture(null, false);
                return;
            }

            bool hasPreview = _partPreviewRenderer.RenderPart(partList);
            SetPartPreviewTexture(_partPreviewRenderer.PreviewTexture, hasPreview);
        }

        private void SetPreviewTexture(Texture texture, bool isVisible)
        {
            // csharp-guardrails: allow-null-defense
            if (_unitPreviewImage == null || _unitPreviewLabel == null)
                return;

            _unitPreviewImage.image = isVisible ? texture : null;
            _unitPreviewImage.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
            _unitPreviewLabel.style.display = isVisible ? DisplayStyle.None : DisplayStyle.Flex;
            _unitPreviewLabel.text = BuildPreviewPlaceholderText();
            _previewTitleLabel.text = isVisible ? "기체 미리보기" : "설계도 확인";
            HidePreviewChrome();
        }

        private string BuildPreviewPlaceholderText()
        {
            // csharp-guardrails: allow-null-defense
            return _lastSelectedSlotLabel ?? "A-01";
        }

        private void HidePreviewChrome()
        {
            // csharp-guardrails: allow-null-defense
            if (_previewTitleLabel != null)
                _previewTitleLabel.style.display = DisplayStyle.None;
            // csharp-guardrails: allow-null-defense
            if (_previewTagRow != null)
                _previewTagRow.style.display = DisplayStyle.None;
        }

        private void SetPartPreviewTexture(Texture texture, bool isVisible)
        {
            // csharp-guardrails: allow-null-defense
            _partListSurface?.SetPreviewTexture(texture, isVisible);
        }

        private void EnsureSlotPreviewRenderers()
        {
            // csharp-guardrails: allow-null-defense
            if (_slotPreviewRenderers != null && _slotPreviewTextures != null)
                return;

            _slotPreviewRenderers = new GarageSetBUitkPreviewRenderer[GarageUitkConstants.Rendering.SlotPreviewRendererCount];
            _slotPreviewTextures = new Texture[GarageUitkConstants.Rendering.SlotPreviewRendererCount];
            for (int i = 0; i < GarageUitkConstants.Rendering.SlotPreviewRendererCount; i++)
                _slotPreviewRenderers[i] = CreateSlotPreviewRenderer(i);
        }

        private GarageSetBUitkPreviewRenderer CreateSlotPreviewRenderer(int index)
        {
            var rendererObject = new GameObject(
                $"GarageSetBSlotPreviewRenderer{index + 1:00}",
                typeof(Camera),
                typeof(GarageSetBUitkPreviewRenderer));
            rendererObject.transform.SetParent(_rendererParent, false);
            rendererObject.transform.localPosition = new Vector3(
                GarageUitkConstants.Rendering.SlotPreviewRendererSpacing * (index + 1),
                0f,
                0f);

            var camera = rendererObject.GetComponent<Camera>();
            // csharp-guardrails: allow-null-defense
            if (camera != null)
                camera.enabled = false;

            var renderer = rendererObject.GetComponent<GarageSetBUitkPreviewRenderer>();
            renderer.ConfigureAssemblyFitScale(2.4f);
            return renderer;
        }

        private static GarageSlotViewModel FindSelectedSlot(
            IReadOnlyList<GarageSlotViewModel> slots)
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

        private static void DisposeUnityObject(UnityEngine.Object target)
        {
            if (target == null)
                return;

            if (UnityEngine.Application.isPlaying)
                Object.Destroy(target);
            else
                Object.DestroyImmediate(target);
        }

    }
}

