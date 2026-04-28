using Shared.Attributes;
using Features.Skill.Application.Events;
using Shared.EventBus;
using Shared.Lifecycle;
using Shared.Math;
using Shared.Runtime.Pooling;
using Shared.Sound;
using System.Collections.Generic;
using UnityEngine;

namespace Features.Skill.Presentation
{
    public sealed class SkillCastEffectSpawner : MonoBehaviour
    {
        [Required, SerializeField]
        private GameObject targetedEffectPrefab;

        [Required, SerializeField]
        private GameObject selfEffectPrefab;

        private IEventSubscriber _eventBus;
        private IEventPublisher _publisher;
        private ISkillEffectPort _effectPort;
        private DisposableScope _disposables = new DisposableScope();
        private readonly Dictionary<GameObject, GameObjectPool> _pools = new Dictionary<GameObject, GameObjectPool>();

        public void Initialize(
            IEventSubscriber eventBus,
            IEventPublisher publisher,
            ISkillEffectPort effectPort = null)
        {
            _eventBus = eventBus;
            _publisher = publisher;
            _effectPort = effectPort;
            _disposables.Dispose();
            _disposables = new DisposableScope();
            _disposables.Add(EventBusSubscription.ForOwner(_eventBus, this));
            _eventBus.Subscribe(this, new System.Action<ZoneRequestedEvent>(OnZoneRequested));
            _eventBus.Subscribe(
                this,
                new System.Action<TargetedRequestedEvent>(OnTargetedRequested)
            );
            _eventBus.Subscribe(this, new System.Action<SelfRequestedEvent>(OnSelfRequested));
        }

        private void OnDestroy()
        {
            _disposables.Dispose();
        }

        private void OnZoneRequested(ZoneRequestedEvent e)
        {
            var spawnPos = e.Position + e.Direction * (e.Spec.Range * 0.5f);
            PublishCastSound(e.SkillId.Value, spawnPos, e.CasterId.Value);
        }

        private void OnTargetedRequested(TargetedRequestedEvent e)
        {
            var prefab = ResolveEffectPrefab(e.SkillId.Value, targetedEffectPrefab);
            if (prefab == null)
                return;

            var spawnPos = e.TargetPosition.ToVector3();
            var effect = GetPool(prefab).RentComponent<TargetedCastEffect>(spawnPos, Quaternion.identity);
            if (effect != null)
                effect.Play();

            PublishCastSound(e.SkillId.Value, e.TargetPosition, e.CasterId.Value);
        }

        private void OnSelfRequested(SelfRequestedEvent e)
        {
            var prefab = ResolveEffectPrefab(e.SkillId.Value, selfEffectPrefab);
            if (prefab == null)
                return;

            var pos = e.Position.ToVector3();
            var effect = GetPool(prefab).RentComponent<SelfCastEffect>(pos, Quaternion.identity);
            if (effect != null)
                effect.Play();

            PublishCastSound(e.SkillId.Value, e.Position, e.CasterId.Value);
        }

        private GameObject ResolveEffectPrefab(string skillId, GameObject fallback)
        {
            if (_effectPort == null)
                return fallback;

            var prefab = _effectPort.GetEffectPrefab(skillId);
            return prefab != null ? prefab : fallback;
        }

        private void PublishCastSound(string skillId, Float3 position, string ownerId)
        {
            _publisher.Publish(new SoundRequestEvent(new SoundRequest(
                $"skill_{skillId}_cast",
                position,
                PlaybackPolicy.All,
                ownerId)));
        }

        private GameObjectPool GetPool(GameObject prefab)
        {
            if (!_pools.TryGetValue(prefab, out var pool))
            {
                pool = new GameObjectPool(prefab, transform);
                _pools.Add(prefab, pool);
            }

            return pool;
        }
    }
}
