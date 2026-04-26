using UnityEngine;
using UnityEngine.UI;

namespace Features.Garage.Presentation
{
    internal sealed class GaragePageScrollController
    {
        public void ScrollBodyToTop(Transform mobileBodyHost)
        {
            if (mobileBodyHost == null)
            {
                return;
            }

            ScrollRect scrollRect = mobileBodyHost.GetComponentInParent<ScrollRect>();
            if (scrollRect == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            if (mobileBodyHost.TryGetComponent<RectTransform>(out var contentRoot))
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);
            }

            scrollRect.StopMovement();
            scrollRect.normalizedPosition = new Vector2(scrollRect.normalizedPosition.x, 1f);
            scrollRect.verticalNormalizedPosition = 1f;
        }
    }
}
