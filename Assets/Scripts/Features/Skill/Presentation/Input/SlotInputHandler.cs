using Shared.Attributes;
using Features.Skill.Application;
using Features.Skill.Domain;
using Features.Skill.Domain.Delivery;
using Shared.ErrorHandling;
using Shared.EventBus;
using Shared.Kernel;
using Shared.Lifecycle;
using Shared.Math;
using Shared.Runtime;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Features.Skill.Presentation
{
    public sealed class SlotInputHandler : MonoBehaviour
    {
        [Required, SerializeField] private InputActionAsset _inputActions;

        private CastSkillUseCase _castSkillUseCase;
        private SkillBar _skillBar;
        private DomainEntityId _casterId;
        private Transform _playerTransform;
        private Camera _camera;
        private IEventPublisher _eventPublisher;

        private readonly System.Action<InputAction.CallbackContext>[] _callbacks =
            new System.Action<InputAction.CallbackContext>[SkillBar.SlotCount];

        private InputAction[] _slotActions;
        private DisposableScope _disposables = new DisposableScope();

        public void Initialize(
            CastSkillUseCase castSkillUseCase,
            SkillBar skillBar,
            DomainEntityId casterId,
            Transform playerTransform,
            Camera camera,
            IEventPublisher eventPublisher
        )
        {
            _castSkillUseCase = castSkillUseCase;
            _skillBar = skillBar;
            _casterId = casterId;
            _playerTransform = playerTransform;
            _camera = camera;
            _eventPublisher = eventPublisher;
            _disposables.Dispose();
            _disposables = new DisposableScope();

            _slotActions = new[]
            {
                _inputActions.FindAction("SkillSlot0"),
                _inputActions.FindAction("SkillSlot1")
            };

            for (var i = 0; i < SkillBar.SlotCount; i++)
            {
                var index = i;
                _callbacks[i] = _ => CastSlot(index);
                _disposables.Add(InputActionSubscription.BindPerformed(_slotActions[i], _callbacks[i]));
            }
        }

        private void OnDestroy()
        {
            _disposables.Dispose();
        }

        private Float3 GetAimDirection()
        {
            var ray = _camera.ScreenPointToRay(Mouse.current.position.ReadValue());
            var plane = new Plane(Vector3.up, _playerTransform.position);
            if (plane.Raycast(ray, out var distance))
            {
                var hitPoint = ray.GetPoint(distance);
                var direction = hitPoint - _playerTransform.position;
                direction.y = 0f;
                if (direction.sqrMagnitude > 0.001f)
                {
                    return direction.normalized.ToFloat3();
                }
            }
            return _playerTransform.forward.ToFloat3();
        }

        private bool TryGetTargetPosition(out Float3 targetPosition)
        {
            var ray = _camera.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (
                Physics.Raycast(
                    ray,
                    out var hit,
                    _camera.farClipPlane,
                    Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore
                )
            )
            {
                if (ComponentAccess.TryGetEntityIdHolder(hit.collider, out var holder) && holder.Id != _casterId)
                {
                    targetPosition = hit.point.ToFloat3();
                    return true;
                }
            }

            targetPosition = Float3.Zero;
            return false;
        }

        private void CastSlot(int slotIndex)
        {
            var skill = _skillBar.GetSkill(slotIndex);
            if (skill == null)
            {
                _eventPublisher.Publish(
                    new UiErrorRequestedEvent(UiErrorMessage.Banner("Skill slot is empty.", "Skill"))
                );
                return;
            }

            var position = _playerTransform.position.ToFloat3();
            var direction = GetAimDirection();
            var hasTarget = TryGetTargetPosition(out var targetPosition);

            var result = _castSkillUseCase.Execute(
                skill,
                slotIndex,
                _casterId,
                Time.time,
                position,
                direction,
                skill.Delivery is not TargetedDelivery || hasTarget,
                targetPosition
            );
            UiErrorResultBridge.PublishBannerIfFailure(_eventPublisher, result, "Skill");
        }
    }
}
