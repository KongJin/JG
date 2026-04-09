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
        private Material _activeMaterial;

        /// <summary>
        /// 배치 영역을 기반으로 메쉬를 생성하고 Material을 설정한다.
        /// </summary>
        public void Initialize(PlacementArea area, Material validMaterial)
        {
            _area = area;
            _validMaterial = validMaterial;
            _activeMaterial = validMaterial;

            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();

            BuildQuadMesh();
            ApplyMaterial(_validMaterial);
        }

        /// <summary>
        /// 드래그 중 하이라이트 상태를 전환한다.
        /// </summary>
        public void SetHighlight(bool isValid)
        {
            if (_highlightMaterial != null)
            {
                ApplyMaterial(isValid ? _highlightMaterial : _validMaterial);
            }
            else
            {
                // 하이라이트 Material이 없으면 색상으로 피드백
                if (_meshRenderer != null && _meshRenderer.sharedMaterial != null)
                {
                    var mat = _meshRenderer.sharedMaterial;
                    var baseColor = isValid ? new Color(0f, 1f, 0f, 0.3f) : new Color(1f, 0f, 0f, 0.3f);
                    mat.color = baseColor;
                }
            }
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
            if (_meshRenderer != null && material != null)
            {
                _meshRenderer.sharedMaterial = material;
            }
        }

        private void OnValidate()
        {
            // 에디터에서 Inspector 변경 시 미리보기
            if (_meshFilter != null && _area != null)
            {
                RebuildMesh();
            }
        }
    }
}
