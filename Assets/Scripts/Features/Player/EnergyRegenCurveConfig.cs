using Features.Player.Application;
using UnityEngine;

namespace Features.Player
{
    /// <summary>
    /// Energy 재생 곡선을 inspector에서 설정하기 위한 authoring config.
    /// </summary>
    [System.Serializable]
    public sealed class EnergyRegenCurveConfig
    {
        [Header("Base Regen")]
        [Tooltip("초기 재생량 (초당).")]
        [SerializeField] private float _baseRegenRate = 3f;

        [Header("Ramp")]
        [Tooltip("재생량 증가 시작 시간 (초).")]
        [SerializeField] private float _rampStartTime = 60f;

        [Tooltip("재생량 증가 종료 시간 (초, 시작 기준 상대값).")]
        [SerializeField] private float _rampDuration = 120f;

        [Tooltip("최대 재생량 (초당).")]
        [SerializeField] private float _maxRegenRate = 5f;

        public EnergyRegenCurve ToCurve()
        {
            return new EnergyRegenCurve(
                _baseRegenRate,
                _rampStartTime,
                _rampDuration,
                _maxRegenRate
            );
        }
    }
}
