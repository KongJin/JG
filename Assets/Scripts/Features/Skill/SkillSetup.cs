using Shared.Attributes;
using Features.Skill.Application;
using Features.Skill.Application.Events;
using Features.Skill.Application.Ports;
using Features.Skill.Domain;
using Features.Skill.Infrastructure;
using Features.Skill.Presentation;
using Features.Wave.Application.Ports;
using Shared.EventBus;
using Shared.Kernel;
using Shared.Lifecycle;
using System.Collections.Generic;
using UnityEngine;

namespace Features.Skill
{
    public sealed class SkillSetup : MonoBehaviour
    {
        [Required, SerializeField]
        private SlotInputHandler _slotInputHandler;

        [Required, SerializeField]
        private SkillCastEffectSpawner _skillCastEffectSpawner;

        [Required, SerializeField]
        private BarView _barView;

        [Required, SerializeField]
        private SkillNetworkAdapter _networkAdapter;

        [Required, SerializeField]
        private SkillCatalogData _catalogData;

        [Required, SerializeField]
        private StartSkillSelectionView _startSkillSelectionView;

        private EventBus _eventBus;
        private SkillCatalog _catalog;
        private EquipSkillUseCase _equipSkillUseCase;
        private SkillBar _skillBar;
        private Deck _deck;
        private DisposableScope _disposables;
        private SkillRewardAdapter _skillRewardAdapter;
        private SkillIconAdapter _skillIconAdapter;
        private SkillUpgradeAdapter _skillUpgradeAdapter;

        // 단계 간 공유 상태
        private Transform _playerTransform;
        private Camera _camera;
        private DomainEntityId _casterId;
        private IManaPort _manaPort;
        private IStatusQueryPort _statusQuery;
        private List<InitializeDeckUseCase.SkillEntry> _entries;

        public ISkillRewardPort SkillReward => _skillRewardAdapter;
        public ISkillIconPort SkillIcon => _skillIconAdapter;
        public ISkillUpgradeQueryPort SkillUpgradeQuery => _skillUpgradeAdapter;
        public ISkillUpgradeCommandPort SkillUpgradeCommand => _skillUpgradeAdapter;

        public void InitializePreSelection(
            EventBus eventBus, Transform playerTransform, Camera camera,
            DomainEntityId casterId, IManaPort manaPort,
            IStatusQueryPort statusQuery,
            System.Action onComplete)
        {
            _eventBus = eventBus;
            _playerTransform = playerTransform;
            _camera = camera;
            _casterId = casterId;
            _manaPort = manaPort;
            _statusQuery = statusQuery;
            _disposables?.Dispose();
            _disposables = new DisposableScope();

            _catalog = new SkillCatalog(_catalogData);
            _skillIconAdapter = new SkillIconAdapter(_catalog);

            _barView.Initialize(eventBus, _skillIconAdapter, casterId);
            _skillCastEffectSpawner.Initialize(eventBus, eventBus, new SkillEffectAdapter(_catalog));
            new SkillNetworkEventHandler(_eventBus, _networkAdapter);

            // 카탈로그에서 후보 목록 + 엔트리 생성
            var uniqueSkills = _catalog.UniqueSkills;
            _entries = new List<InitializeDeckUseCase.SkillEntry>(uniqueSkills.Length);
            var candidates = new StartSkillCandidate[uniqueSkills.Length];
            for (var i = 0; i < uniqueSkills.Length; i++)
            {
                var data = uniqueSkills[i];
                var name = data.Presentation != null ? data.Presentation.DisplayName : data.SkillId;
                _entries.Add(new InitializeDeckUseCase.SkillEntry(data.SkillId, name));
                candidates[i] = new StartSkillCandidate(data.SkillId, name);
            }

            // 선택 완료 핸들러 (콜백 → Phase 2 트리거)
            var selectionHandler = new StartSkillSelectionHandler(
                eventBus,
                chosenIds => InitializePostSelection(chosenIds, onComplete));
            _disposables.Add(EventBusSubscription.ForOwner(eventBus, selectionHandler));

            // 선택 UI 초기화 + 이벤트 발행
            _startSkillSelectionView.Initialize(eventBus, eventBus, _skillIconAdapter);
            eventBus.Publish(new StartSkillSelectionRequestedEvent(candidates, SkillBar.SlotCount));
        }

        private void InitializePostSelection(string[] chosenSkillIds, System.Action onComplete)
        {
            _equipSkillUseCase = new EquipSkillUseCase(_eventBus);

            var initDeckUseCase = new InitializeDeckUseCase();
            var deckSetup = initDeckUseCase.Execute(_entries, chosenSkillIds);
            _deck = deckSetup.Deck;
            _skillBar = deckSetup.SkillBar;

            // 선택한 스킬 장착
            var initialHand = deckSetup.InitialHandIds;
            for (var i = 0; i < initialHand.Count && i < SkillBar.SlotCount; i++)
            {
                var skill = _catalog.Get(initialHand[i].Value);
                if (skill != null)
                    _equipSkillUseCase.Execute(_skillBar, i, skill);
            }

            // 덱 순환 핸들러
            var deckCycleHandler = new DeckCycleHandler(
                _deck, _skillBar, _equipSkillUseCase,
                id => _catalog.Get(id), _casterId, _eventBus);
            _disposables.Add(EventBusSubscription.ForOwner(_eventBus, deckCycleHandler));

            _skillUpgradeAdapter = new SkillUpgradeAdapter(
                new Domain.SkillUpgradeLevel(),
                id => _catalog.GetData(id)?.GrowthAxes.GetEnabledAxes());
            var castSkillUseCase = new CastSkillUseCase(_manaPort, _networkAdapter, _statusQuery, _skillUpgradeAdapter);

            _slotInputHandler.Initialize(
                castSkillUseCase,
                _skillBar,
                _casterId,
                _playerTransform,
                _camera,
                _eventBus);

            _skillRewardAdapter = new SkillRewardAdapter(
                _deck, _skillBar, deckSetup.RewardPool,
                _skillUpgradeAdapter, _skillUpgradeAdapter,
                id => _catalog.GetData(id)?.Presentation?.DisplayName ?? id,
                id => _catalog.GetData(id)?.GrowthAxes.GetEnabledAxes());

            // SkillsReady CustomProperty 설정
            _networkAdapter.SyncSkillsReady();

            onComplete?.Invoke();
        }

        public Result SwapSkill(int slotIndex, string skillId)
        {
            var skill = _catalog.Get(skillId);
            if (skill == null)
                return Result.Failure($"Skill not found: {skillId}");

            return _equipSkillUseCase.Execute(_skillBar, slotIndex, skill);
        }

        private void OnDestroy()
        {
            _disposables?.Dispose();
        }
    }
}
