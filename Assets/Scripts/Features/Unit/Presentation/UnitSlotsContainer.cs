using System.Collections.Generic;
using Features.Unit.Application;
using Features.Unit.Application.Ports;
using Features.Unit.Domain;
using Shared.Attributes;
using Shared.EventBus;
using Shared.Kernel;
using UnityEngine;
using UnityEngine.UI;
using UnitSpec = Features.Unit.Domain.Unit;

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
        [SerializeField] private UnitSlotInputHandler _inputHandlerPrefab;

        [Header("Layout")]
        [Required, SerializeField] private RectTransform _slotsParent;
        [Required, SerializeField] private Canvas _canvas;
        [SerializeField] private Camera _worldCamera;

        [Header("Placement")]
        [Tooltip("배치 영역 판정용 PlacementArea 참조.")]
        [SerializeField] private PlacementArea _placementArea;
        [Tooltip("배치 실패 시 에러 표시 UI View.")]
        [SerializeField] private PlacementErrorView _errorView;

        private IEventSubscriber _fullEventBus;
        private IEventSubscriber _eventBus;
        private SummonUnitUseCase _summonUseCase;
        private IUnitEnergyPort _energyPort;

        private UnitSpec[] _roster; // 최대 6개
        private DomainEntityId _ownerId;
        private int _nextIndex; // 다음에 표시할 슬롯 인덱스 (0~5 로테이션)
        private int _visibleStart; // 현재 보이는 슬롯의 시작 인덱스 (0~3)

        private readonly List<UnitSlotView> _activeSlots = new();

        /// <summary>전체 로스터 유닛 수.</summary>
        public int TotalUnits => _roster != null ? _roster.Length : 0;

        /// <summary>
        /// 소환 슬롯 컨테이너 초기화.
        /// </summary>
        public void Initialize(
            IEventSubscriber eventBus,
            SummonUnitUseCase summonUseCase,
            IUnitEnergyPort energyPort,
            UnitSpec[] roster,
            DomainEntityId ownerId,
            Vector3 defaultSpawnPosition,
            PlacementArea placementArea)
        {
            _fullEventBus = eventBus;
            _eventBus = eventBus;
            _summonUseCase = summonUseCase;
            _energyPort = energyPort;
            _roster = roster;
            _ownerId = ownerId;
            _placementArea = placementArea;

            // UnitSummonCompletedEvent 구독 — 소환 시 슬롯 교체
            _fullEventBus.Subscribe(this, new System.Action<Features.Unit.Application.Events.UnitSummonCompletedEvent>(OnSummonCompleted));

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
            slotGo.name = $"UnitSlot-{rosterIndex}";
            var slotView = slotGo.GetComponent<UnitSlotView>();
            slotView.Initialize(
                _eventBus,
                _summonUseCase,
                _energyPort,
                _roster[rosterIndex],
                _ownerId,
                spawnPosition);

            // 드래그 앤 드롭 핸들러 연결 (클릭 + 드래그 모두 이 핸들러가 담당)
            if (_inputHandlerPrefab != null && _canvas != null)
            {
                var inputGo = Instantiate(_inputHandlerPrefab.gameObject, slotGo.transform, false);
                var inputHandler = inputGo.GetComponent<UnitSlotInputHandler>();
                inputHandler.Initialize(
                    _roster[rosterIndex],
                    _fullEventBus,
                    OnSummonRequested,
                    _ => OnSlotClicked(slotView),
                    _canvas,
                    _worldCamera,
                    _placementArea,
                    _errorView);
            }

            _activeSlots.Add(slotView);
        }

        /// <summary>
        /// 드래그 앤 드롭으로 소환 요청.
        /// </summary>
        private void OnSummonRequested(UnitSpec unitSpec, Shared.Math.Float3 spawnPosition)
        {
            _summonUseCase.Execute(_ownerId, unitSpec, spawnPosition);
        }

        /// <summary>
        /// 슬롯 클릭 시 소환 실행.
        /// </summary>
        private void OnSlotClicked(UnitSlotView slotView)
        {
            if (slotView.UnitSpec == null) return;
            // 현재 슬롯의 스펙으로 소환 (PlacementArea 중심 위치 사용)
            var spawnPos = _placementArea != null ? _placementArea.Center : Vector3.zero;
            _summonUseCase.Execute(_ownerId, slotView.UnitSpec, new Shared.Math.Float3(spawnPos.x, spawnPos.y, spawnPos.z));
        }

        /// <summary>
        /// 소환 완료 이벤트 구독.
        /// </summary>
        private void OnSummonCompleted(Features.Unit.Application.Events.UnitSummonCompletedEvent e)
        {
            if (e.PlayerId != _ownerId) return;

            // 첫 번째 슬롯을 교체 (가장 왼쪽 슬롯)
            if (_activeSlots.Count > 0)
            {
                OnUnitSummoned(0);
            }
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

                // 새 슬롯 생성 — 소환 위치는 PlacementArea 중심 사용
                var spawnPos = _placementArea != null ? _placementArea.Center : Vector3.zero;
                CreateSlot(_nextIndex, spawnPos);
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

        /// <summary>
        /// 다음 3개로 로테이션 (Clash Royale 스타일).
        /// </summary>
        public void RotateNext()
        {
            if (_roster == null || _visibleStart + 3 >= _roster.Length) return;

            _visibleStart++;
            RebuildSlots();
        }

        /// <summary>
        /// 이전 3개로 로테이션.
        /// </summary>
        public void RotatePrevious()
        {
            if (_visibleStart <= 0) return;

            _visibleStart--;
            RebuildSlots();
        }

        private void RebuildSlots()
        {
            // 기존 슬롯 제거
            foreach (var slot in _activeSlots)
            {
                if (slot != null) Destroy(slot.gameObject);
            }
            _activeSlots.Clear();

            // 새 슬롯 생성
            var visibleCount = Mathf.Min(3, _roster.Length - _visibleStart);
            var spawnPos = _placementArea != null ? _placementArea.Center : Vector3.zero;
            for (var i = 0; i < visibleCount; i++)
            {
                CreateSlot(_visibleStart + i, spawnPos);
            }
        }

        private void OnDestroy()
        {
            if (_fullEventBus != null)
            {
                _fullEventBus.UnsubscribeAll(this);
            }
        }
    }
}
