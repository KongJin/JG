using Features.Unit.Application;
using Features.Unit.Application.Ports;
using Features.Unit.Domain;
using Shared.Attributes;
using Shared.EventBus;
using Shared.Kernel;
using Shared.Math;
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

        [Header("Summon Settings")]
        [SerializeField] private Transform _spawnParent;

        private IEventSubscriber _eventBus;
        private SummonUnitUseCase _summonUseCase;
        private IUnitEnergyPort _energyPort;

        private UnitSpec _unitSpec;
        private DomainEntityId _ownerId;
        private Vector3 _spawnPosition;
        private bool _canAfford;

        /// <summary>현재 슬롯의 유닛 스펙.</summary>
        public UnitSpec UnitSpec => _unitSpec;

        public void Initialize(
            IEventSubscriber eventBus,
            SummonUnitUseCase summonUseCase,
            IUnitEnergyPort energyPort,
            UnitSpec unitSpec,
            DomainEntityId ownerId,
            Vector3 spawnPosition)
        {
            _eventBus = eventBus;
            _summonUseCase = summonUseCase;
            _energyPort = energyPort;
            _unitSpec = unitSpec;
            _ownerId = ownerId;
            _spawnPosition = spawnPosition;

            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            if (_unitSpec == null) return;

            _nameText.text = _unitSpec.DisplayName;
            _costText.text = _unitSpec.SummonCost.ToString();

            _canAfford = _energyPort.GetCurrentEnergy(_ownerId) >= _unitSpec.SummonCost;
            _cannotAffordOverlay.SetActive(!_canAfford);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!_canAfford) return;
            OnClicked();
        }

        /// <summary>슬롯 클릭 시 소환 실행 (InputHandler에서도 호출 가능).</summary>
        public void OnClicked()
        {
            if (!_canAfford) return;
            Summon();
        }

        private void Summon()
        {
            if (_unitSpec == null) return;

            var success = _summonUseCase.Execute(
                _ownerId,
                _unitSpec,
                new Float3(_spawnPosition.x, _spawnPosition.y, _spawnPosition.z));

            if (success)
            {
                UpdateDisplay(); // 에너지 차감 후 상태 갱신
            }
        }
    }
}
