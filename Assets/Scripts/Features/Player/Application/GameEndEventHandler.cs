using Features.Player.Application.Events;
using Shared.EventBus;
using System;
using UnityEngine;

namespace Features.Player.Application
{
    /// <summary>
    /// 게임 종료 이벤트 구독 및 결과 처리.
    /// GameEndEvent를 받아 Lobby 복귀 액션을 제공한다.
    /// </summary>
    public sealed class GameEndEventHandler : IDisposable
    {
        private readonly IEventPublisher _publisher;
        private bool _gameEnded;

        public GameEndEventHandler(
            IEventSubscriber subscriber,
            IEventPublisher publisher
        )
        {
            _publisher = publisher;
            subscriber.Subscribe(this, new Action<GameEndEvent>(OnGameEnd));
        }

        private void OnGameEnd(GameEndEvent e)
        {
            if (_gameEnded) return;
            _gameEnded = true;

            Debug.Log($"[GameEndEventHandler] Game ended: {(e.IsVictory ? "Victory" : "Defeat")} - {e.Message}");
            Debug.Log($"  Wave: {e.ReachedWave}, PlayTime: {e.PlayTimeSeconds:F1}s, Summons: {e.SummonCount}, Kills: {e.UnitKillCount}");

            // TODO: SceneLoaderPort를 통해 결과 화면 → Lobby 전환
            // 현재 WaveEndView가 패널 표시 + PhotonNetwork.LeaveRoom() 처리
            // PvP 모드에서는 이 핸들러가 직접 결과 UI를 띄울 수 있음
        }

        public void Dispose()
        {
        }
    }
}
