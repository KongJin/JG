namespace Features.Player.Application
{
    /// <summary>
    /// 게임 진행 시간에 따라 증가하는 Energy 재생 곡선.
    /// </summary>
    public sealed class EnergyRegenCurve
    {
        private readonly float _baseRegenRate;
        private readonly float _rampStartTime;
        private readonly float _rampDuration;
        private readonly float _maxRegenRate;

        public EnergyRegenCurve(
            float baseRegenRate,
            float rampStartTime,
            float rampDuration,
            float maxRegenRate)
        {
            _baseRegenRate = baseRegenRate;
            _rampStartTime = rampStartTime;
            _rampDuration = rampDuration;
            _maxRegenRate = maxRegenRate;
        }

        public float GetRegenRate(float elapsedSeconds)
        {
            if (elapsedSeconds <= _rampStartTime)
                return _baseRegenRate;

            var rampElapsed = elapsedSeconds - _rampStartTime;
            if (rampElapsed >= _rampDuration)
                return _maxRegenRate;

            if (_rampDuration <= 0f)
                return _maxRegenRate;

            var t = rampElapsed / _rampDuration;
            return _baseRegenRate + ((_maxRegenRate - _baseRegenRate) * t);
        }
    }
}
