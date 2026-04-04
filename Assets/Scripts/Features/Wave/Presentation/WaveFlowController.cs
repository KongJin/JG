using System;
using System.Text;
using Features.Wave.Application;
using Features.Wave.Application.Events;
using Features.Wave.Application.Ports;
using Features.Wave.Domain;
using Shared.EventBus;
using Shared.Kernel;
using UnityEngine;

namespace Features.Wave.Presentation
{
    public sealed class WaveFlowController : MonoBehaviour
    {
        private WaveLoopUseCase _waveLoop;
        private IWaveTablePort _waveTable;
        private IWaveSpawnPort _spawnPort;
        private IEventSubscriber _subscriber;
        private IEventPublisher _publisher;
        private DomainEntityId _localPlayerId;

        private readonly SelectionTimer _selectionTimer = new SelectionTimer();
        private RewardCandidate[] _cachedCandidates;

        public SelectionTimer SelectionTimer => _selectionTimer;

        public void Initialize(
            WaveLoopUseCase waveLoop,
            IWaveTablePort waveTable,
            IWaveSpawnPort spawnPort,
            IEventSubscriber subscriber,
            IEventPublisher publisher,
            DomainEntityId localPlayerId)
        {
            _waveLoop = waveLoop;
            _waveTable = waveTable;
            _spawnPort = spawnPort;
            _subscriber = subscriber;
            _publisher = publisher;
            _localPlayerId = localPlayerId;

            _subscriber.Subscribe(this, new Action<SkillSelectedEvent>(OnSkillSelected));
            _subscriber.Subscribe(this, new Action<GameStartEvent>(OnGameStart));
            _subscriber.Subscribe(this, new Action<SkillSelectionRequestedEvent>(OnSelectionRequested));
        }

        public void StartFirstWave()
        {
            StartCountdownForCurrentWave();
        }

        private void Update()
        {
            if (_waveLoop == null) return;

            if (_waveLoop.CurrentState == WaveState.Countdown)
            {
                var finished = _waveLoop.TickCountdown(Time.deltaTime);
                if (finished)
                    BeginCurrentWave();
            }
            else if (_waveLoop.CurrentState == WaveState.Cleared)
            {
                if (!_waveLoop.EnterUpgradeSelection())
                    StartCountdownForCurrentWave();
            }
            else if (_waveLoop.CurrentState == WaveState.UpgradeSelection)
            {
                if (_selectionTimer.Tick(Time.deltaTime))
                    AutoSelectFirst();
            }
        }

        private void OnGameStart(GameStartEvent e)
        {
            StartFirstWave();
        }

        private void OnSelectionRequested(SkillSelectionRequestedEvent e)
        {
            _cachedCandidates = e.Candidates;
            _selectionTimer.Start(e.SelectionDuration);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            LogRewardCandidatesOffered(e);
#endif
        }

        private void OnSkillSelected(SkillSelectedEvent e)
        {
            _selectionTimer.Stop();
            _cachedCandidates = null;

            if (_waveLoop.CurrentState != WaveState.UpgradeSelection) return;

            _waveLoop.ExitUpgradeSelection();
            StartCountdownForCurrentWave();
        }

        private void AutoSelectFirst()
        {
            if (_cachedCandidates == null || _cachedCandidates.Length == 0) return;

            var c = _cachedCandidates[0];
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log(
                $"[MvpReward] auto_select contextWaveIndex={_waveLoop.CurrentWaveIndex} " +
                $"index=0 type={c.Type} skillId={c.SkillId} axis={c.Axis}");
#endif
            _publisher.Publish(new SkillSelectedEvent(
                _localPlayerId, c.SkillId, c.DisplayName, c.Type, c.Axis));
        }

        private void StartCountdownForCurrentWave()
        {
            var waveIndex = _waveLoop.CurrentWaveIndex;
            if (waveIndex >= _waveTable.WaveCount) return;

            _waveLoop.BeginCountdown(_waveTable.GetCountdownDuration(waveIndex));
        }

        private void BeginCurrentWave()
        {
            var waveIndex = _waveLoop.CurrentWaveIndex;
            if (waveIndex >= _waveTable.WaveCount) return;

            _waveLoop.BeginWave(_waveTable.GetEnemyCount(waveIndex));
            _spawnPort.SpawnWave(waveIndex);
        }

        private void OnDestroy()
        {
            _subscriber?.UnsubscribeAll(this);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private static void LogRewardCandidatesOffered(SkillSelectionRequestedEvent e)
        {
            var sb = new StringBuilder();
            sb.Append("[MvpReward] candidates_offered waveIndex=").Append(e.WaveIndex).Append(" order=");
            for (var i = 0; i < e.Candidates.Length; i++)
            {
                var c = e.Candidates[i];
                if (i > 0) sb.Append(';');
                sb.Append(i).Append(':').Append(c.Type).Append(':').Append(c.SkillId);
            }

            Debug.Log(sb.ToString());
        }
#endif
    }
}
