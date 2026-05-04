using System;

namespace Shared.Ui
{
    public abstract class BaseSurface<TBinding> : IDisposable
        where TBinding : class
    {
        private bool _isDisposed;

        protected BaseSurface(TBinding binding)
        {
            Binding = binding ?? throw new ArgumentNullException(nameof(binding));
        }

        protected TBinding Binding { get; }
        protected bool IsDisposed => _isDisposed;

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            DisposeSurface();
        }

        protected abstract void DisposeSurface();
    }
}
