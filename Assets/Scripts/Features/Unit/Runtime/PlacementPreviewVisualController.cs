using UnityEngine;

namespace Features.Unit.Runtime
{
    internal sealed class PlacementPreviewVisualController
    {
        private const int SegmentCount = 80;
        private const float PreviewYOffset = 0.08f;
        private const float AnchorLineWidth = 0.04f;
        private const float AttackLineWidth = 0.028f;
        private const float SpawnLineWidth = 0.035f;
        private const float SpawnMarkerRadius = 0.16f;

        private readonly GameObject _root;
        private readonly LineRenderer _anchorRing;
        private readonly LineRenderer _attackRing;
        private readonly LineRenderer _spawnMarker;
        private readonly Material _anchorMaterial;
        private readonly Material _attackMaterial;
        private readonly Material _spawnMaterial;

        private PlacementPreviewVisualController(Transform parent)
        {
            _root = new GameObject("PlacementPreviewVisuals");
            _root.transform.SetParent(parent, worldPositionStays: false);
            _root.transform.localPosition = Vector3.zero;
            _root.transform.localRotation = Quaternion.identity;
            _root.transform.localScale = Vector3.one;
            _root.SetActive(false);

            _anchorMaterial = CreateLineMaterial(new Color(0.28f, 0.86f, 1f, 0.82f));
            _attackMaterial = CreateLineMaterial(new Color(1f, 0.78f, 0.26f, 0.72f));
            _spawnMaterial = CreateLineMaterial(new Color(0.92f, 1f, 1f, 0.95f));

            _anchorRing = CreateRing("AnchorRadius", _anchorMaterial, AnchorLineWidth);
            _attackRing = CreateRing("AttackRange", _attackMaterial, AttackLineWidth);
            _spawnMarker = CreateRing("ExpectedSpawnPoint", _spawnMaterial, SpawnLineWidth);
        }

        public static PlacementPreviewVisualController Attach(Transform parent)
        {
            return parent == null ? null : new PlacementPreviewVisualController(parent);
        }

        public void Show(Vector3 worldPosition, float anchorRadius, float attackRange)
        {
            if (_root == null)
                return;

            _root.SetActive(true);
            DrawRing(_anchorRing, worldPosition, anchorRadius);
            DrawRing(_attackRing, worldPosition, attackRange);
            DrawRing(_spawnMarker, worldPosition, SpawnMarkerRadius);
        }

        public void Hide()
        {
            if (_root != null)
                _root.SetActive(false);
        }

        public void Dispose()
        {
            DestroyObject(_root);
            DestroyObject(_anchorMaterial);
            DestroyObject(_attackMaterial);
            DestroyObject(_spawnMaterial);
        }

        private LineRenderer CreateRing(string name, Material material, float width)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_root.transform, worldPositionStays: false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            var line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.loop = true;
            line.useWorldSpace = true;
            line.positionCount = SegmentCount;
            line.startWidth = width;
            line.endWidth = width;
            line.numCornerVertices = 4;
            line.numCapVertices = 4;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            return line;
        }

        private static void DrawRing(LineRenderer line, Vector3 center, float radius)
        {
            if (line == null)
                return;

            var visible = radius > 0f;
            line.enabled = visible;
            if (!visible)
                return;

            var y = center.y + PreviewYOffset;
            for (var i = 0; i < SegmentCount; i++)
            {
                var angle = (Mathf.PI * 2f * i) / SegmentCount;
                var point = new Vector3(
                    center.x + Mathf.Cos(angle) * radius,
                    y,
                    center.z + Mathf.Sin(angle) * radius);
                line.SetPosition(i, point);
            }
        }

        private static Material CreateLineMaterial(Color color)
        {
            var material = new Material(ResolveLineShader());
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            return material;
        }

        private static Shader ResolveLineShader()
        {
            return Shader.Find("Sprites/Default")
                ?? Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Unlit/Color")
                ?? Shader.Find("Standard");
        }

        private static void DestroyObject(Object target)
        {
            if (target == null)
                return;

            if (global::UnityEngine.Application.isPlaying)
            {
                Object.Destroy(target);
                return;
            }

            Object.DestroyImmediate(target);
        }
    }
}
