using Features.Player.Application.Ports;
using Shared.Runtime;
using Shared.Runtime.Pooling;
using Shared.Math;
using UnityEngine;

namespace Features.Player.Infrastructure
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMotorAdapter : MonoBehaviour, IPlayerMotorPort
    {
        private CharacterController _controller;

        private void Awake()
        {
            _controller = ComponentAccess.Get<CharacterController>(gameObject);
        }

        public MotorResult Move(Float3 delta)
        {
            _controller.Move(delta.ToVector3());
            return new MotorResult(
                transform.position.ToFloat3(),
                _controller.isGrounded
            );
        }

        public void Rotate(Float3 direction, float rotationSpeed, float deltaTime)
        {
            if (direction == Float3.Zero)
                return;

            var targetRotation = Quaternion.LookRotation(direction.ToVector3());
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotationSpeed * deltaTime
            );
        }
    }
}
