using Features.Player.Application.Events;
using Shared.EventBus;
using Shared.Kernel;
using UnityEngine;
using UnityEngine.UI;

namespace Features.Player.Presentation
{
    public sealed class PlayerHealthHudView : MonoBehaviour
    {
        [SerializeField]
        private Slider _healthSlider;

        [SerializeField]
        private Image _healthFillImage;

        [SerializeField]
        private Color _normalColor = Color.green;

        [SerializeField]
        private Color _lowColor = Color.red;

        [SerializeField]
        private float _lowHealthThreshold = 0.3f;

        [SerializeField]
        private Vector3 _headOffset = new Vector3(0f, 2.2f, 0f);

        private IEventSubscriber _eventBus;
        private DomainEntityId _playerId;
        private Transform _target;
        private Camera _worldCamera;
        private Canvas _hudCanvas;
        private RectTransform _rectTransform;
        private RectTransform _hudCanvasRectTransform;

        private void Awake()
        {
            _rectTransform = transform as RectTransform;

            if (_healthSlider == null)
            {
                Debug.LogError("[PlayerHealthHudView] HealthSlider is not assigned.", this);
            }

            if (_healthFillImage == null)
            {
                Debug.LogError("[PlayerHealthHudView] HealthFillImage is not assigned.", this);
            }

            if (_rectTransform == null)
            {
                Debug.LogError("[PlayerHealthHudView] RectTransform is missing.", this);
            }
        }

        public void Initialize(
            IEventSubscriber eventBus,
            DomainEntityId playerId,
            float maxHealth,
            Transform target,
            Camera worldCamera,
            Canvas hudCanvas
        )
        {
            if (eventBus == null)
            {
                Debug.LogError("[PlayerHealthHudView] EventBus is missing.", this);
                return;
            }

            if (target == null)
            {
                Debug.LogError("[PlayerHealthHudView] Target is missing.", this);
                return;
            }

            if (worldCamera == null)
            {
                Debug.LogError("[PlayerHealthHudView] World camera is missing.", this);
                return;
            }

            if (hudCanvas == null)
            {
                Debug.LogError("[PlayerHealthHudView] Hud canvas is missing.", this);
                return;
            }

            _eventBus = eventBus;
            _playerId = playerId;
            _target = target;
            _worldCamera = worldCamera;
            _hudCanvas = hudCanvas;
            _hudCanvasRectTransform = hudCanvas.transform as RectTransform;

            _healthSlider.maxValue = maxHealth;
            _healthSlider.value = maxHealth;
            UpdateFillColor(1f);

            _eventBus.Subscribe(this, new System.Action<PlayerHealthChangedEvent>(OnHealthChanged));
            _eventBus.Subscribe(this, new System.Action<PlayerRespawnedEvent>(OnRespawned));
        }

        private void LateUpdate()
        {
            if (_target == null)
                return;

            var screenPoint = _worldCamera.WorldToScreenPoint(_target.position + _headOffset);
            if (screenPoint.z <= 0f)
            {
                _rectTransform.anchoredPosition = new Vector2(100000f, 100000f);
                return;
            }

            var uiCamera = _hudCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : _hudCanvas.worldCamera;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _hudCanvasRectTransform,
                screenPoint,
                uiCamera,
                out var anchoredPosition
            );
            _rectTransform.anchoredPosition = anchoredPosition;
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
