using Features.Player.Application.Events;
using Shared.EventBus;
using System;

namespace Features.Player.Application
{
    /// <summary>
    /// 게임 종료 이벤트 구독 및 결과 정리.
    /// </summary>
    public sealed class GameEndEventHandler : IDisposable
    {
        private bool _gameEnded;

        public GameEndEventHandler(IEventSubscriber subscriber)
        {
            subscriber.Subscribe(this, new Action<GameEndEvent>(OnGameEnd));
        }

        private void OnGameEnd(GameEndEvent e)
        {
            if (_gameEnded) return;
            _gameEnded = true;
            // 실제 로깅/정리는 Bootstrap이 GameEndReportRequestedEvent로 처리
        }

        public void Dispose()
        {
        }
    }
}
