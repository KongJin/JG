using Features.Skill.Application.Events;
using Features.Skill.Application.Ports;
using Shared.EventBus;
using Shared.Lifecycle;
using Shared.Math;
using Shared.Runtime.Pooling;
using System.Collections.Generic;
using UnityEngine;

namespace Features.Skill.Presentation
{
    public sealed class SkillCastEffectSpawner : MonoBehaviour
    {
        [SerializeField]
        private GameObject targetedEffectPrefab;

        [SerializeField]
        private GameObject selfEffectPrefab;

        private IEventSubscriber _eventBus;
        private ISkillEffectPort _effectPort;
        private DisposableScope _disposables = new DisposableScope();
        private readonly Dictionary<GameObject, GameObjectPool> _pools = new Dictionary<GameObject, GameObjectPool>();

        public void Initialize(IEventSubscriber eventBus, ISkillEffectPort effectPort = null)
        {
            _eventBus = eventBus;
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
            var pos = e.Position.ToVector3();
            var dir = e.Direction.ToVector3();
            var spawnPos = pos + dir * (e.Spec.Range * 0.5f);
            PlayCastSound(e.SkillId.Value, spawnPos);
        }

        private void OnTargetedRequested(TargetedRequestedEvent e)
        {
            var prefab = ResolveEffectPrefab(e.SkillId.Value, targetedEffectPrefab);
            if (prefab == null)
                return;

            var spawnPos = e.TargetPosition.ToVector3();
            var go = GetPool(prefab).Rent(spawnPos, Quaternion.identity);

            var effect = go.GetComponent<TargetedCastEffect>();
            if (effect != null)
                effect.Play();

            PlayCastSound(e.SkillId.Value, spawnPos);
        }

        private void OnSelfRequested(SelfRequestedEvent e)
        {
            var prefab = ResolveEffectPrefab(e.SkillId.Value, selfEffectPrefab);
            if (prefab == null)
                return;

            var pos = e.Position.ToVector3();
            var go = GetPool(prefab).Rent(pos, Quaternion.identity);

            var effect = go.GetComponent<SelfCastEffect>();
            if (effect != null)
                effect.Play();

            PlayCastSound(e.SkillId.Value, pos);
        }

        private GameObject ResolveEffectPrefab(string skillId, GameObject fallback)
        {
            if (_effectPort == null)
                return fallback;

            var prefab = _effectPort.GetEffectPrefab(skillId);
            return prefab != null ? prefab : fallback;
        }

        private void PlayCastSound(string skillId, Vector3 position)
        {
            if (_effectPort == null)
                return;

            var clip = _effectPort.GetCastSound(skillId);
            if (clip == null)
                return;

            AudioSource.PlayClipAtPoint(clip, position);
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
