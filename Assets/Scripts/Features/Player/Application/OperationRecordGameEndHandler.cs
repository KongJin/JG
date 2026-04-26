using System;
using Features.Player.Application.Events;
using Shared.EventBus;

namespace Features.Player.Application
{
    public sealed class OperationRecordGameEndHandler : IDisposable
    {
        private readonly SaveOperationRecordUseCase _saveOperationRecord;
        private readonly OperationRecordFactory _factory;
        private readonly Func<long> _getUnixTimeMs;
        private readonly Action<string> _logWarning;
        private bool _hasRecordedGameEnd;

        public OperationRecordGameEndHandler(
            IEventSubscriber subscriber,
            SaveOperationRecordUseCase saveOperationRecord,
            OperationRecordFactory factory = null,
            Func<long> getUnixTimeMs = null,
            Action<string> logWarning = null)
        {
            _saveOperationRecord = saveOperationRecord;
            _factory = factory ?? new OperationRecordFactory();
            _getUnixTimeMs = getUnixTimeMs ?? GetCurrentUnixTimeMs;
            _logWarning = logWarning;
            subscriber?.Subscribe(this, new Action<GameEndReportRequestedEvent>(OnGameEndReport));
        }

        private void OnGameEndReport(GameEndReportRequestedEvent report)
        {
            if (_hasRecordedGameEnd)
                return;

            _hasRecordedGameEnd = true;
            var record = _factory.Build(report, _getUnixTimeMs());
            var result = _saveOperationRecord.Execute(record);
            if (result.IsFailure)
            {
                _logWarning?.Invoke($"[OperationRecord] Save failed: {result.Error}");
                return;
            }
        }

        public void Dispose()
        {
        }

        private static long GetCurrentUnixTimeMs()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
    }
}
