using System.Collections.Generic;
using Features.Unit.Application;
using Features.Unit.Application.Ports;
using Features.Unit.Domain;
using Shared.Attributes;
using Shared.EventBus;
using Shared.Kernel;
using UnityEngine;
using UnityEngine.UI;

namespace Features.Unit.Presentation
{
    /// <summary>
    /// 3개 표시 슬롯 + 6개 로테이션 컨테이너.
    /// Clash Royale 스타일: 현재 보이는 3개 중 하나 선택 → 소환 → 다음 것으로 교체.
    /// </summary>
    public sealed class UnitSlotsContainer : MonoBehaviour
    {
        [Header("Slot Prefab")]
        [Required, SerializeField] private UnitSlotView _slotPrefab;

        [Header("Layout")]
        [Required, SerializeField] private RectTransform _slotsParent;

        private IEventSubscriber _eventBus;
        private SummonUnitUseCase _summonUseCase;
        private IUnitEnergyPort _energyPort;

        private Unit[] _roster; // 최대 6개
        private DomainEntityId _ownerId;
        private int _nextIndex; // 다음에 표시할 슬롯 인덱스 (0~5 로테이션)

        private readonly List<UnitSlotView> _activeSlots = new();

        /// <summary>
        /// 소환 슬롯 컨테이너 초기화.
        /// </summary>
        public void Initialize(
            IEventSubscriber eventBus,
            SummonUnitUseCase summonUseCase,
            IUnitEnergyPort energyPort,
            Unit[] roster,
            DomainEntityId ownerId,
            Vector3 defaultSpawnPosition)
        {
            _eventBus = eventBus;
            _summonUseCase = summonUseCase;
            _energyPort = energyPort;
            _roster = roster;
            _ownerId = ownerId;

            // 처음 3개 슬롯 생성
            var visibleCount = Mathf.Min(3, roster.Length);
            for (var i = 0; i < visibleCount; i++)
            {
                CreateSlot(i, defaultSpawnPosition);
            }

            _nextIndex = visibleCount;
        }

        private void CreateSlot(int rosterIndex, Vector3 spawnPosition)
        {
            if (rosterIndex >= _roster.Length) return;

            var slotGo = Instantiate(_slotPrefab.gameObject, _slotsParent, false);
            var slotView = slotGo.GetComponent<UnitSlotView>();
            slotView.Initialize(
                _eventBus,
                _summonUseCase,
                _energyPort,
                _roster[rosterIndex],
                _ownerId,
                spawnPosition);

            _activeSlots.Add(slotView);
        }

        /// <summary>
        /// 소환 완료 시 호출. 소환된 슬롯을 다음 로스터 항목으로 교체.
        /// </summary>
        public void OnUnitSummoned(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _activeSlots.Count) return;

            // 로테이션: 다음 항목이 있으면 교체, 없으면 슬롯 제거
            if (_nextIndex < _roster.Length)
            {
                // 기존 슬롯 제거
                var oldSlot = _activeSlots[slotIndex];
                if (oldSlot != null)
                {
                    _activeSlots.RemoveAt(slotIndex);
                    Destroy(oldSlot.gameObject);
                }

                // 새 슬롯 생성
                // spawnPosition은 기존 슬롯의 위치 재사용
                CreateSlot(_nextIndex, Vector3.zero); // TODO: 실제 위치 전달 필요
                _nextIndex++;
            }
            else
            {
                // 로스터 모두 소모 — 슬롯만 비활성화
                var oldSlot = _activeSlots[slotIndex];
                if (oldSlot != null)
                {
                    _activeSlots.RemoveAt(slotIndex);
                    Destroy(oldSlot.gameObject);
                }
            }
        }
    }
}
