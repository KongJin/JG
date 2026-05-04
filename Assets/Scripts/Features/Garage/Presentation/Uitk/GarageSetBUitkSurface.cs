using System;

namespace Features.Garage.Presentation
{
    internal abstract class GarageSetBUitkSurface : IDisposable
    {
        protected bool IsDisposed { get; private set; }

        public void Dispose()
        {
            if (IsDisposed)
                return;

            IsDisposed = true;
            DisposeSurface();
        }

        protected abstract void DisposeSurface();
    }
}
