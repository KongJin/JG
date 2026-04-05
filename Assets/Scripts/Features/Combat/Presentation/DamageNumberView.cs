using Shared.Attributes;
using UnityEngine;
using UnityEngine.UI;

namespace Features.Combat.Presentation
{
    public sealed class DamageNumberView : MonoBehaviour
    {
        [Required, SerializeField] private Text damageText;
        [Required, SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float floatSpeed = 1.5f;
        [SerializeField] private float lifetime = 0.8f;
        [SerializeField] private float fadeStartRatio = 0.4f;

        private float _elapsed;

        public void Show(float damage)
        {
            damageText.text = Mathf.CeilToInt(damage).ToString();
            _elapsed = 0f;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            transform.position += Vector3.up * (floatSpeed * Time.deltaTime);

            var fadeProgress = _elapsed / lifetime;
            if (fadeProgress > fadeStartRatio)
            {
                var t = (fadeProgress - fadeStartRatio) / (1f - fadeStartRatio);
                canvasGroup.alpha = 1f - t;
            }

            if (_elapsed >= lifetime)
                Destroy(gameObject);
        }

        private void LateUpdate()
        {
            var cam = Camera.main;
            if (cam != null)
                transform.forward = cam.transform.forward;
        }
    }
}
