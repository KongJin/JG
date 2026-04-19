using UnityEngine;
using UnityEngine.UI;
using Features.Garage.Domain;
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
            _rawImage.color = ThemeColors.PreviewBackground;
            _emptyStateText.text = "선택한 빌드 미리보기가 여기에 표시됩니다.";
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
                return;

            CreatePreview(viewModel, catalog);
            SetEmptyStateVisible(false);

            if (_previewCamera.targetTexture != null)
                _previewCamera.Render();
        }

        private void CreatePreview(GarageSlotViewModel viewModel, GaragePanelCatalog catalog)
        {
            _currentPreviewRoot = new GameObject("PreviewRoot");
            _currentPreviewRoot.transform.SetParent(transform, false);
            _currentPreviewRoot.transform.localPosition = new Vector3(0f, -0.02f, 0f);

            // 프레임 (중심)
            var frameObj = CreateFrame(viewModel.FrameId);
            frameObj.transform.SetParent(_currentPreviewRoot.transform, false);
            frameObj.transform.localPosition = Vector3.zero;

            // 무기 (상단)
            var weaponObj = CreateWeapon(viewModel.FirepowerId);
            weaponObj.transform.SetParent(_currentPreviewRoot.transform, false);
            weaponObj.transform.localPosition = new Vector3(0, 0.78f, 0);
            weaponObj.transform.localEulerAngles = new Vector3(0, 0, 90);

            // 기동 (하단)
            var thrusterObj = CreateThruster(viewModel.MobilityId);
            thrusterObj.transform.SetParent(_currentPreviewRoot.transform, false);
            thrusterObj.transform.localPosition = new Vector3(0, -0.72f, 0);
        }

        private GameObject CreateFrame(string frameId)
        {
            var obj = Instantiate(_framePrefab);
            obj.SetActive(true);
            obj.GetComponent<Renderer>().material.color = GetFrameColor(frameId);
            obj.transform.localScale = new Vector3(0.82f, 0.62f, 0.42f);
            return obj;
        }

        private GameObject CreateWeapon(string firepowerId)
        {
            var obj = Instantiate(_weaponPrefab);
            obj.SetActive(true);
            obj.GetComponent<Renderer>().material.color = GetWeaponColor(firepowerId);
            obj.transform.localScale = new Vector3(0.18f, 0.58f, 0.18f);
            return obj;
        }

        private GameObject CreateThruster(string mobilityId)
        {
            var obj = Instantiate(_thrusterPrefab);
            obj.SetActive(true);
            obj.GetComponent<Renderer>().material.color = GetThrusterColor(mobilityId);
            obj.transform.localScale = new Vector3(0.36f, 0.28f, 0.36f);
            obj.transform.localEulerAngles = new Vector3(180, 0, 0);
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

            // 마우스 드래그 감지 — Input System과 구버전 Input 모두 지원
            var mouseDown = false;
            var mouseUp = false;
            var mousePos = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            // Input System 패키지를 사용하는 경우
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse != null)
            {
                mouseDown = mouse.leftButton.wasPressedThisFrame;
                mouseUp = mouse.leftButton.wasReleasedThisFrame;
                mousePos = mouse.position.ReadValue();
            }
#else
            // 구버전 Input 시스템
            mouseDown = UnityEngine.Input.GetMouseButtonDown(0);
            mouseUp = UnityEngine.Input.GetMouseButtonUp(0);
            mousePos = UnityEngine.Input.mousePosition;
#endif

            if (mouseDown)
            {
                var ray = _previewCamera.ScreenPointToRay(mousePos);
                if (Physics.Raycast(ray, out var hit))
                {
                    if (hit.transform.IsChildOf(_currentPreviewRoot.transform))
                    {
                        _isDragging = true;
                        _lastMousePosition = mousePos;
                    }
                }
            }

            if (mouseUp)
                _isDragging = false;

            // 회전 처리
            if (_isDragging)
            {
                Vector2 delta = mousePos - _lastMousePosition;
                _manualRotationY += delta.x * _dragRotationMultiplier * Time.deltaTime;
                _lastMousePosition = mousePos;
            }
            else
            {
                _manualRotationY += _autoRotationSpeed * Time.deltaTime;
            }

            _currentPreviewRoot.transform.localEulerAngles = new Vector3(0, _manualRotationY, 0);
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
