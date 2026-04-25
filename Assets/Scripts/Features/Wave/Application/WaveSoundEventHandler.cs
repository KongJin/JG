using System;
using Features.Combat.Application.Events;
using Features.Player.Application.Events;
using Shared.EventBus;
using Shared.Kernel;
using Shared.Math;
using Shared.Sound;

namespace Features.Wave.Application
{
    public sealed class WaveSoundEventHandler
    {
        private const float CoreWarningRatio = 0.3f;

        private readonly IEventPublisher _publisher;
        private readonly DomainEntityId _objectiveCoreId;
        private readonly float _coreMaxHp;
        private bool _coreWarningPlayed;

        public WaveSoundEventHandler(
            IEventSubscriber subscriber,
            IEventPublisher publisher,
            DomainEntityId objectiveCoreId,
            float coreMaxHp)
        {
            _publisher = publisher;
            _objectiveCoreId = objectiveCoreId;
            _coreMaxHp = coreMaxHp;

            subscriber.Subscribe(this, new Action<DamageAppliedEvent>(OnDamageApplied));
            subscriber.Subscribe(this, new Action<GameEndEvent>(OnGameEnd));
        }

        private void OnDamageApplied(DamageAppliedEvent e)
        {
            if (!_objectiveCoreId.Equals(e.TargetId))
            {
                _publisher.Publish(new SoundRequestEvent(new SoundRequest(
                    e.IsDead ? "battle_explosion" : "battle_hit",
                    Float3.Zero,
                    PlaybackPolicy.All,
                    string.Empty,
                    0.03f)));
                return;
            }

            if (e.IsDead)
            {
                _publisher.Publish(new SoundRequestEvent(new SoundRequest(
                    "core_destroyed",
                    Float3.Zero,
                    PlaybackPolicy.All,
                    string.Empty,
                    0.2f)));
                return;
            }

            if (_coreWarningPlayed || _coreMaxHp <= 0f)
                return;

            var ratio = e.RemainingHealth / _coreMaxHp;
            if (ratio > CoreWarningRatio)
                return;

            _coreWarningPlayed = true;
            _publisher.Publish(new SoundRequestEvent(new SoundRequest(
                "core_warning",
                Float3.Zero,
                PlaybackPolicy.All,
                string.Empty,
                1f)));
        }

        private void OnGameEnd(GameEndEvent e)
        {
            _publisher.Publish(new SoundRequestEvent(new SoundRequest(
                "bgm_result",
                Float3.Zero,
                PlaybackPolicy.All,
                string.Empty)));
        }
    }
}
