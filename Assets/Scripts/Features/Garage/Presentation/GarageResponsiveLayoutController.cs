using UnityEngine;

namespace Features.Garage.Presentation
{
    internal sealed class GarageResponsiveLayoutController
    {
        public GarageResponsiveLayoutState Evaluate(
            float width,
            float mobileBreakpointWidth,
            float lastResponsiveWidth,
            bool isMobileLayout)
        {
            bool nextIsMobile = width <= mobileBreakpointWidth;
            bool widthChanged = Mathf.Abs(width - lastResponsiveWidth) > 0.5f;
            bool modeChanged = nextIsMobile != isMobileLayout;
            return new GarageResponsiveLayoutState(widthChanged || modeChanged, nextIsMobile);
        }
    }

    internal readonly struct GarageResponsiveLayoutState
    {
        public bool ShouldRefresh { get; }
        public bool IsMobileLayout { get; }

        public GarageResponsiveLayoutState(bool shouldRefresh, bool isMobileLayout)
        {
            ShouldRefresh = shouldRefresh;
            IsMobileLayout = isMobileLayout;
        }
    }
}
