using UnityEngine;
using UnityEngine.UI;
using Features.Garage.Domain;
using Features.Garage.Runtime;
using Features.Garage.Presentation.Theme;
using Shared.Attributes;
using TMPro;
using UnityEngine.InputSystem;

namespace Features.Garage.Presentation
{
    /// <summary>
    /// Garage에서 선택된 유닛의 3D 미리보기를 표시합니다.
    /// Nova1492 preview prefab mapping을 우선 사용하고, mapping이 없으면 기본 도형 fallback을 사용합니다.
    /// </summary>
    public sealed class GarageUnitPreviewView : MonoBehaviour
    {
        private static readonly Vector3 FallbackFramePosition = Vector3.zero;
        private static readonly Vector3 FallbackWeaponPosition = new Vector3(0f, 0.62f, 0f);
        private static readonly Vector3 FallbackWeaponEuler = new Vector3(0f, 0f, 90f);
        private static readonly Vector3 FallbackMobilityPosition = new Vector3(0f, -0.58f, 0f);

        [System.Serializable]
        private sealed class PartPrefabMapping
        {
            [SerializeField] private string _id;
            [SerializeField] private GameObject _prefab;

            public GameObject Prefab => _prefab;

            public bool Matches(string id)
            {
                return !string.IsNullOrWhiteSpace(id) && _id == id && _prefab != null;
            }
        }

        [Header("Viewport")]
        [Required, SerializeField] private Camera _previewCamera;
        [SerializeField] private RenderTexture _renderTexture;
        [Required, SerializeField] private RawImage _rawImage;
        [Required, SerializeField] private TMP_Text _emptyStateText;

        [Header("Part Prefabs (Primitive Fallbacks)")]
        [Tooltip("Nova1492 frame mapping이 없을 때 쓰는 fallback")]
        [Required, SerializeField] private GameObject _framePrefab;
        [Tooltip("Nova1492 firepower mapping이 없을 때 쓰는 fallback")]
        [Required, SerializeField] private GameObject _weaponPrefab;
        [Tooltip("Nova1492 mobility mapping이 없을 때 쓰는 fallback")]
        [Required, SerializeField] private GameObject _thrusterPrefab;

        [Header("Nova1492 Model Mappings")]
        [SerializeField] private PartPrefabMapping[] _frameModelPrefabs;
        [SerializeField] private PartPrefabMapping[] _firepowerModelPrefabs;
        [SerializeField] private PartPrefabMapping[] _mobilityModelPrefabs;

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
            GaragePreviewAssembler.EnsurePreviewLighting(_previewCamera);
            _rawImage.color = Color.Lerp(ThemeColors.PreviewBackground, ThemeColors.BackgroundCard, 0.18f);
            _emptyStateText.text = "저장 유닛 실루엣";
            _emptyStateText.color = ThemeColors.TextMuted;

            SetEmptyStateVisible(true);
        }

        public void Render(GarageSlotViewModel viewModel)
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

            CreatePreview(viewModel);
            SetEmptyStateVisible(false);
            _rawImage.color = Color.white;

