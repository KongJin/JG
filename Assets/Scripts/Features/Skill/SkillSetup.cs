using Shared.Attributes;
using Features.Skill.Application;
using Features.Skill.Application.Ports;
using Features.Skill.Domain;
using Features.Skill.Infrastructure;
using Features.Skill.Presentation;
using Shared.EventBus;
using Shared.Kernel;
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
        private SkillLoadoutData _loadoutData;

        private EventBus _eventBus;
        private SkillCatalog _catalog;
        private EquipSkillUseCase _equipSkillUseCase;
        private SkillBar _skillBar;
        private SkillRotator _skillRotator;

        public SkillCatalog Catalog => _catalog;

        public void Initialize(EventBus eventBus, Transform playerTransform, Camera camera, DomainEntityId casterId, IStatusQueryPort statusQuery = null)
        {
            _eventBus = eventBus;

            _catalog = new SkillCatalog(_catalogData);
            _skillRotator = new SkillRotator(CollectCatalogSkillIds());

            _barView.Initialize(eventBus, new SkillIconAdapter(_catalog), casterId);
            _skillCastEffectSpawner.Initialize(eventBus, eventBus, new SkillEffectAdapter(_catalog));

            new SkillNetworkEventHandler(_eventBus, _networkAdapter);

            var cooldownTracker = new CooldownTracker();

            var loadoutRepo = new SkillLoadoutRepository(_loadoutData);
            _equipSkillUseCase = new EquipSkillUseCase(_eventBus, cooldownTracker);
            _skillBar = _equipSkillUseCase.BuildFromLoadout(
                loadoutRepo.Load(),
                skillId => _catalog.Get(skillId)
            );
            _barView.SetSlotClickHandler(slotIndex =>
                _skillRotator.HandleSlotSwap(_skillBar, slotIndex, id => _catalog.Get(id), _equipSkillUseCase)
            );

            var castSkillUseCase = new CastSkillUseCase(cooldownTracker, _networkAdapter, statusQuery);

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
