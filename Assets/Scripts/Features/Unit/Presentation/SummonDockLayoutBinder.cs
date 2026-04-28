using Shared.Runtime;
using Shared.Runtime.Pooling;
using UnityEngine;
using UnityEngine.UI;

namespace Features.Unit.Presentation
{
    internal sealed class SummonDockLayoutBinder
    {
        private readonly Transform _rootTransform;
        private RectTransform _slotRowRect;

        public SummonDockLayoutBinder(Transform rootTransform)
        {
            _rootTransform = rootTransform;
        }

        public void Apply(
            ref RectTransform dockRoot,
            ref RectTransform slotRowRect,
            ref RectTransform energyBarRect,
            ref RectTransform feedbackRect,
            ref HorizontalLayoutGroup slotRowLayout,
            ref Image dockBackgroundImage,
            Color dockBackgroundColor,
            ref Camera worldCamera)
        {
            dockRoot ??= _rootTransform as RectTransform;
            dockBackgroundImage ??= ComponentAccess.Get<Image>(_rootTransform.gameObject);
            slotRowRect ??= FindRect("SlotRow");
            energyBarRect ??= FindRect("EnergyBar");
            feedbackRect ??= FindRect("PlacementErrorView");

            if (slotRowLayout == null && slotRowRect != null)
                slotRowLayout = ComponentAccess.Get<HorizontalLayoutGroup>(slotRowRect.gameObject);

            worldCamera ??= Camera.main;
            _slotRowRect = slotRowRect;

            if (dockBackgroundImage == null)
                return;

            dockBackgroundImage.color = dockBackgroundColor;
            dockBackgroundImage.raycastTarget = false;
        }

        public void RebuildSlotRow()
        {
            if (_slotRowRect != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(_slotRowRect);
        }

        private RectTransform FindRect(string childName)
        {
            var child = _rootTransform.Find(childName);
            return child as RectTransform;
        }
    }
}
