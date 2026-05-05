using System;
using System.Collections.Generic;

namespace Shared.Lifecycle
{
    public sealed class DelegateDisposable : IDisposable
    {
        private Action _cleanup;

        public DelegateDisposable(Action cleanup)
        {
            _cleanup = cleanup;
        }

        public void Dispose()
        {
            var cleanup = _cleanup;
            // csharp-guardrails: allow-null-defense
            if (cleanup == null)
                return;

            _cleanup = null;
            cleanup();
        }
    }

    public sealed class DisposableScope : IDisposable
    {
        private readonly List<IDisposable> _disposables = new List<IDisposable>();
        private bool _isDisposed;

        public void Add(IDisposable disposable)
        {
            if (disposable == null)
                return;

            if (_isDisposed)
            {
                disposable.Dispose();
                return;
            }

            _disposables.Add(disposable);
        }

        public void Add(Action cleanup)
        {
            Add(new DelegateDisposable(cleanup));
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            for (var i = _disposables.Count - 1; i >= 0; i--)
                _disposables[i].Dispose();

            _disposables.Clear();
        }
    }
}
