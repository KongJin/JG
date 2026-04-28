using UnityEngine;

namespace Features.Combat.Presentation
{
    public sealed class DamageNumberView : MonoBehaviour
    {
        [SerializeField] private float floatSpeed = 1.5f;
        [SerializeField] private float lifetime = 0.8f;

        private float _elapsed;

        public float Damage { get; private set; }

        public void Show(float damage)
        {
            Damage = damage;
            _elapsed = 0f;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            transform.position += Vector3.up * (floatSpeed * Time.deltaTime);

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
