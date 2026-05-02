using Features.Player.Application.Events;
using Shared.EventBus;
using Shared.Kernel;
using Shared.Logging;
using UnityEngine;

namespace Features.Player.Presentation
{
    public sealed class PlayerHealthHudView : MonoBehaviour
    {
        [SerializeField] private Color _normalColor = Color.green;
        [SerializeField] private Color _lowColor = Color.red;
        [SerializeField] private float _lowHealthThreshold = 0.3f;
        [SerializeField] private Vector3 _headOffset = new(0f, 2.2f, 0f);

        private IEventSubscriber _eventBus;
        private DomainEntityId _playerId;
        private Transform _target;
        private Camera _worldCamera;

        public float CurrentHp { get; private set; }
        public float MaxHp { get; private set; }
        public Color CurrentColor { get; private set; }
        public Vector3 ScreenPosition { get; private set; }
        public bool IsVisible { get; private set; }

        public void Initialize(
            IEventSubscriber eventBus,
            DomainEntityId playerId,
            float maxHealth,
            bool isLocalPlayer,
            Transform target,
            Camera worldCamera)
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

            _eventBus = eventBus;
            _playerId = playerId;
            _target = target;
            _worldCamera = worldCamera;
            MaxHp = maxHealth;
            CurrentHp = maxHealth;
            UpdateFillColor(1f);

            _eventBus.Subscribe(this, new System.Action<PlayerHealthChangedEvent>(OnHealthChanged));
            _eventBus.Subscribe(this, new System.Action<PlayerRespawnedEvent>(OnRespawned));
        }

        private void OnDestroy()
        {
            _eventBus?.UnsubscribeAll(this);
        }

        private void LateUpdate()
        {
            if (_target == null || _worldCamera == null)
                return;

            ScreenPosition = _worldCamera.WorldToScreenPoint(_target.position + _headOffset);
            IsVisible = ScreenPosition.z > 0f;
        }

        private void OnHealthChanged(PlayerHealthChangedEvent e)
        {
            if (!e.PlayerId.Equals(_playerId))
                return;

            MaxHp = e.MaxHp;
            CurrentHp = e.CurrentHp;
            var healthPercent = e.MaxHp > 0f ? e.CurrentHp / e.MaxHp : 0f;
            UpdateFillColor(healthPercent);
        }

        private void OnRespawned(PlayerRespawnedEvent e)
        {
            if (!e.PlayerId.Equals(_playerId))
                return;

            MaxHp = e.MaxHp;
            CurrentHp = e.CurrentHp;
            IsVisible = true;
            UpdateFillColor(1f);
        }

        private void UpdateFillColor(float healthPercent)
        {
            CurrentColor = healthPercent <= _lowHealthThreshold ? _lowColor : _normalColor;
        }
    }
}
