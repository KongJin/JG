using Features.Unit.Application.Ports;
using Features.Unit.Domain;
using Features.Player.Application.Events;
using Shared.Attributes;
using Shared.EventBus;
using Shared.Kernel;
using Shared.Runtime;
using Shared.Runtime.Pooling;
using System.Globalization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnitSpec = Features.Unit.Domain.Unit;

namespace Features.Unit.Presentation
{
    /// <summary>
    /// 유닛 소환 슬롯 UI.
    /// 아이콘, 이름, 소환 비용, 에너지 부족/가능 상태 표시.
    /// 클릭 시 배치 모드 진입.
    /// </summary>
    public sealed class UnitSlotView : MonoBehaviour, IPointerClickHandler
    {
        [Header("References")]
        [Required, SerializeField] private Image _iconImage;
        [Required, SerializeField] private Text _nameText;
        [Required, SerializeField] private Text _costText;
        [Required, SerializeField] private GameObject _cannotAffordOverlay;
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private LayoutElement _layoutElement;

        [Header("Visual State")]
        [SerializeField] private Color _idleColor = new(0.08f, 0.14f, 0.21f, 0.96f);
        [SerializeField] private Color _selectedColor = new(0.19f, 0.42f, 0.67f, 1f);
        [SerializeField] private Color _disabledColor = new(0.05f, 0.08f, 0.12f, 0.92f);

        private IEventSubscriber _eventBus;
        private IUnitEnergyPort _energyPort;
        private System.Action<UnitSlotView> _selectionRequested;

        private UnitSpec _unitSpec;
        private DomainEntityId _ownerId;
        private int _slotIndex;
        private bool _canAfford;
        private bool _isSelected;

        /// <summary>현재 슬롯의 유닛 스펙.</summary>
        public UnitSpec UnitSpec => _unitSpec;
        public int SlotIndex => _slotIndex;
        public bool CanAfford => _canAfford;

        public void Initialize(
            IEventSubscriber eventBus,
            IUnitEnergyPort energyPort,
            UnitSpec unitSpec,
            DomainEntityId ownerId,
            int slotIndex,
            System.Action<UnitSlotView> selectionRequested)
        {
            _eventBus = eventBus;
            _energyPort = energyPort;
            _unitSpec = unitSpec;
            _ownerId = ownerId;
            _slotIndex = slotIndex;
            _selectionRequested = selectionRequested;

            ApplyPresentationDefaults();
            _eventBus.Subscribe(this, new System.Action<PlayerEnergyChangedEvent>(OnEnergyChanged));
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            if (_unitSpec == null) return;

            _nameText.text = FormatSlotName(_unitSpec.DisplayName);
            _costText.text = _unitSpec.SummonCost.ToString();

            _canAfford = _energyPort.GetCurrentEnergy(_ownerId) >= _unitSpec.SummonCost;
            RefreshVisualState();
        }

        private static string FormatSlotName(string rawName)
        {
            if (string.IsNullOrWhiteSpace(rawName))
            {
                return "Unit";
            }

            var trimmed = rawName.Trim();
            if (trimmed.StartsWith("frame_", System.StringComparison.OrdinalIgnoreCase))
            {
                trimmed = trimmed.Substring("frame_".Length);
            }

            trimmed = trimmed.Replace('_', ' ').Replace('-', ' ');

            return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(trimmed.ToLowerInvariant());
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            OnClicked();
        }

        /// <summary>슬롯 클릭 시 선택 요청 (InputHandler에서도 호출 가능).</summary>
        public void OnClicked()
        {
            _selectionRequested?.Invoke(this);
        }

        public void SetSelected(bool isSelected)
        {
            _isSelected = isSelected;
            RefreshVisualState();
        }

        private void OnEnergyChanged(PlayerEnergyChangedEvent e)
        {
            if (!_ownerId.Equals(e.PlayerId))
                return;

            UpdateDisplay();
        }

        private void RefreshVisualState()
        {
            _cannotAffordOverlay.SetActive(!_canAfford);

            var background = ResolveBackgroundImage();
            if (background == null)
                return;

            if (!_canAfford)
            {
                background.color = _disabledColor;
                return;
            }

            background.color = _isSelected ? _selectedColor : _idleColor;
        }

        private Image ResolveBackgroundImage()
        {
            if (_backgroundImage != null)
                return _backgroundImage;

            _backgroundImage = ComponentAccess.Get<Image>(gameObject);
            return _backgroundImage;
        }

        private void ApplyPresentationDefaults()
        {
            if (_layoutElement == null)
            {
                _layoutElement = ComponentAccess.Get<LayoutElement>(gameObject);
            }

            if (_nameText != null)
            {
                _nameText.fontSize = 15;
                _nameText.fontStyle = FontStyle.Bold;
                _nameText.alignment = TextAnchor.UpperLeft;
                _nameText.color = new Color(0.88f, 0.94f, 1f, 1f);
            }

            if (_costText != null)
            {
                _costText.fontSize = 22;
                _costText.fontStyle = FontStyle.Bold;
                _costText.alignment = TextAnchor.MiddleRight;
                _costText.color = Color.white;
            }

            if (_iconImage != null)
            {
                _iconImage.color = new Color(0.63f, 0.86f, 1f, 1f);
            }
        }

        private void OnDestroy()
        {
            _eventBus?.UnsubscribeAll(this);
        }
    }
}
