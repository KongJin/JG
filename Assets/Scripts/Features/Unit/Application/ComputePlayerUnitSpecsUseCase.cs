using Features.Unit.Application.Events;
using Features.Unit.Application.Ports;
using Features.Unit.Domain;
using Shared.EventBus;
using Shared.Kernel;
using Shared.Time;
using System.Collections.Generic;

namespace Features.Unit.Application
{
    /// <summary>
    /// GarageRoster의 UnitLoadout[]를 실제 Unit 스펙으로 계산하는 UseCase.
    /// 각 Loadout별로 ComposeUnitUseCase를 호출하여 Unit[] 생성.
    /// </summary>
    public sealed class ComputePlayerUnitSpecsUseCase
    {
        private readonly ComposeUnitUseCase _composeUnitUseCase;
        private readonly IClockPort _clock;
        private readonly IEventPublisher _eventBus;

        public ComputePlayerUnitSpecsUseCase(
            ComposeUnitUseCase composeUnitUseCase,
            IClockPort clock,
            IEventPublisher eventBus)
        {
            _composeUnitUseCase = composeUnitUseCase;
            _clock = clock;
            _eventBus = eventBus;
        }

        /// <summary>
        /// UnitLoadout[]를 Unit[]로 계산.
        /// </summary>
        /// <param name="loadouts">Garage에서 저장한 유닛 편성</param>
        /// <param name="ownerId">소유자 플레이어 ID</param>
        /// <returns>계산된 Unit 스펙 배열 (실패한 항목 제외)</returns>
        public Unit[] Execute(Garage.Domain.GarageRoster.UnitLoadout[] loadouts, DomainEntityId ownerId)
        {
            var units = new List<Unit>();

            foreach (var loadout in loadouts)
            {
                var unitId = _clock.NewId();
                var result = _composeUnitUseCase.Execute(
                    unitId,
                    loadout.frameId,
                    loadout.firepowerModuleId,
                    loadout.mobilityModuleId);

                if (result.IsSuccess)
                {
                    units.Add(result.Value);
                }
            }

            var computedUnits = units.ToArray();
            _eventBus.Publish(new UnitSpecsComputedEvent(ownerId, computedUnits));
            return computedUnits;
        }
    }
}