            if (_previewCamera.targetTexture != null)
                _previewCamera.Render();
        }

        private void CreatePreview(GarageSlotViewModel viewModel)
        {
            _currentPreviewRoot = new GameObject("PreviewRoot");
            GaragePreviewAssembler.AttachToPreviewCamera(_currentPreviewRoot, _previewCamera, new Vector3(0f, -0.04f, 6f), Vector3.zero);

            var framePosition = ResolveFramePosition(viewModel.FrameAlignment);
            var weaponPosition = ResolveAttachedPartPosition(
                viewModel.FrameAlignment,
                viewModel.FirepowerAlignment,
                attachToFrameTop: true,
                FallbackWeaponPosition);
            var weaponEuler = ResolvePartEuler(viewModel.FirepowerAlignment, FallbackWeaponEuler);
            var mobilityPosition = ResolveAttachedPartPosition(
                viewModel.FrameAlignment,
                viewModel.MobilityAlignment,
                attachToFrameTop: false,
                FallbackMobilityPosition);
            var mobilityEuler = ResolvePartEuler(viewModel.MobilityAlignment, Vector3.zero);

            var frameObj = CreateFrame(viewModel.FrameId, viewModel.FramePreviewPrefab);
            GaragePreviewAssembler.Attach(frameObj, _currentPreviewRoot.transform, framePosition, Vector3.zero);

            var weaponObj = CreateWeapon(viewModel.FirepowerId, viewModel.FirepowerPreviewPrefab);
            GaragePreviewAssembler.Attach(weaponObj, _currentPreviewRoot.transform, weaponPosition, weaponEuler);

            var thrusterObj = CreateThruster(viewModel.MobilityId, viewModel.MobilityPreviewPrefab);
            GaragePreviewAssembler.Attach(thrusterObj, _currentPreviewRoot.transform, mobilityPosition, mobilityEuler);
        }

        private static Vector3 ResolveFramePosition(GaragePanelCatalog.PartAlignment frameAlignment)
        {
            return CanApply(frameAlignment)
                ? frameAlignment.PivotOffset
                : FallbackFramePosition;
        }

        private static Vector3 ResolveAttachedPartPosition(
            GaragePanelCatalog.PartAlignment frameAlignment,
            GaragePanelCatalog.PartAlignment partAlignment,
            bool attachToFrameTop,
            Vector3 fallbackPosition)
        {
            if (!CanApply(frameAlignment) || !CanApply(partAlignment))
                return fallbackPosition;

            var framePosition = ResolveFramePosition(frameAlignment);
            var frameSocket = attachToFrameTop
                ? ResolveFrameTopSocket(frameAlignment, framePosition)
                : ResolveFrameBottomSocket(frameAlignment, framePosition);
            var rotatedSocketOffset = Quaternion.Euler(partAlignment.SocketEuler) * partAlignment.SocketOffset;
            return frameSocket - rotatedSocketOffset;
        }

        private static Vector3 ResolvePartEuler(GaragePanelCatalog.PartAlignment partAlignment, Vector3 fallbackEuler)
        {
            return CanApply(partAlignment)
                ? partAlignment.SocketEuler
                : fallbackEuler;
        }

        private static Vector3 ResolveFrameTopSocket(GaragePanelCatalog.PartAlignment alignment, Vector3 framePosition)
        {
            return framePosition + new Vector3(
                alignment.BoundsCenter.x,
                alignment.BoundsCenter.y + alignment.BoundsSize.y * 0.5f,
                alignment.BoundsCenter.z);
        }

        private static Vector3 ResolveFrameBottomSocket(GaragePanelCatalog.PartAlignment alignment, Vector3 framePosition)
        {
            return framePosition + new Vector3(
                alignment.BoundsCenter.x,
                alignment.BoundsCenter.y - alignment.BoundsSize.y * 0.5f,
                alignment.BoundsCenter.z);
        }

        private static bool CanApply(GaragePanelCatalog.PartAlignment alignment)
        {
            return alignment != null && alignment.CanApply;
        }

        private GameObject CreateFrame(string frameId, GameObject previewPrefab)
        {
            var obj = Instantiate(ResolvePartPrefab(frameId, previewPrefab, _frameModelPrefabs, _framePrefab));
            obj.SetActive(true);
            return obj;
        }

        private GameObject CreateWeapon(string firepowerId, GameObject previewPrefab)
        {
            var obj = Instantiate(ResolvePartPrefab(firepowerId, previewPrefab, _firepowerModelPrefabs, _weaponPrefab));
            obj.SetActive(true);
            return obj;
        }

        private GameObject CreateThruster(string mobilityId, GameObject previewPrefab)
        {
            var obj = Instantiate(ResolvePartPrefab(mobilityId, previewPrefab, _mobilityModelPrefabs, _thrusterPrefab));
            obj.SetActive(true);
            return obj;
        }

        private static GameObject ResolvePartPrefab(string id, GameObject previewPrefab, PartPrefabMapping[] mappings, GameObject fallback)
        {
            if (previewPrefab != null)
                return previewPrefab;

            if (mappings != null)
            {
                for (var i = 0; i < mappings.Length; i++)
                {
                    var mapping = mappings[i];
                    if (mapping != null && mapping.Matches(id))
                        return mapping.Prefab;
                }
            }

            return fallback;
        }

        private void DestroyCurrentPreview()
        {
            if (_currentPreviewRoot == null)
                return;

            Destroy(_currentPreviewRoot);
            _currentPreviewRoot = null;
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

            bool hasPointer = TryGetPointerState(
                out var pointerPosition,
                out var wasPressedThisFrame,
                out var wasReleasedThisFrame);

            if (hasPointer && wasPressedThisFrame && IsPointerInsidePreview(pointerPosition))
            {
                _isDragging = true;
                _lastMousePosition = pointerPosition;
            }
            else if (wasReleasedThisFrame)
            {
                _isDragging = false;
            }

            if (_isDragging && hasPointer)
            {
                var delta = pointerPosition - _lastMousePosition;
                _manualRotationY -= delta.x * _dragRotationMultiplier;
                _lastMousePosition = pointerPosition;
            }

            var autoRotation = Time.unscaledTime * _autoRotationSpeed;
            GaragePreviewAssembler.SetYaw(_currentPreviewRoot, _manualRotationY + autoRotation);
        }

        private static bool TryGetPointerState(
            out Vector2 pointerPosition,
            out bool wasPressedThisFrame,
            out bool wasReleasedThisFrame)
        {
            var pointer = Pointer.current;
            if (pointer == null)
            {
                pointerPosition = Vector2.zero;
                wasPressedThisFrame = false;
                wasReleasedThisFrame = false;
                return false;
            }

            pointerPosition = pointer.position.ReadValue();
            wasPressedThisFrame = pointer.press.wasPressedThisFrame;
            wasReleasedThisFrame = pointer.press.wasReleasedThisFrame;
            return true;
        }

        private bool IsPointerInsidePreview(Vector2 pointerPosition)
        {
            return _rawImage != null &&
                   RectTransformUtility.RectangleContainsScreenPoint(_rawImage.rectTransform, pointerPosition);
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
