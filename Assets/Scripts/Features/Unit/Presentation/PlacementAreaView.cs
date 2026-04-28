using Features.Unit.Runtime;
using Shared.Runtime;
using Shared.Runtime.Pooling;
using UnityEngine;

namespace Features.Unit.Presentation
{
    /// <summary>
    /// 배치 영역을 3D Quad로 시각화한다.
    /// CoreObjectiveBootstrap에서 런타임 생성하거나, 씬에 미리 배치하고 Inspector에서 연결한다.
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public sealed class PlacementAreaView : MonoBehaviour
    {
        [Header("Materials")]
        [Tooltip("유효한 배치 영역 표시용 반투명 Material.")]
        [SerializeField] private Material _validMaterial;

        [Tooltip("드래그 중 하이라이트용 Material (선택 사항).")]
        [SerializeField] private Material _highlightMaterial;

        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private PlacementArea _area;
        private Material _idleMaterial;
        private Material _activeMaterial;
        private Material _invalidMaterial;
        private bool _selectionActive;
        private Coroutine _invalidFeedbackRoutine;
        private PlacementPreviewVisualController _previewVisual;

        public bool HasUnitPreview { get; private set; }
        public Vector3 PreviewWorldPosition { get; private set; }
        public float PreviewAnchorRadius { get; private set; }
        public float PreviewAttackRange { get; private set; }

        /// <summary>
        /// 배치 영역을 기반으로 메쉬를 생성하고 Material을 설정한다.
        /// </summary>
        public void Initialize(PlacementArea area, Material validMaterial)
        {
            _area = area;
            _validMaterial = validMaterial;
            _idleMaterial = validMaterial != null
                ? validMaterial
                : PlacementAreaMaterialFactory.CreateIdleMaterial();
            _activeMaterial = _highlightMaterial != null
                ? _highlightMaterial
                : PlacementAreaMaterialFactory.CreateActiveMaterial();
            _invalidMaterial = PlacementAreaMaterialFactory.CreateInvalidMaterial();

            _meshFilter = ComponentAccess.Get<MeshFilter>(gameObject);
            _meshRenderer = ComponentAccess.Get<MeshRenderer>(gameObject);
            _previewVisual ??= PlacementPreviewVisualController.Attach(transform);

            BuildQuadMesh();
            ApplyMaterial(_idleMaterial);
        }

        /// <summary>
        /// 드래그 중 하이라이트 상태를 전환한다.
        /// </summary>
        public void SetHighlight(bool isValid)
        {
            ApplyMaterial(isValid ? _activeMaterial : _idleMaterial);
        }

        public void SetSelectionActive(bool active)
        {
            _selectionActive = active;
            ApplyMaterial(active ? _activeMaterial : _idleMaterial);
        }

        public void ShowUnitPreview(Vector3 worldPosition, float anchorRadius, float attackRange)
        {
            HasUnitPreview = true;
            PreviewWorldPosition = worldPosition;
            PreviewAnchorRadius = Mathf.Max(0f, anchorRadius);
            PreviewAttackRange = Mathf.Max(0f, attackRange);
            SetSelectionActive(true);
            _previewVisual?.Show(PreviewWorldPosition, PreviewAnchorRadius, PreviewAttackRange);
        }

        public void HideUnitPreview()
        {
            HasUnitPreview = false;
            PreviewWorldPosition = Vector3.zero;
            PreviewAnchorRadius = 0f;
            PreviewAttackRange = 0f;
            SetSelectionActive(false);
            _previewVisual?.Hide();
        }

        public void ShowInvalidPlacementFeedback()
        {
            if (!isActiveAndEnabled)
            {
                ApplyMaterial(_selectionActive ? _activeMaterial : _idleMaterial);
                return;
            }

            if (_invalidFeedbackRoutine != null)
            {
                StopCoroutine(_invalidFeedbackRoutine);
            }

            _invalidFeedbackRoutine = StartCoroutine(ShowInvalidFeedbackRoutine());
        }

        /// <summary>
        /// 배치 영역 메쉬를 갱신한다 (영역 크기 변경 시).
        /// </summary>
        public void RebuildMesh()
        {
            if (_area != null)
            {
                BuildQuadMesh();
            }
        }

        private void BuildQuadMesh()
        {
            if (_meshFilter == null || _area == null) return;

            var corners = _area.GetCorners();
            var mesh = new Mesh
            {
                name = "PlacementAreaQuad"
            };

            // 정점: 4개 (좌하단, 우하단, 우상단, 좌상단)
            mesh.vertices = new[]
            {
                corners[0], // 좌하단
                corners[1], // 우하단
                corners[2], // 우상단
                corners[3], // 좌상단
            };

            // 삼각형: 2개 (6개 인덱스)
            mesh.triangles = new[]
            {
                0, 1, 2,
                0, 2, 3
            };

            // UV 좌표
            mesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f)
            };

            mesh.RecalculateNormals();
            _meshFilter.mesh = mesh;
        }

        private void ApplyMaterial(Material material)
        {
            if (_meshRenderer == null)
            {
                _meshRenderer = ComponentAccess.Get<MeshRenderer>(gameObject);
            }

            if (_meshRenderer == null || material == null)
                return;

            PlacementAreaRendererMaterialApplier.Apply(_meshRenderer, material);
        }

        private System.Collections.IEnumerator ShowInvalidFeedbackRoutine()
        {
            ApplyMaterial(_invalidMaterial);
            yield return new WaitForSeconds(0.25f);
            ApplyMaterial(_selectionActive ? _activeMaterial : _idleMaterial);
            _invalidFeedbackRoutine = null;
        }

        private void OnValidate()
        {
            // 에디터에서 Inspector 변경 시 미리보기
            if (_meshFilter != null && _area != null)
            {
                RebuildMesh();
            }
        }

        private void OnDestroy()
        {
            _previewVisual?.Dispose();
            _previewVisual = null;
        }
    }
}
