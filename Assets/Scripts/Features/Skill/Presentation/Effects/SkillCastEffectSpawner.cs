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
        private ISkillPresentationAssetPort _assetPort;
        private DisposableScope _disposables = new DisposableScope();
        private readonly Dictionary<GameObject, GameObjectPool> _pools = new Dictionary<GameObject, GameObjectPool>();

        public void Initialize(
            IEventSubscriber eventBus,
            IEventPublisher publisher,
            ISkillPresentationAssetPort assetPort = null)
        {
            _eventBus = eventBus;
            _publisher = publisher;
            _assetPort = assetPort;
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
            // csharp-guardrails: allow-null-defense
            if (prefab == null)
                return;

            var spawnPos = e.TargetPosition.ToVector3();
            var effect = GetPool(prefab).RentComponent<TargetedCastEffect>(spawnPos, Quaternion.identity);
// csharp-guardrails: allow-null-defense
            if (effect != null)
                effect.Play();

            PublishCastSound(e.SkillId.Value, e.TargetPosition, e.CasterId.Value);
        }

        private void OnSelfRequested(SelfRequestedEvent e)
        {
            var prefab = ResolveEffectPrefab(e.SkillId.Value, selfEffectPrefab);
            // csharp-guardrails: allow-null-defense
            if (prefab == null)
                return;

            var pos = e.Position.ToVector3();
            var effect = GetPool(prefab).RentComponent<SelfCastEffect>(pos, Quaternion.identity);
// csharp-guardrails: allow-null-defense
            if (effect != null)
                effect.Play();

            PublishCastSound(e.SkillId.Value, e.Position, e.CasterId.Value);
        }

        private GameObject ResolveEffectPrefab(string skillId, GameObject defaultPrefab)
        {
// csharp-guardrails: allow-null-defense
            if (_assetPort == null)
                return defaultPrefab;

            var prefab = _assetPort.GetEffectPrefab(skillId);
            // csharp-guardrails: allow-null-defense
            return prefab != null ? prefab : defaultPrefab;
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
