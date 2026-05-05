using System;
using System.Collections.Generic;
using Shared.Ui;
using UnityEngine;
using UnityEngine.UIElements;

namespace Features.Garage.Presentation
{
    internal sealed class GarageSetBSlotSurface : BaseSurface<VisualElement>
    {
        private const float SlotDragThreshold = 16f;

        private readonly SlotBinding[] _slots = new SlotBinding[GarageUitkConstants.Slots.MaxCount];
        private readonly Action[] _slotClicked = new Action[GarageUitkConstants.Slots.MaxCount];
        private readonly Action[] _slotClearClicked = new Action[GarageUitkConstants.Slots.MaxCount];
        private readonly EventCallback<PointerDownEvent>[] _slotPointerDown = new EventCallback<PointerDownEvent>[GarageUitkConstants.Slots.MaxCount];
        private readonly EventCallback<PointerMoveEvent>[] _slotPointerMove = new EventCallback<PointerMoveEvent>[GarageUitkConstants.Slots.MaxCount];
        private readonly EventCallback<PointerUpEvent>[] _slotPointerUp = new EventCallback<PointerUpEvent>[GarageUitkConstants.Slots.MaxCount];
        private readonly EventCallback<PointerCancelEvent>[] _slotPointerCancel = new EventCallback<PointerCancelEvent>[GarageUitkConstants.Slots.MaxCount];
        private int _activeDragSlot = -1;
        private int _activeDragPointerId = -1;
        private int _suppressedClickSlot = -1;
        private Vector2 _dragStartPosition;
        private bool _isSlotDragActive;

        public GarageSetBSlotSurface(VisualElement root)
            : base(root)
        {
            for (int i = 0; i < GarageUitkConstants.Slots.MaxCount; i++)
            {
                int slotNumber = i + 1;
                _slots[i] = new SlotBinding(
                    UitkElementUtility.Required<Button>(root, GarageUitkConstants.Slots.BuildCardName(slotNumber)),
                    UitkElementUtility.Required<Label>(root, GarageUitkConstants.Slots.BuildCodeLabelName(slotNumber)),
                    UitkElementUtility.Required<VisualElement>(root, GarageUitkConstants.Slots.BuildIconName(slotNumber)),
                    UitkElementUtility.Required<VisualElement>(root, GarageUitkConstants.Slots.BuildIconGlyphName(slotNumber)),
                    UitkElementUtility.Required<Button>(root, GarageUitkConstants.Slots.BuildClearButtonName(slotNumber)),
                    UitkElementUtility.Required<Label>(root, GarageUitkConstants.Slots.BuildNameLabelName(slotNumber)));
                _slots[i].IconHost.Insert(0, _slots[i].PreviewImage);
            }

            BindCallbacks();
        }

        public event Action<int> SlotSelected;
        public event Action<int> SlotClearRequested;
        public event Action<int, int> SlotMoveRequested;

        public void Render(IReadOnlyList<GarageSlotViewModel> slots)
        {
            Render(slots, null);
        }

