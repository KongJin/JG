using System.Collections;
using Features.Combat;
using Features.Combat.Domain;
using Features.Wave;
using Features.Wave.Application.Events;
using Shared.Attributes;
using Shared.EventBus;
using Shared.Kernel;
using UnityEngine;

namespace Features.Player
{
    public sealed class GameSceneMcpSmokeDriver : MonoBehaviour, IGameSceneEventBusConsumer
    {
        [Required, SerializeField] private CombatSetup _combatSetup;
        [SerializeField] private CoreObjectiveSetup _coreObjective;
        [Required, SerializeField] private PlayerSceneRegistry _playerSceneRegistry;

        private EventBus _eventBus;
        private Coroutine _finalWaveClearRoutine;
        private bool _finalWaveClearGameEnded;

        public void Initialize(EventBus eventBus)
        {
            _eventBus = eventBus;
            _eventBus?.Subscribe(this, new System.Action<Features.Player.Application.Events.GameEndReportRequestedEvent>(_ =>
            {
                _finalWaveClearGameEnded = true;
            }));
        }

        public void ForceCoreDefeatForMcpSmoke()
        {
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
            _eventBus?.Publish(new WaveVictoryEvent());
        }

        public void RunFinalWaveClearForMcpSmoke(float timeScale = 8f, float maxRealtimeSeconds = 90f)
        {
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
            if (_combatSetup == null)
                return 0;

            var holders = FindObjectsByType<EntityIdHolder>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            var attackerId = ResolveAttackerId();
            var cleared = 0;

            foreach (var holder in holders)
            {
                if (holder == null || !holder.IsInitialized)
                    continue;
                if (string.IsNullOrWhiteSpace(holder.Id.Value) || !holder.Id.Value.StartsWith("enemy-"))
                    continue;

                _combatSetup.ApplyDamage(
                    holder.Id,
                    100000f,
                    DamageType.Physical,
                    attackerId);
                cleared++;
            }

            return cleared;
        }

        private DomainEntityId ResolveAttackerId()
        {
            if (_playerSceneRegistry != null)
            {
                foreach (var player in _playerSceneRegistry.All)
                {
                    if (player != null && player.NetworkAdapter != null && player.NetworkAdapter.IsMine)
                        return player.PlayerId;
                }
            }

            return new DomainEntityId("mcp-smoke");
        }

        private void OnDestroy()
        {
            if (_finalWaveClearRoutine != null)
            {
                StopCoroutine(_finalWaveClearRoutine);
                _finalWaveClearRoutine = null;
            }

            Time.timeScale = 1f;
        }
    }
}
