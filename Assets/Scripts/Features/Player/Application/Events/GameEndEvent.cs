using Shared.Kernel;

namespace Features.Player.Application.Events
{
    /// <summary>
    /// 게임 종료 이벤트. Wave 승패 및 기본 통계를 전달한다.
    /// Player 피처가 게임 생명주기 이벤트의 소유자이며,
    /// WaveGameEndBridge가 Wave 이벤트를 이 이벤트로 변환하여 발행한다.
    /// </summary>
    public readonly struct GameEndEvent
    {
        public GameEndEvent(
            bool isVictory,
            string message,
            int reachedWave = 0,
            float playTimeSeconds = 0f,
            int summonCount = 0,
            int unitKillCount = 0
        )
        {
            IsVictory = isVictory;
            Message = message;
            ReachedWave = reachedWave;
            PlayTimeSeconds = playTimeSeconds;
            SummonCount = summonCount;
            UnitKillCount = unitKillCount;
        }

        /// <summary>승리 여부.</summary>
        public bool IsVictory { get; }

        /// <summary>결과 메시지 ("Victory!", "Defeat!" 등).</summary>
        public string Message { get; }

        /// <summary>도달한 웨이브 번호 (1-based).</summary>
        public int ReachedWave { get; }

        /// <summary>총 플레이 시간 (초).</summary>
        public float PlayTimeSeconds { get; }

        /// <summary>소환 횟수.</summary>
        public int SummonCount { get; }

        /// <summary>처치한 유닛/적 수.</summary>
        public int UnitKillCount { get; }
    }
}
