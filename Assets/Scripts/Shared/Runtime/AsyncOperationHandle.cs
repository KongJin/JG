using System;
using System.Threading;
using System.Threading.Tasks;

namespace Shared.Runtime
{
    /// <summary>
    /// 비동기 작업 취소 및 상태 추적을 위한 핸들.
    /// UI 렌더링과 비동기 작업 동기화 개선.
    /// </summary>
    public sealed class AsyncOperationHandle : IDisposable
    {
        private readonly CancellationTokenSource _cts;
        private readonly string _operationName;
        private Task _task;
        private bool _isCompleted;
        private bool _isCancellationRequested;
        private bool _isDisposed;

        public AsyncOperationHandle(string operationName)
        {
            _operationName = operationName ?? "Operation";
            _cts = new CancellationTokenSource();
        }

        /// <summary>
        /// 취소 토큰
        /// </summary>
        public CancellationToken Token => _cts.Token;

        /// <summary>
        /// 작업 이름
        /// </summary>
        public string OperationName => _operationName;

        /// <summary>
        /// 작업 완료 여부
        /// </summary>
        public bool IsCompleted => _isCompleted || (_task?.IsCompleted ?? false);

        /// <summary>
        /// 작업 취소 요청 여부
        /// </summary>
        public bool IsCancellationRequested => _isCancellationRequested;

        /// <summary>
        /// 작업 중인지 여부
        /// </summary>
        public bool IsInProgress => !_isDisposed && !IsCompleted;

        /// <summary>
        /// 작업 설정
        /// </summary>
        public void SetTask(Task task)
        {
            _task = task;
        }

        public void Complete()
        {
            _isCompleted = true;
        }

        /// <summary>
        /// 작업 취소
        /// </summary>
        public void Cancel()
        {
            if (!_isDisposed && !_cts.IsCancellationRequested)
            {
                _isCancellationRequested = true;
                _cts.Cancel();
            }
        }

        /// <summary>
        /// 취소 가능한지 확인
        /// </summary>
        public bool CanCancel => !_isDisposed && IsInProgress && !_cts.IsCancellationRequested;

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            if (!_isCompleted && !_cts.IsCancellationRequested)
            {
                _isCancellationRequested = true;
                _cts.Cancel();
            }
            _cts.Dispose();
            _task = null;
        }
    }
}
