#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using System.Collections.Generic;
using Features.Combat;
using Features.Combat.Domain;
using Features.Enemy.Application.Events;
using Features.Player.Application.Events;
using Features.Wave;
using Features.Wave.Application.Events;
using Shared.Attributes;
using Shared.EventBus;
using Shared.Kernel;
using UnityEngine;

namespace Features.Player
{
    public sealed class BattleSceneMcpSmokeDriver : MonoBehaviour, IBattleSceneEventBusConsumer
    {
        [Required, SerializeField] private CombatSetup _combatSetup;
        [Required, SerializeField] private CoreObjectiveSetup _coreObjective;
        [Required, SerializeField] private PlayerSceneRegistry _playerSceneRegistry;

        private EventBus _eventBus;
        private Coroutine _finalWaveClearRoutine;
        private bool _finalWaveClearGameEnded;
        private readonly HashSet<DomainEntityId> _activeEnemyIds = new();
        private readonly List<DomainEntityId> _enemyIdBuffer = new();

        public void Initialize(EventBus eventBus)
        {
            _eventBus = eventBus;
// csharp-guardrails: allow-null-defense
            _eventBus?.Subscribe(this, new System.Action<GameEndReportRequestedEvent>(_ =>
            {
                _finalWaveClearGameEnded = true;
            }));
// csharp-guardrails: allow-null-defense
            _eventBus?.Subscribe(this, new System.Action<EnemySpawnedEvent>(OnEnemySpawned));
// csharp-guardrails: allow-null-defense
            _eventBus?.Subscribe(this, new System.Action<EnemyDiedEvent>(OnEnemyDied));
        }

        public void ForceCoreDefeatForMcpSmoke()
        {
// csharp-guardrails: allow-null-defense
            if (_combatSetup == null || _coreObjective == null)
                return;

            var lethalDamage = _coreObjective.CoreMaxHp + 10000f;
            _combatSetup.ApplyDamage(
                _coreObjective.CoreId,
                lethalDamage,
                DamageType.Physical,
                ResolveAttackerId());
        }

        public void ForceVictoryForMcpSmoke()
        {
// csharp-guardrails: allow-null-defense
            _eventBus?.Publish(new WaveVictoryEvent());
        }

        public void RunFinalWaveClearForMcpSmoke(float timeScale = 8f, float maxRealtimeSeconds = 90f)
        {
// csharp-guardrails: allow-null-defense
            if (_finalWaveClearRoutine != null)
                StopCoroutine(_finalWaveClearRoutine);

            var requestedTimeScale = timeScale > 0f ? timeScale : 12f;
            var requestedMaxRealtimeSeconds = maxRealtimeSeconds > 0f ? maxRealtimeSeconds : 70f;
            _finalWaveClearGameEnded = false;
            Debug.Log($"[McpSmoke] Final wave clear smoke started. timeScale={requestedTimeScale:F1}, maxRealtimeSeconds={requestedMaxRealtimeSeconds:F1}");
            _finalWaveClearRoutine = StartCoroutine(RunFinalWaveClearForMcpSmokeRoutine(
                Mathf.Clamp(requestedTimeScale, 1f, 20f),
                Mathf.Max(1f, requestedMaxRealtimeSeconds)));
        }

        private IEnumerator RunFinalWaveClearForMcpSmokeRoutine(float timeScale, float maxRealtimeSeconds)
        {
            var previousTimeScale = Time.timeScale;
            var elapsed = 0f;
            Time.timeScale = timeScale;

            while (elapsed < maxRealtimeSeconds && !_finalWaveClearGameEnded)
            {
                var cleared = ClearSpawnedEnemiesForMcpSmoke();
                if (cleared > 0)
                    Debug.Log($"[McpSmoke] Cleared spawned enemies: {cleared}");

                yield return new WaitForSecondsRealtime(0.25f);
                elapsed += 0.25f;
            }

            Time.timeScale = previousTimeScale;
            Debug.Log("[McpSmoke] Final wave clear smoke finished.");
            _finalWaveClearRoutine = null;
        }

        private int ClearSpawnedEnemiesForMcpSmoke()
        {
// csharp-guardrails: allow-null-defense
            if (_combatSetup == null)
                return 0;

            var attackerId = ResolveAttackerId();
            var cleared = 0;

            _enemyIdBuffer.Clear();
            foreach (var enemyId in _activeEnemyIds)
                _enemyIdBuffer.Add(enemyId);

            for (var i = 0; i < _enemyIdBuffer.Count; i++)
            {
                var result = _combatSetup.ApplyDamage(
                    _enemyIdBuffer[i],
                    100000f,
                    DamageType.Physical,
                    attackerId);
                if (result.IsSuccess)
                    cleared++;
            }

            return cleared;
        }

        private void OnEnemySpawned(EnemySpawnedEvent e)
        {
            if (!string.IsNullOrWhiteSpace(e.EnemyId.Value))
                _activeEnemyIds.Add(e.EnemyId);
        }

        private void OnEnemyDied(EnemyDiedEvent e)
        {
            if (!string.IsNullOrWhiteSpace(e.EnemyId.Value))
                _activeEnemyIds.Remove(e.EnemyId);
        }

        private DomainEntityId ResolveAttackerId()
        {
// csharp-guardrails: allow-null-defense
            if (_playerSceneRegistry != null)
            {
                foreach (var playerSetup in _playerSceneRegistry.All)
                {
// csharp-guardrails: allow-null-defense
                    if (playerSetup != null && playerSetup.NetworkAdapter != null && playerSetup.NetworkAdapter.IsMine)
                        return playerSetup.PlayerId;
                }
            }

            return new DomainEntityId("mcp-smoke");
        }

        private void OnDestroy()
        {
            // csharp-guardrails: allow-null-defense
            _eventBus?.UnsubscribeAll(this);

            // csharp-guardrails: allow-null-defense
            if (_finalWaveClearRoutine != null)
            {
                StopCoroutine(_finalWaveClearRoutine);
                _finalWaveClearRoutine = null;
            }

            Time.timeScale = 1f;
        }
    }
}
#else
using UnityEngine;

namespace Features.Player
{
    public sealed class BattleSceneMcpSmokeDriver : MonoBehaviour
    {
    }
}
#endif
