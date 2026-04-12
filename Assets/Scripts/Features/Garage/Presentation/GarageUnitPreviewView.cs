using UnityEngine;
using UnityEngine.UI;
using Features.Garage.Domain;

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
        [SerializeField] private Camera _previewCamera;
        [SerializeField] private RenderTexture _renderTexture;
        [SerializeField] private RawImage _rawImage;

        [Header("Part Prefabs (Basic Shapes)")]
        [Tooltip("프레임: 직육면체")]
        [SerializeField] private GameObject _framePrefab;
        [Tooltip("무기: 원기둥")]
        [SerializeField] private GameObject _weaponPrefab;
        [Tooltip("기동: 원뿔")]
        [SerializeField] private GameObject _thrusterPrefab;

        [Header("Frame Colors")]
        [SerializeField] private Color _strikerColor = new(0.95f, 0.5f, 0.1f);   // 주황
        [SerializeField] private Color _bastionColor = new(0.2f, 0.4f, 0.9f);   // 파랑
        [SerializeField] private Color _relayColor = new(0.2f, 0.8f, 0.4f);     // 초록

        [Header("Rotation")]
        [SerializeField] private float _autoRotationSpeed = 20f;
        [SerializeField] private float _dragRotationMultiplier = 2f;

        private GameObject _currentPreviewRoot;
        private bool _isDragging;
        private Vector2 _lastMousePosition;
        private float _manualRotationY;

        public void Initialize()
        {
            if (_renderTexture == null && _previewCamera != null)
            {
                _renderTexture = new RenderTexture(256, 256, 16);
                _renderTexture.antiAliasing = 2;
                _previewCamera.targetTexture = _renderTexture;

                if (_rawImage != null)
                    _rawImage.texture = _renderTexture;
            }

            if (_previewCamera != null)
            {
                _previewCamera.backgroundColor = new Color(0.05f, 0.06f, 0.10f, 1f);
                _previewCamera.clearFlags = CameraClearFlags.SolidColor;
            }
        }

        public void Render(GarageSlotViewModel viewModel, GaragePanelCatalog catalog)
        {
            DestroyCurrentPreview();

            if (viewModel == null || !viewModel.HasCommittedLoadout)
                return;

            CreatePreview(viewModel, catalog);
        }

        private void CreatePreview(GarageSlotViewModel viewModel, GaragePanelCatalog catalog)
        {
            _currentPreviewRoot = new GameObject("PreviewRoot");
            _currentPreviewRoot.transform.SetParent(transform, false);
            _currentPreviewRoot.transform.localPosition = Vector3.zero;

            // 프레임 (중심)
            var frameObj = CreateFrame(viewModel.FrameId, catalog);
            frameObj.transform.SetParent(_currentPreviewRoot.transform, false);
            frameObj.transform.localPosition = Vector3.zero;

            // 무기 (상단)
            var weaponObj = CreateWeapon(viewModel.FirepowerId);
            weaponObj.transform.SetParent(_currentPreviewRoot.transform, false);
            weaponObj.transform.localPosition = new Vector3(0, 0.55f, 0);
            weaponObj.transform.localEulerAngles = new Vector3(0, 0, 90);

            // 기동 (하단)
            var thrusterObj = CreateThruster(viewModel.MobilityId);
            thrusterObj.transform.SetParent(_currentPreviewRoot.transform, false);
            thrusterObj.transform.localPosition = new Vector3(0, -0.5f, 0);
        }

        private GameObject CreateFrame(string frameId, GaragePanelCatalog catalog)
        {
            var obj = Instantiate(_framePrefab);
            var frame = catalog?.FindFrame(frameId);
            obj.GetComponent<Renderer>().material.color = GetFrameColor(frameId);
            obj.transform.localScale = new Vector3(0.5f, 0.4f, 0.3f);
            return obj;
        }

        private GameObject CreateWeapon(string firepowerId)
        {
            var obj = Instantiate(_weaponPrefab);
            obj.GetComponent<Renderer>().material.color = GetWeaponColor(firepowerId);
            obj.transform.localScale = new Vector3(0.12f, 0.35f, 0.12f);
            return obj;
        }

        private GameObject CreateThruster(string mobilityId)
        {
            var obj = Instantiate(_thrusterPrefab);
            obj.GetComponent<Renderer>().material.color = GetThrusterColor(mobilityId);
            obj.transform.localScale = new Vector3(0.25f, 0.2f, 0.25f);
            obj.transform.localEulerAngles = new Vector3(180, 0, 0);
            return obj;
        }

        private Color GetFrameColor(string frameId)
        {
            return frameId switch
            {
                "frame_striker" => _strikerColor,
                "frame_bastion" => _bastionColor,
                "frame_relay" => _relayColor,
                _ => Color.white
            };
        }

        private Color GetWeaponColor(string firepowerId)
        {
            return firepowerId switch
            {
                "fire_scatter" => new Color(0.9f, 0.2f, 0.2f),
                "fire_pulse" => new Color(0.9f, 0.9f, 0.2f),
                "fire_rail" => new Color(0.6f, 0.2f, 0.9f),
                _ => Color.white
            };
        }

        private Color GetThrusterColor(string mobilityId)
        {
            return mobilityId switch
            {
                "mob_treads" => new Color(0.5f, 0.5f, 0.5f),
                "mob_vector" => Color.white,
                "mob_burst" => new Color(0.2f, 0.8f, 0.4f),
                _ => Color.white
            };
        }

        private void DestroyCurrentPreview()
        {
            if (_currentPreviewRoot != null)
                Destroy(_currentPreviewRoot);
        }

        private void Update()
        {
            if (_currentPreviewRoot == null) return;

            // 마우스 드래그 감지
            if (Input.GetMouseButtonDown(0))
            {
                var ray = _previewCamera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out var hit))
                {
                    if (hit.transform.IsChildOf(_currentPreviewRoot.transform))
                    {
                        _isDragging = true;
                        _lastMousePosition = Input.mousePosition;
                    }
                }
            }

            if (Input.GetMouseButtonUp(0))
                _isDragging = false;

            // 회전 처리
            if (_isDragging)
            {
                Vector2 delta = (Vector2)Input.mousePosition - _lastMousePosition;
                _manualRotationY += delta.x * _dragRotationMultiplier * Time.deltaTime;
                _lastMousePosition = Input.mousePosition;
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
