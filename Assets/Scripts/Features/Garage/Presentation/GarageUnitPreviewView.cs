using UnityEngine;
using UnityEngine.UI;
using Features.Garage.Domain;
using Features.Garage.Runtime;
using Features.Garage.Presentation.Theme;
using Shared.Attributes;
using TMPro;

namespace Features.Garage.Presentation
{
    /// <summary>
    /// Garage에서 선택된 유닛의 3D 미리보기를 표시합니다.
    /// 현재는 기본 도형(Cube/Cylinder/Cone)으로 플레이스홀더 역할을 하며,
    /// 추후 FBX 모델로 교체할 수 있는 구조입니다.
    /// </summary>
    public sealed class GarageUnitPreviewView : MonoBehaviour
    {
        [Header("Viewport")]
        [Required, SerializeField] private Camera _previewCamera;
        [SerializeField] private RenderTexture _renderTexture;
        [Required, SerializeField] private RawImage _rawImage;
        [Required, SerializeField] private TMP_Text _emptyStateText;

        [Header("Part Prefabs (Basic Shapes)")]
        [Tooltip("프레임: 직육면체")]
        [Required, SerializeField] private GameObject _framePrefab;
        [Tooltip("무기: 원기둥")]
        [Required, SerializeField] private GameObject _weaponPrefab;
        [Tooltip("기동: 원뿔")]
        [Required, SerializeField] private GameObject _thrusterPrefab;

        [Header("Rotation")]
        [SerializeField] private float _autoRotationSpeed = 20f;
        [SerializeField] private float _dragRotationMultiplier = 2f;

        private GameObject _currentPreviewRoot;
        private bool _isDragging;
        private Vector2 _lastMousePosition;
        private float _manualRotationY;

        public void Initialize()
        {
            EnsureRenderTexture();
            _previewCamera.backgroundColor = ThemeColors.PreviewBackground;
            _previewCamera.clearFlags = CameraClearFlags.SolidColor;
            _rawImage.color = Color.Lerp(ThemeColors.PreviewBackground, ThemeColors.BackgroundCard, 0.18f);
            _emptyStateText.text = "저장 유닛 실루엣";
            _emptyStateText.color = ThemeColors.TextMuted;

            SetEmptyStateVisible(true);
        }

        public void Render(GarageSlotViewModel viewModel, GaragePanelCatalog catalog)
        {
            EnsureRenderTexture();
            DestroyCurrentPreview();
            SetEmptyStateVisible(true);

            bool hasPreviewLoadout =
                viewModel != null &&
                !string.IsNullOrWhiteSpace(viewModel.FrameId) &&
                !string.IsNullOrWhiteSpace(viewModel.FirepowerId) &&
                !string.IsNullOrWhiteSpace(viewModel.MobilityId);

            if (!hasPreviewLoadout)
            {
                _rawImage.color = Color.Lerp(ThemeColors.PreviewBackground, ThemeColors.BackgroundCard, 0.18f);
                return;
            }

            CreatePreview(viewModel, catalog);
            SetEmptyStateVisible(false);
            _rawImage.color = ThemeColors.PreviewBackground;

            if (_previewCamera.targetTexture != null)
                _previewCamera.Render();
        }

        private void CreatePreview(GarageSlotViewModel viewModel, GaragePanelCatalog catalog)
        {
            _currentPreviewRoot = new GameObject("PreviewRoot");
            GaragePreviewAssembler.Attach(_currentPreviewRoot, transform, new Vector3(0f, -0.04f, 0f), Vector3.zero);

            // 프레임 (중심)
            var frameObj = CreateFrame(viewModel.FrameId);
            GaragePreviewAssembler.Attach(frameObj, _currentPreviewRoot.transform, Vector3.zero, Vector3.zero);

            // 무기 (상단)
            var weaponObj = CreateWeapon(viewModel.FirepowerId);
            GaragePreviewAssembler.Attach(weaponObj, _currentPreviewRoot.transform, new Vector3(0f, 0.62f, 0f), new Vector3(0f, 0f, 90f));

            // 기동 (하단)
            var thrusterObj = CreateThruster(viewModel.MobilityId);
            GaragePreviewAssembler.Attach(thrusterObj, _currentPreviewRoot.transform, new Vector3(0f, -0.58f, 0f), Vector3.zero);
        }

        private GameObject CreateFrame(string frameId)
        {
            var obj = Instantiate(_framePrefab);
            obj.SetActive(true);
            return obj;
        }

        private GameObject CreateWeapon(string firepowerId)
        {
            var obj = Instantiate(_weaponPrefab);
            obj.SetActive(true);
            return obj;
        }

        private GameObject CreateThruster(string mobilityId)
        {
            var obj = Instantiate(_thrusterPrefab);
            obj.SetActive(true);
            return obj;
        }

        private Color GetFrameColor(string frameId)
        {
            return frameId switch
            {
                "frame_striker" => ThemeColors.PreviewFrameStriker,
                "frame_bastion" => ThemeColors.PreviewFrameBastion,
                "frame_relay" => ThemeColors.PreviewFrameRelay,
                _ => Color.white
            };
        }

        private Color GetWeaponColor(string firepowerId)
        {
            return firepowerId switch
            {
                "fire_scatter" => ThemeColors.PreviewFireScatter,
                "fire_pulse" => ThemeColors.PreviewFirePulse,
                "fire_rail" => ThemeColors.PreviewFireRail,
                _ => Color.white
            };
        }

        private Color GetThrusterColor(string mobilityId)
        {
            return mobilityId switch
            {
                "mob_treads" => ThemeColors.PreviewMobTreads,
                "mob_vector" => Color.white,
                "mob_burst" => ThemeColors.PreviewMobBurst,
                _ => Color.white
            };
        }

        private void DestroyCurrentPreview()
        {
            if (_currentPreviewRoot != null)
                Destroy(_currentPreviewRoot);
        }

        private void SetEmptyStateVisible(bool isVisible)
        {
            _emptyStateText.gameObject.SetActive(isVisible);
        }

        private void EnsureRenderTexture()
        {
            if (_renderTexture == null)
            {
                _renderTexture = new RenderTexture(256, 256, 16)
                {
                    antiAliasing = 2,
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
                _renderTexture.Create();
            }

            if (_previewCamera.targetTexture != _renderTexture)
                _previewCamera.targetTexture = _renderTexture;

            if (_rawImage.texture != _renderTexture)
                _rawImage.texture = _renderTexture;
        }

        private void Update()
        {
            if (_currentPreviewRoot == null) return;
        }

        private void OnDestroy()
        {
            DestroyCurrentPreview();
            if (_renderTexture != null)
            {
                _renderTexture.Release();
                Destroy(_renderTexture);
            }
        }
    }
}
