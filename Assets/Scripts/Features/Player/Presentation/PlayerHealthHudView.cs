using Shared.Attributes;
using Features.Player.Application.Events;
using Features.Player.Runtime;
using Shared.EventBus;
using Shared.Kernel;
using Shared.Logging;
using UnityEngine;
using UnityEngine.UI;

namespace Features.Player.Presentation
{
    public sealed class PlayerHealthHudView : MonoBehaviour
    {
        [Required, SerializeField]
        private Slider _healthSlider;

        [Required, SerializeField]
        private Image _healthFillImage;

        [Required, SerializeField]
        private Color _normalColor = Color.green;

        [Required, SerializeField]
        private Color _lowColor = Color.red;

        [Required, SerializeField]
        private float _lowHealthThreshold = 0.3f;

        [Required, SerializeField]
        private Vector3 _headOffset = new Vector3(0f, 2.2f, 0f);

        private IEventSubscriber _eventBus;
        private DomainEntityId _playerId;

        public void Initialize(
            IEventSubscriber eventBus,
            DomainEntityId playerId,
            float maxHealth,
            bool isLocalPlayer,
            Transform target,
            Camera worldCamera,
            Canvas hudCanvas
        )
        {
            if (eventBus == null)
            {
                Log.Error("Player", "[PlayerHealthHudView] EventBus is missing.", this);
                return;
            }

            if (target == null)
            {
                Log.Error("Player", "[PlayerHealthHudView] Target is missing.", this);
                return;
            }

            if (worldCamera == null)
            {
                Log.Error("Player", "[PlayerHealthHudView] World camera is missing.", this);
                return;
            }

            if (hudCanvas == null)
            {
                Log.Error("Player", "[PlayerHealthHudView] Hud canvas is missing.", this);
                return;
            }

            _eventBus = eventBus;
            _playerId = playerId;

            _healthSlider.maxValue = maxHealth;
            _healthSlider.value = maxHealth;
            UpdateFillColor(1f);

            var follower = gameObject.GetComponent<PlayerHealthHudFollower>();
            if (follower == null)
                follower = gameObject.AddComponent<PlayerHealthHudFollower>();

            follower.Initialize(target, worldCamera, hudCanvas, _headOffset);

            _eventBus.Subscribe(this, new System.Action<PlayerHealthChangedEvent>(OnHealthChanged));
            _eventBus.Subscribe(this, new System.Action<PlayerRespawnedEvent>(OnRespawned));
        }

        private void OnDestroy()
        {
            _eventBus?.UnsubscribeAll(this);
        }

        private void OnHealthChanged(PlayerHealthChangedEvent e)
        {
            if (!e.PlayerId.Equals(_playerId))
                return;

            _healthSlider.maxValue = e.MaxHp;
            _healthSlider.value = e.CurrentHp;

            var healthPercent = e.MaxHp > 0f ? e.CurrentHp / e.MaxHp : 0f;
            UpdateFillColor(healthPercent);
        }

        private void OnRespawned(PlayerRespawnedEvent e)
        {
            if (!e.PlayerId.Equals(_playerId))
                return;

            _healthSlider.maxValue = e.MaxHp;
            _healthSlider.value = e.CurrentHp;

            gameObject.SetActive(true);
            UpdateFillColor(1f);
        }

        private void UpdateFillColor(float healthPercent)
        {
            _healthFillImage.color = healthPercent <= _lowHealthThreshold
                ? _lowColor
                : _normalColor;
        }
    }
}
