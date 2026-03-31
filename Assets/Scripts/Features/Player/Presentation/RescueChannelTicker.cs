using Features.Player.Application;
using UnityEngine;

namespace Features.Player.Presentation
{
    public sealed class RescueChannelTicker : MonoBehaviour
    {
        private RescueChannelTracker _tracker;
        private PlayerUseCases _useCases;
        private bool _initialized;

        public void Initialize(RescueChannelTracker tracker, PlayerUseCases useCases)
        {
            if (tracker == null)
            {
                Debug.LogError("[RescueChannelTicker] RescueChannelTracker is not provided.", this);
                return;
            }

            _tracker = tracker;
            _useCases = useCases;
            _initialized = true;
        }

        private void Update()
        {
            if (!_initialized || !_tracker.IsActive) return;

            if (_tracker.Tick(Time.deltaTime))
            {
                var rescuerId = _useCases.LocalPlayer?.Id ?? default;
                _useCases.CompleteRescue(rescuerId, _tracker.TargetId);
                _tracker.Cancel();
            }
        }
    }
}
