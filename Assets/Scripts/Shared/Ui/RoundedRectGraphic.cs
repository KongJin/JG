using UnityEngine;

namespace Shared.Ui
{
    [DisallowMultipleComponent]
    public sealed class RoundedRectGraphic : MonoBehaviour
    {
        [SerializeField]
        private float _cornerRadius = 24f;

        [SerializeField]
        private float _borderThickness;

        public float CornerRadius => _cornerRadius;
        public float BorderThickness => _borderThickness;
    }
}
