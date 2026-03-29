using System.Collections;
using Features.Combat;
using Features.Enemy;
using Features.Wave.Application;
using Features.Wave.Application.Ports;
using Features.Wave.Infrastructure;
using Features.Wave.Presentation;
using Photon.Pun;
using Shared.EventBus;
using Shared.Lifecycle;
using UnityEngine;

namespace Features.Wave
{
    public sealed class WaveBootstrap : MonoBehaviour
    {
        [SerializeField] private WaveTableData _waveTable;
        [SerializeField] private EnemySpawnAdapter _spawnAdapter;
        [SerializeField] private PlayerPositionQueryAdapter _playerPositionQuery;
        [SerializeField] private WaveHudView _hudView;
        [SerializeField] private WaveEndView _endView;

        private EventBus _eventBus;
        private CombatBootstrap _combatBootstrap;
        private WaveLoopUseCase _waveLoop;
        private WaveEventHandler _waveHandler;
        private DisposableScope _disposables;

        public IPlayerPositionQuery PlayerPositionQuery => _playerPositionQuery;

        public void Initialize(EventBus eventBus, CombatBootstrap combatBootstrap)
        {
            if (eventBus == null)
            {
                Debug.LogError("[WaveBootstrap] EventBus is not provided.", this);
                return;
            }

            if (combatBootstrap == null)
            {
                Debug.LogError("[WaveBootstrap] CombatBootstrap is not assigned.", this);
                return;
            }

            _eventBus = eventBus;
            _combatBootstrap = combatBootstrap;

            EnsureDependencies();

            if (_waveTable == null)
                _waveTable = Resources.Load<WaveTableData>("Wave/DefaultWaveTable");

            if (_waveTable == null)
            {
                Debug.LogError("[WaveBootstrap] WaveTableData is not assigned.", this);
                return;
            }

            if (_waveTable.Waves == null || _waveTable.Waves.Length == 0)
            {
                Debug.LogError("[WaveBootstrap] WaveTableData has no waves configured.", this);
                return;
            }

            _disposables?.Dispose();
            _disposables = new DisposableScope();

            var playerCount = PhotonNetwork.PlayerList.Length;
            var aliveQuery = new AlivePlayerQueryAdapter(eventBus, playerCount);
            _disposables.Add(EventBusSubscription.ForOwner(eventBus, aliveQuery));

            _spawnAdapter.Initialize(eventBus, combatBootstrap, _playerPositionQuery);

            _waveLoop = new WaveLoopUseCase(eventBus, _waveTable.Waves.Length);
            _waveHandler = new WaveEventHandler(eventBus, _waveLoop, aliveQuery);
            _disposables.Add(EventBusSubscription.ForOwner(eventBus, _waveHandler));

            if (_hudView == null)
                _hudView = WaveHudView.CreateDefault();
            _hudView.Initialize(eventBus);
            _disposables.Add(EventBusSubscription.ForOwner(eventBus, _hudView));

            if (_endView == null)
                _endView = WaveEndView.CreateDefault();
            _endView.Initialize(eventBus);
            _disposables.Add(EventBusSubscription.ForOwner(eventBus, _endView));

            StartCountdown();
        }

        public void RegisterPlayer(Transform playerTransform)
        {
            if (_playerPositionQuery != null)
                _playerPositionQuery.RegisterPlayer(playerTransform);
        }

        private void StartCountdown()
        {
            if (_waveLoop == null)
                return;

            var waveIndex = _waveLoop.CurrentWaveIndex;
            if (waveIndex >= _waveTable.Waves.Length) return;

            var entry = _waveTable.Waves[waveIndex];
            if (entry == null)
            {
                Debug.LogError($"[WaveBootstrap] Wave entry at index {waveIndex} is null.", this);
                return;
            }

            _waveLoop.BeginCountdown(entry.CountdownDuration);
        }

        private void Update()
        {
            if (_waveLoop == null) return;

            InitializePendingEnemies();

            if (_waveLoop.CurrentState == Domain.WaveState.Countdown)
            {
                var finished = _waveLoop.TickCountdown(Time.deltaTime);
                if (_hudView != null)
                    _hudView.UpdateCountdown(_waveLoop.CountdownRemaining);

                if (finished)
                    BeginWave();
            }
            else if (_waveLoop.CurrentState == Domain.WaveState.Cleared)
            {
                StartCountdown();
            }
        }

        private void BeginWave()
        {
            var waveIndex = _waveLoop.CurrentWaveIndex;
            if (waveIndex >= _waveTable.Waves.Length) return;

            var entry = _waveTable.Waves[waveIndex];
            if (entry == null)
            {
                Debug.LogError($"[WaveBootstrap] Wave entry at index {waveIndex} is null.", this);
                return;
            }

            if (entry.EnemyData == null)
            {
                Debug.LogError($"[WaveBootstrap] EnemyData is missing for wave {waveIndex + 1}.", this);
                return;
            }

            _waveLoop.BeginWave(entry.Count);

            if (PhotonNetwork.IsMasterClient)
                StartCoroutine(SpawnWaveEnemies(entry));
        }

        private IEnumerator SpawnWaveEnemies(WaveTableData.WaveEntry entry)
        {
            _spawnAdapter.SetCurrentEnemyData(entry.EnemyData);

            for (var i = 0; i < entry.Count; i++)
            {
                var angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                var x = Mathf.Cos(angle) * entry.SpawnRadius;
                var z = Mathf.Sin(angle) * entry.SpawnRadius;

                _spawnAdapter.SpawnEnemy(entry.EnemyData.PrefabName, x, 0.75f, z);

                if (entry.SpawnDelay > 0f)
                    yield return new WaitForSeconds(entry.SpawnDelay);
            }
        }

        private void EnsureDependencies()
        {
            if (_spawnAdapter == null)
                _spawnAdapter = GetComponent<EnemySpawnAdapter>() ?? gameObject.AddComponent<EnemySpawnAdapter>();

            if (_playerPositionQuery == null)
                _playerPositionQuery = GetComponent<PlayerPositionQueryAdapter>() ?? gameObject.AddComponent<PlayerPositionQueryAdapter>();
        }

        private void InitializePendingEnemies()
        {
            var enemySetups = FindObjectsByType<EnemySetup>(FindObjectsSortMode.None);
            for (var i = 0; i < enemySetups.Length; i++)
            {
                var enemy = enemySetups[i];
                if (enemy == null || enemy.IsInitialized)
                    continue;

                enemy.Initialize(_eventBus, _combatBootstrap, _playerPositionQuery);
            }
        }

        private void OnDestroy()
        {
            _disposables?.Dispose();
        }
    }
}
