namespace Features.Wave.Application
{
    public sealed class SelectionTimer
    {
        private float _remaining;
        private bool _running;

        public float Remaining => _remaining;
        public bool IsRunning => _running;

        public void Start(float duration)
        {
            _remaining = duration;
            _running = true;
        }

        public void Stop()
        {
            _running = false;
        }

        public bool Tick(float deltaTime)
        {
            if (!_running) return false;
            _remaining -= deltaTime;
            if (_remaining <= 0f)
            {
                _remaining = 0f;
                _running = false;
                return true;
            }
            return false;
        }
    }
}
