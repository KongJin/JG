using Features.Player.Application;
using UnityEngine;

namespace Features.Player.Presentation
{
    public sealed class BleedoutTicker : MonoBehaviour
    {
        private BleedoutTracker _tracker;
        private bool _initialized;

        public void Initialize(BleedoutTracker tracker)
        {
            if (tracker == null)
            {
                Debug.LogError("[BleedoutTicker] BleedoutTracker is not provided.", this);
                return;
            }

            _tracker = tracker;
            _initialized = true;
        }

        private void Update()
        {
            if (!_initialized) return;
            _tracker.Tick(Time.deltaTime);
        }
    }
}
