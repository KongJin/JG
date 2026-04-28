using Shared.EventBus;
using Shared.Math;
using Shared.Sound;
using UnityEngine;

namespace Shared.Ui
{
    public sealed class ButtonSoundEmitter : MonoBehaviour
    {
        [SerializeField] private string soundKey = "ui_click";
        [SerializeField] private float cooldown = 0.05f;

        private IEventPublisher _publisher;
        private string _ownerId;

        public void Initialize(IEventPublisher publisher, string ownerId)
        {
            _publisher = publisher;
            _ownerId = ownerId;
        }

        public void PublishClickSound()
        {
            _publisher?.Publish(new SoundRequestEvent(new SoundRequest(
                soundKey,
                Float3.Zero,
                PlaybackPolicy.LocalOnly,
                _ownerId,
                cooldown)));
        }
    }
}
