using Shared.Attributes;
using Features.Skill.Application;
using Features.Skill.Application.Ports;
using Features.Skill.Domain;
using Features.Skill.Infrastructure;
using Features.Skill.Presentation;
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

        private EventBus _eventBus;
        private SkillCatalog _catalog;
        private EquipSkillUseCase _equipSkillUseCase;
        private SkillBar _skillBar;
        private Deck _deck;
        private DisposableScope _disposables;

        public SkillCatalog Catalog => _catalog;

        public void Initialize(EventBus eventBus, Transform playerTransform, Camera camera, DomainEntityId casterId, IManaPort manaPort, IStatusQueryPort statusQuery = null)
        {
            _eventBus = eventBus;
            _disposables?.Dispose();
            _disposables = new DisposableScope();

            _catalog = new SkillCatalog(_catalogData);

            _barView.Initialize(eventBus, new SkillIconAdapter(_catalog), casterId);
            _skillCastEffectSpawner.Initialize(eventBus, eventBus, new SkillEffectAdapter(_catalog));

            new SkillNetworkEventHandler(_eventBus, _networkAdapter);

            _equipSkillUseCase = new EquipSkillUseCase(_eventBus);

            // Build deck from all catalog skills
            var allSkillIds = CollectCatalogSkillIds();
            var domainIds = new List<DomainEntityId>();
            foreach (var id in allSkillIds)
                domainIds.Add(new DomainEntityId(id));
            _deck = new Deck(domainIds);

            // Draw initial hand from deck
            _skillBar = new SkillBar();
            for (var i = 0; i < SkillBar.SlotCount; i++)
            {
                var drawnId = _deck.Draw();
                if (string.IsNullOrEmpty(drawnId.Value)) continue;
                var skill = _catalog.Get(drawnId.Value);
                if (skill != null)
                    _equipSkillUseCase.Execute(_skillBar, i, skill);
            }

            // Deck cycling: cast -> discard -> draw next
            var deckCycleHandler = new DeckCycleHandler(
                _deck, _skillBar, _equipSkillUseCase,
                id => _catalog.Get(id), casterId, eventBus);
            _disposables.Add(EventBusSubscription.ForOwner(eventBus, deckCycleHandler));

            var castSkillUseCase = new CastSkillUseCase(manaPort, _networkAdapter, statusQuery);

            _slotInputHandler.Initialize(
                castSkillUseCase,
                _skillBar,
                casterId,
                playerTransform,
                camera,
                eventBus
            );
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

        private List<string> CollectCatalogSkillIds()
        {
            var ids = new List<string>();
            if (_catalog == null || _catalog.AllSkills == null)
                return ids;

            foreach (var skillData in _catalog.AllSkills)
            {
                if (skillData == null || string.IsNullOrWhiteSpace(skillData.SkillId))
                    continue;
                if (ids.Contains(skillData.SkillId))
                    continue;
                ids.Add(skillData.SkillId);
            }

            return ids;
        }
    }
}
