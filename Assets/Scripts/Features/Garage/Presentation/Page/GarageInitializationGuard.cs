using Features.Garage.Application;
using Features.Unit.Application;
using Shared.EventBus;

namespace Features.Garage.Presentation
{
    /// <summary>
    /// Garage Page 컨트롤러 초기화 상태 검증 전용 클래스.
    /// CanRender()의 8개 null 체크를 중앙화.
    /// </summary>
    internal sealed class GarageInitializationGuard
    {
        public bool IsReady { get; private set; }
        public string MissingDependency { get; private set; }

        /// <summary>
        /// 모든 의존성이 준비되었는지 검증
        /// </summary>
        public bool Validate(
            GarageSetBUitkRuntimeAdapter adapter,
            GaragePageState state,
            GaragePagePresenter presenter,
            GaragePanelCatalog catalog,
            ComposeUnitUseCase composeUnit,
            ValidateRosterUseCase validateRoster,
            SaveRosterUseCase saveRoster,
            IEventPublisher eventPublisher)
        {
            MissingDependency = null;

            if (adapter == null)
            {
                MissingDependency = nameof(GarageSetBUitkRuntimeAdapter);
                return false;
            }

            if (state == null)
            {
                MissingDependency = nameof(GaragePageState);
                return false;
            }

            if (presenter == null)
            {
                MissingDependency = nameof(GaragePagePresenter);
                return false;
            }

            if (catalog == null)
            {
                MissingDependency = nameof(GaragePanelCatalog);
                return false;
            }

            if (composeUnit == null)
            {
                MissingDependency = nameof(ComposeUnitUseCase);
                return false;
            }

            if (validateRoster == null)
            {
                MissingDependency = nameof(ValidateRosterUseCase);
                return false;
            }

            if (saveRoster == null)
            {
                MissingDependency = nameof(SaveRosterUseCase);
                return false;
            }

            if (eventPublisher == null)
            {
                MissingDependency = nameof(IEventPublisher);
                return false;
            }

            IsReady = true;
            return true;
        }

        /// <summary>
        /// 준비 상태로 강제 설정 (초기화 완료 시 호출)
        /// </summary>
        public void MarkAsReady()
        {
            IsReady = true;
            MissingDependency = null;
        }

        /// <summary>
        /// 준비 상태 리셋
        /// </summary>
        public void Reset()
        {
            IsReady = false;
            MissingDependency = null;
        }
    }
}