        public void Render(IReadOnlyList<GarageSlotViewModel> slots, IReadOnlyList<Texture> slotPreviewTextures)
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                var slot = slots != null && i < slots.Count ? slots[i] : null;
                var previewTexture = slotPreviewTextures != null && i < slotPreviewTextures.Count
                    ? slotPreviewTextures[i]
                    : null;
                RenderSlot(_slots[i], slot, previewTexture, i);
            }
        }

        private void BindCallbacks()
        {
            if (IsDisposed)
                return;

            for (int i = 0; i < _slots.Length; i++)
            {
                int slotIndex = i;
// csharp-guardrails: allow-null-defense
                _slotClicked[i] ??= () => SelectSlotFromClick(slotIndex);
                _slotClearClicked[i] ??= () => SlotClearRequested?.Invoke(slotIndex);
// csharp-guardrails: allow-null-defense
                _slotPointerDown[i] ??= evt => BeginSlotDrag(slotIndex, evt);
// csharp-guardrails: allow-null-defense
                _slotPointerMove[i] ??= evt => UpdateSlotDrag(slotIndex, evt);
// csharp-guardrails: allow-null-defense
                _slotPointerUp[i] ??= evt => EndSlotDrag(slotIndex, evt);
// csharp-guardrails: allow-null-defense
                _slotPointerCancel[i] ??= evt => CancelSlotDrag(slotIndex, evt);
                _slots[i].Card.clicked += _slotClicked[i];
                _slots[i].ClearButton.clicked += _slotClearClicked[i];
                _slots[i].Card.RegisterCallback(_slotPointerDown[i]);
                _slots[i].Card.RegisterCallback(_slotPointerMove[i]);
                _slots[i].Card.RegisterCallback(_slotPointerUp[i]);
                _slots[i].Card.RegisterCallback(_slotPointerCancel[i]);
            }
        }

        protected override void DisposeSurface()
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slotClicked[i] != null)
                    _slots[i].Card.clicked -= _slotClicked[i];

                if (_slotClearClicked[i] != null)
                    _slots[i].ClearButton.clicked -= _slotClearClicked[i];

                if (_slotPointerDown[i] != null)
                    _slots[i].Card.UnregisterCallback(_slotPointerDown[i]);

                if (_slotPointerMove[i] != null)
                    _slots[i].Card.UnregisterCallback(_slotPointerMove[i]);

                if (_slotPointerUp[i] != null)
                    _slots[i].Card.UnregisterCallback(_slotPointerUp[i]);

                if (_slotPointerCancel[i] != null)
                    _slots[i].Card.UnregisterCallback(_slotPointerCancel[i]);
            }
        }

        private void SelectSlotFromClick(int slotIndex)
        {
            if (_suppressedClickSlot == slotIndex)
            {
                _suppressedClickSlot = -1;
                return;
            }

            SlotSelected?.Invoke(slotIndex);
        }

        private void BeginSlotDrag(int slotIndex, PointerDownEvent evt)
        {
            if (_activeDragPointerId >= 0 ||
                evt == null ||
                IsInsideElement(evt.target as VisualElement, _slots[slotIndex].ClearButton))
                return;

            _activeDragSlot = slotIndex;
            _activeDragPointerId = evt.pointerId;
            _dragStartPosition = ToVector2(evt.position);
            _isSlotDragActive = false;
            _slots[slotIndex].Card.CapturePointer(evt.pointerId);
        }

        private void UpdateSlotDrag(int slotIndex, PointerMoveEvent evt)
        {
            if (evt == null || !IsActivePointer(slotIndex, evt.pointerId))
                return;

            var position = ToVector2(evt.position);
            if (!_isSlotDragActive &&
                (position - _dragStartPosition).sqrMagnitude >= SlotDragThreshold * SlotDragThreshold)
                _isSlotDragActive = true;

            if (!_isSlotDragActive)
                return;

            SetSlotDragClasses(FindSlotAtPanelPosition(position));
            evt.StopPropagation();
        }

        private void EndSlotDrag(int slotIndex, PointerUpEvent evt)
        {
            if (evt == null || !IsActivePointer(slotIndex, evt.pointerId))
                return;

            int sourceSlot = _activeDragSlot;
            int targetSlot = _isSlotDragActive ? FindSlotAtPanelPosition(ToVector2(evt.position)) : -1;
            ReleasePointer(slotIndex, evt.pointerId);
            bool wasDragging = _isSlotDragActive;
            ClearSlotDragClasses();
            ResetSlotDrag();

            if (!wasDragging)
                return;

            SuppressNextSlotClick(sourceSlot);
            evt.StopImmediatePropagation();

            if (targetSlot >= 0 && targetSlot != sourceSlot)
                SlotMoveRequested?.Invoke(sourceSlot, targetSlot);
        }

        private void CancelSlotDrag(int slotIndex, PointerCancelEvent evt)
        {
            if (evt == null || !IsActivePointer(slotIndex, evt.pointerId))
                return;

            ReleasePointer(slotIndex, evt.pointerId);
            if (_isSlotDragActive)
                SuppressNextSlotClick(_activeDragSlot);
            ClearSlotDragClasses();
            ResetSlotDrag();
        }

        private bool IsActivePointer(int slotIndex, int pointerId)
        {
            return _activeDragSlot == slotIndex && _activeDragPointerId == pointerId;
        }

        private void ReleasePointer(int slotIndex, int pointerId)
        {
            if (_slots[slotIndex].Card.HasPointerCapture(pointerId))
                _slots[slotIndex].Card.ReleasePointer(pointerId);
        }

        private void ResetSlotDrag()
        {
            _activeDragSlot = -1;
            _activeDragPointerId = -1;
            _dragStartPosition = Vector2.zero;
            _isSlotDragActive = false;
        }

        private void SetSlotDragClasses(int dropTargetSlot)
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                UitkElementUtility.SetClass(
                    _slots[i].Card,
                    GarageUitkConstants.Classes.Slot.CardDragging,
                    i == _activeDragSlot);
                UitkElementUtility.SetClass(
                    _slots[i].Card,
                    GarageUitkConstants.Classes.Slot.CardDropTarget,
                    i == dropTargetSlot && i != _activeDragSlot);
            }
        }

        private void ClearSlotDragClasses()
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                UitkElementUtility.SetClass(_slots[i].Card, GarageUitkConstants.Classes.Slot.CardDragging, false);
                UitkElementUtility.SetClass(_slots[i].Card, GarageUitkConstants.Classes.Slot.CardDropTarget, false);
            }
        }

        private int FindSlotAtPanelPosition(Vector2 panelPosition)
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i].Card.worldBound.Contains(panelPosition))
                    return i;
            }

            return -1;
        }

        private void SuppressNextSlotClick(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _slots.Length)
                return;

            _suppressedClickSlot = slotIndex;
            _slots[slotIndex].Card.schedule.Execute(() =>
            {
                if (_suppressedClickSlot == slotIndex)
                    _suppressedClickSlot = -1;
            }).ExecuteLater(0);
        }

        private static bool IsInsideElement(VisualElement target, VisualElement ancestor)
        {
            while (target != null)
            {
                if (ReferenceEquals(target, ancestor))
                    return true;

                target = target.parent;
            }

            return false;
        }

        private static Vector2 ToVector2(Vector3 position)
        {
            return new Vector2(position.x, position.y);
        }

        private static void RenderSlot(SlotBinding binding, GarageSlotViewModel slot, Texture previewTexture, int slotIndex)
        {
            bool isEmpty = slot == null || slot.IsEmpty;
            bool isSelected = slot != null && slot.IsSelected;
            bool hasPreview = !isEmpty && previewTexture != null;

// csharp-guardrails: allow-null-defense
            binding.CodeLabel.text = slot?.SlotLabel ?? $"UNIT_{slotIndex + 1:00}";
            binding.CodeLabel.style.display = DisplayStyle.None;
            UitkIconRegistry.Apply(binding.IconGlyph, BuildSlotIconId(slot));
            binding.NameLabel.text = BuildSlotName(slot, slotIndex);
            binding.NameLabel.style.display = DisplayStyle.Flex;
            binding.PreviewImage.image = hasPreview ? previewTexture : null;
            binding.PreviewImage.style.display = hasPreview ? DisplayStyle.Flex : DisplayStyle.None;
            binding.IconGlyph.style.display = hasPreview ? DisplayStyle.None : DisplayStyle.Flex;
            binding.ClearButton.style.display = isSelected && !isEmpty ? DisplayStyle.Flex : DisplayStyle.None;
            binding.ClearButton.SetEnabled(isSelected && !isEmpty);

            UitkElementUtility.SetClass(binding.Card, GarageUitkConstants.Classes.Slot.CardActive, isSelected);
            UitkElementUtility.SetClass(binding.Card, GarageUitkConstants.Classes.Slot.CardEmpty, isEmpty);
            UitkElementUtility.SetClass(
                binding.ClearButton,
                GarageUitkConstants.Classes.Slot.ClearButtonVisible,
                isSelected && !isEmpty);
            UitkElementUtility.SetClass(binding.CodeLabel, GarageUitkConstants.Classes.Slot.CodeActive, isSelected);
            UitkElementUtility.SetClass(binding.CodeLabel, GarageUitkConstants.Classes.Slot.CodeEmpty, isEmpty);
            UitkElementUtility.SetClass(binding.IconHost, GarageUitkConstants.Classes.Slot.IconActive, isSelected);
            UitkElementUtility.SetClass(binding.IconHost, GarageUitkConstants.Classes.Slot.IconEmpty, isEmpty);
            UitkElementUtility.SetClass(binding.IconGlyph, GarageUitkConstants.Classes.Slot.IconGlyphActive, isSelected);
            UitkElementUtility.SetClass(binding.IconGlyph, GarageUitkConstants.Classes.Slot.IconGlyphEmpty, isEmpty);
            UitkElementUtility.SetClass(binding.NameLabel, GarageUitkConstants.Classes.Slot.NameActive, isSelected);
            UitkElementUtility.SetClass(binding.NameLabel, GarageUitkConstants.Classes.Slot.NameEmpty, isEmpty);
        }

        private static string BuildSlotIconId(GarageSlotViewModel slot)
        {
            if (slot == null || slot.IsEmpty)
                return GarageUitkConstants.Icons.Add;

            // csharp-guardrails: allow-null-defense
            var role = (slot.RoleLabel ?? string.Empty).ToLowerInvariant();
// csharp-guardrails: allow-null-defense
            var status = (slot.StatusBadgeText ?? string.Empty).ToLowerInvariant();
// csharp-guardrails: allow-null-defense
            var display = (slot.Title ?? string.Empty).ToLowerInvariant();
            var combined = string.Concat(role, " ", status, " ", display);

            if (combined.Contains("방어") || combined.Contains("defense") || combined.Contains("guard") || combined.Contains("고정"))
                return GarageUitkConstants.Icons.Security;

            if (combined.Contains("지원") || combined.Contains("support") || combined.Contains("정비"))
                return GarageUitkConstants.Icons.PrecisionManufacturing;

            return GarageUitkConstants.Icons.SmartToy;
        }

        private static string BuildSlotName(GarageSlotViewModel slot, int slotIndex)
        {
            if (slot == null || slot.IsEmpty || string.IsNullOrWhiteSpace(slot.FirepowerId))
// csharp-guardrails: allow-null-defense
                return slot?.SlotLabel ?? $"A-{slotIndex + 1:00}";

// csharp-guardrails: allow-null-defense
            string summary = slot.Summary ?? string.Empty;
            int roleSeparatorIndex = summary.LastIndexOf('|');
            if (roleSeparatorIndex >= 0 && roleSeparatorIndex + 1 < summary.Length)
                summary = summary.Substring(roleSeparatorIndex + 1);

            int separatorIndex = summary.IndexOf('/');
            string weaponName = separatorIndex >= 0
                ? summary.Substring(0, separatorIndex)
                : summary;

            return weaponName.Trim();
        }

        private readonly struct SlotBinding
        {
            public SlotBinding(
                Button card,
                Label codeLabel,
                VisualElement iconHost,
                VisualElement iconGlyph,
                Button clearButton,
                Label nameLabel)
            {
                Card = card;
                CodeLabel = codeLabel;
                IconHost = iconHost;
                IconGlyph = iconGlyph;
                ClearButton = clearButton;
                PreviewImage = UitkElementUtility.CreateAbsoluteImage($"Slot{card.name.Substring("SlotCard".Length)}PreviewImage");
                PreviewImage.scaleMode = ScaleMode.ScaleAndCrop;
                NameLabel = nameLabel;
            }

            public Button Card { get; }
            public Label CodeLabel { get; }
            public VisualElement IconHost { get; }
            public VisualElement IconGlyph { get; }
            public Button ClearButton { get; }
            public Image PreviewImage { get; }
            public Label NameLabel { get; }
        }
    }
}
