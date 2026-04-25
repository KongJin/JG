using Shared.EventBus;
using Shared.Math;
using Shared.Sound;
using UnityEngine;
using UnityEngine.UI;

namespace Shared.Ui
{
    [RequireComponent(typeof(Button))]
    public sealed class ButtonSoundEmitter : MonoBehaviour
    {
        [SerializeField] private string soundKey = "ui_click";
        [SerializeField] private float cooldown = 0.05f;

        private Button _button;
        private IEventPublisher _publisher;
        private string _ownerId;

        public void Initialize(IEventPublisher publisher, string ownerId)
        {
            _publisher = publisher;
            _ownerId = ownerId;
            _button = GetComponent<Button>();
            _button.onClick.RemoveListener(PublishClickSound);
            _button.onClick.AddListener(PublishClickSound);
        }

        private void PublishClickSound()
        {
            _publisher?.Publish(new SoundRequestEvent(new SoundRequest(
                soundKey,
                Float3.Zero,
                PlaybackPolicy.LocalOnly,
                _ownerId,
                cooldown)));
        }

        private void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(PublishClickSound);
        }
    }
}
