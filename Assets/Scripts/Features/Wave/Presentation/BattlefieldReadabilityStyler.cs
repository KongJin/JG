using UnityEngine;

namespace Features.Wave.Presentation
{
    public sealed class BattlefieldReadabilityStyler : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private Renderer _groundRenderer;
        [SerializeField] private Renderer _laneRenderer;
        [SerializeField] private Renderer _coreRenderer;
        [SerializeField] private Renderer _rearSafeZoneRenderer;
        [SerializeField] private Transform _coreVisual;

        [Header("Palette")]
        [SerializeField] private Color _groundColor = new(0.27f, 0.32f, 0.39f, 1f);
        [SerializeField] private Color _laneColor = new(0.12f, 0.46f, 0.57f, 1f);
        [SerializeField] private Color _coreColor = new(0.35f, 0.79f, 1f, 1f);
        [SerializeField] private Color _rearSafeZoneColor = new(0.15f, 0.20f, 0.28f, 0.92f);

        [Header("Silhouette")]
        [SerializeField] private Vector3 _coreScale = new(1.3f, 1.9f, 1.3f);

        private void Awake()
        {
            Apply();
        }

        public void Apply()
        {
            AutoBindIfNeeded();

            ApplyColor(_groundRenderer, _groundColor);
            ApplyColor(_laneRenderer, _laneColor);
            ApplyColor(_coreRenderer, _coreColor);
            ApplyColor(_rearSafeZoneRenderer, _rearSafeZoneColor);

            if (_coreVisual != null)
            {
                _coreVisual.localScale = _coreScale;
            }
        }

        private void AutoBindIfNeeded()
        {
            if (_groundRenderer == null)
            {
                _groundRenderer = FindRenderer("ArenaGround");
            }

            if (_laneRenderer == null)
            {
                _laneRenderer = FindRenderer("LaneStripe");
            }

            if (_coreRenderer == null)
            {
                _coreRenderer = FindRenderer("ObjectiveCore");
            }

            if (_rearSafeZoneRenderer == null)
            {
                _rearSafeZoneRenderer = FindRenderer("RearSafeZone");
            }

            if (_coreVisual == null && _coreRenderer != null)
            {
                _coreVisual = _coreRenderer.transform;
            }
        }

        private Renderer FindRenderer(string childName)
        {
            var child = transform.Find(childName);
            return child != null ? child.GetComponent<Renderer>() : null;
        }

        private static void ApplyColor(Renderer targetRenderer, Color color)
        {
            if (targetRenderer == null || targetRenderer.sharedMaterial == null)
                return;

            var styledMaterial = new Material(targetRenderer.sharedMaterial);
            var colorPropertyId = Shader.PropertyToID("_Color");
            var baseColorPropertyId = Shader.PropertyToID("_BaseColor");

            if (styledMaterial.HasProperty(baseColorPropertyId))
            {
                styledMaterial.SetColor(baseColorPropertyId, color);
            }

            if (styledMaterial.HasProperty(colorPropertyId))
            {
                styledMaterial.SetColor(colorPropertyId, color);
            }

            targetRenderer.sharedMaterial = styledMaterial;
        }
    }
}
