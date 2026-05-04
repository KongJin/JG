using System;
using System.Collections.Generic;

namespace Shared.Ui
{
    /// <summary>
    /// UI Surface 클래스의 공통 기능을 제공하는 추상 클래스.
    /// GarageSetBSlotSurface, GarageSetBPartListSurface의 중복 패턴 제거.
    /// </summary>
    /// <typeparam name="TBinding">바인딩 타입</typeparam>
    public abstract class BaseSurface<TBinding> : IDisposable
        where TBinding : class
    {
        private readonly List<IDisposable> _disposables = new();
        private bool _isDisposed;

        /// <summary>
        /// 리소스 정리
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            OnDisposing();

            foreach (var disposable in _disposables)
            {
                disposable?.Dispose();
            }
            _disposables.Clear();
        }

        /// <summary>
        /// Disposable 등록
        /// </summary>
        protected void RegisterDisposable(IDisposable disposable)
        {
            if (disposable != null && !_isDisposed)
            {
                _disposables.Add(disposable);
            }
        }

        /// <summary>
        /// 콜백 등록 헬퍼 (Action)
        /// </summary>
        protected void RegisterCallback<T>(ref Action<T> field, Action<T> callback, Action<T> eventSource, Action<Action<T>> addHandler, Action<Action<T>> removeHandler)
        {
            UnregisterCallback(ref field, eventSource, removeHandler);
            field = callback;
            addHandler?.Invoke(callback);
        }

        /// <summary>
        /// 콜백 등록 헬퍼 (Action 무인자)
        /// </summary>
        protected void RegisterCallback(ref Action field, Action callback, Action eventSource, Action<Action> addHandler, Action<Action> removeHandler)
        {
            UnregisterCallback(ref field, eventSource, removeHandler);
            field = callback;
            addHandler?.Invoke(callback);
        }

        /// <summary>
        /// 콜백 해제 헬퍼 (Action)
        /// </summary>
        protected void UnregisterCallback<T>(ref Action<T> field, Action<T> eventSource, Action<Action<T>> removeHandler)
        {
            if (field != null && removeHandler != null)
            {
                removeHandler.Invoke(field);
            }
            field = null;
        }

        /// <summary>
        /// 콜백 해제 헬퍼 (Action 무인자)
        /// </summary>
        protected void UnregisterCallback(ref Action field, Action eventSource, Action<Action> removeHandler)
        {
            if (field != null && removeHandler != null)
            {
                removeHandler.Invoke(field);
            }
            field = null;
        }

        /// <summary>
        /// 버튼 콜백 간편 등록
        /// </summary>
        protected void RegisterButtonCallback(UnityEngine.UIElements.Button button, Action callback)
        {
            if (button == null || _isDisposed)
                return;

            var wrappedCallback = callback ?? throw new ArgumentNullException(nameof(callback));
            button.clicked += wrappedCallback;
            RegisterDisposable(new ActionDisposable(() => button.clicked -= wrappedCallback));
        }

        /// <summary>
        /// 파생 클래스에서 추가 정리 작업
        /// </summary>
        protected virtual void OnDisposing() { }

        /// <summary>
        /// Dispose 여부
        /// </summary>
        protected bool IsDisposed => _isDisposed;

        /// <summary>
        /// Action을 래핑한 간단한 IDisposable
        /// </summary>
        private sealed class ActionDisposable : IDisposable
        {
            private readonly Action _disposeAction;
            private bool _isDisposed;

            public ActionDisposable(Action disposeAction)
            {
                _disposeAction = disposeAction ?? throw new ArgumentNullException(nameof(disposeAction));
            }

            public void Dispose()
            {
                if (_isDisposed)
                    return;

                _isDisposed = true;
                _disposeAction?.Invoke();
            }
        }
    }
}
