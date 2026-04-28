using UnityEngine;

namespace Features.Garage.Presentation
{
    internal sealed class GaragePageScrollController
    {
        public Transform LastRequestedBodyHost { get; private set; }

        public void ScrollBodyToTop(Transform mobileBodyHost)
        {
            LastRequestedBodyHost = mobileBodyHost;
        }
    }
}
