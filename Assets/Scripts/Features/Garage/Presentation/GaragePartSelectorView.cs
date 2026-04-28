using System;
using UnityEngine;

namespace Features.Garage.Presentation
{
    public sealed class GaragePartSelectorView : MonoBehaviour
    {
        private bool _callbacksHooked;

        public string ValueText { get; private set; } = string.Empty;
        public string HintText { get; private set; } = string.Empty;

        public event Action<int> CycleRequested;

        public void Bind()
        {
            _callbacksHooked = true;
        }

        public void Render(string valueText, string hintText)
        {
            ValueText = valueText ?? string.Empty;
            HintText = hintText ?? string.Empty;
        }

        public void Cycle(int delta)
        {
            if (!_callbacksHooked)
                Bind();

            CycleRequested?.Invoke(delta);
        }
    }
}
