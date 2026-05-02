using Features.Player.Application.Events;
using Features.Unit.Application.Ports;
using Shared.EventBus;
using Shared.Kernel;
using System.Globalization;
using UnityEngine;
using UnitSpec = Features.Unit.Domain.Unit;

namespace Features.Unit.Presentation
{
    public sealed class UnitSlotView : MonoBehaviour
    {
        private IEventSubscriber _eventBus;
        private IUnitEnergyPort _energyPort;
        private System.Action<UnitSlotView> _selectionRequested;
        private UnitSpec _unitSpec;
        private DomainEntityId _ownerId;
        private int _slotIndex;
        private bool _canAfford;
        private bool _isSelected;

        public UnitSpec UnitSpec => _unitSpec;
        public int SlotIndex => _slotIndex;
        public bool CanAfford => _canAfford;
        public bool IsSelected => _isSelected;
        public string NameText { get; private set; } = string.Empty;
        public string CostText { get; private set; } = string.Empty;

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

            _eventBus.Subscribe(this, new System.Action<PlayerEnergyChangedEvent>(OnEnergyChanged));
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            if (_unitSpec == null)
                return;

            NameText = FormatSlotName(_unitSpec.DisplayName);
            CostText = _unitSpec.SummonCost.ToString(CultureInfo.InvariantCulture);
            _canAfford = _energyPort.GetCurrentEnergy(_ownerId) >= _unitSpec.SummonCost;
        }

        private static string FormatSlotName(string rawName)
        {
            if (string.IsNullOrWhiteSpace(rawName))
                return "Unit";

            var trimmed = rawName.Trim();
            if (trimmed.StartsWith("frame_", System.StringComparison.OrdinalIgnoreCase))
                trimmed = trimmed.Substring("frame_".Length);

            trimmed = trimmed.Replace('_', ' ').Replace('-', ' ');
            return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(trimmed.ToLowerInvariant());
        }

        public void OnClicked()
        {
            _selectionRequested?.Invoke(this);
        }

        public void SetSelected(bool isSelected)
        {
            _isSelected = isSelected;
        }

        private void OnEnergyChanged(PlayerEnergyChangedEvent e)
        {
            if (_ownerId.Equals(e.PlayerId))
                UpdateDisplay();
        }

        private void OnDestroy()
        {
            _eventBus?.UnsubscribeAll(this);
        }
    }
}
