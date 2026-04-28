using UnityEngine;

namespace Features.Unit.Presentation
{
    internal sealed class SummonDockLayoutBinder
    {
        private readonly Transform _rootTransform;

        public SummonDockLayoutBinder(Transform rootTransform)
        {
            _rootTransform = rootTransform;
        }

        public void Apply(ref Camera worldCamera)
        {
            worldCamera ??= Camera.main;
        }

        public void RebuildSlotRow()
        {
        }
    }
}
