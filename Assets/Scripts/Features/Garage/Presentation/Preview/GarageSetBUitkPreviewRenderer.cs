using System;
using Features.Garage.Presentation.Theme;
using Features.Garage.Runtime;
using Shared.Runtime;
using UnityEngine;

namespace Features.Garage.Presentation
{
    public sealed class GarageSetBUitkPreviewRenderer : MonoBehaviour
    {
        private const int AssemblyPreviewLayer = 29;
        private const int PartPreviewLayer = 30;

        [SerializeField] private Camera _previewCamera;
        [SerializeField] private Light _previewKeyLight;
        [SerializeField] private RenderTexture _renderTexture;
        [SerializeField] private int _textureSize = 512;
        [SerializeField] private float _autoRotationSpeed = 20f;
        [SerializeField] private int _previewLayer = -1;
        [SerializeField] private float _assemblyFitScale = 1f;
        [SerializeField] private bool _transparentBackground;

        private GameObject _currentPreviewRoot;

        internal Texture PreviewTexture => _renderTexture;
        internal bool HasPreview { get; private set; }
        internal GameObject CurrentPreviewRoot => _currentPreviewRoot;

        internal void ClearPreview()
        {
            DestroyCurrentPreview();
            HasPreview = false;
        }

        internal void ConfigurePreviewLayer(int previewLayer)
        {
            _previewLayer = Mathf.Clamp(previewLayer, 0, 30);
            EnsureCamera();
            if (_currentPreviewRoot != null)
                AssignPreviewLayer(_currentPreviewRoot);
        }

        internal void ConfigureAssemblyFitScale(float scale)
        {
            _assemblyFitScale = Mathf.Max(0.1f, scale);
        }

        internal void ConfigureTransparentBackground(bool isTransparent)
        {
            _transparentBackground = isTransparent;
            EnsureCamera();
        }

        internal bool Render(GarageSlotViewModel viewModel)
        {
            EnsureCamera();
            EnsureRenderTexture();
            DestroyCurrentPreview();
            HasPreview = false;

            if (_previewCamera == null ||
                !GarageUnitPreviewAssembly.HasPreviewAssemblyData(viewModel) ||
                viewModel.FramePreviewPrefab == null ||
                viewModel.FirepowerPreviewPrefab == null ||
                viewModel.MobilityPreviewPrefab == null)
                return false;

            if (!GarageUnitPreviewAssembly.TryCreatePreviewRoot(
                    viewModel,
                    _previewCamera,
                    viewModel.FramePreviewPrefab,
                    viewModel.FirepowerPreviewPrefab,
                    viewModel.MobilityPreviewPrefab,
                    out _currentPreviewRoot))
                return false;

            AssignPreviewLayer(_currentPreviewRoot);
            FitAssemblyToPreviewRoot(_currentPreviewRoot, _assemblyFitScale);
            HasPreview = true;
            RenderPreviewFrame();
            return true;
        }

        internal bool RenderPart(GarageNovaPartsPanelViewModel viewModel)
        {
            EnsureCamera();
            EnsureRenderTexture();
            DestroyCurrentPreview();
            HasPreview = false;

            if (_previewCamera == null ||
                viewModel == null ||
                viewModel.SelectedPreviewPrefab == null)
                return false;

            _currentPreviewRoot = new GameObject("PartPreviewRoot");
            GaragePreviewAssembler.Attach(
                _currentPreviewRoot,
                _previewCamera.transform,
                new Vector3(0f, -0.02f, 4.3f),
                Vector3.zero);

            var partObj = Instantiate(viewModel.SelectedPreviewPrefab);
            partObj.SetActive(true);
            GaragePreviewAssembler.Attach(
                partObj,
                _currentPreviewRoot.transform,
                Vector3.zero,
                ResolvePartPreviewEuler(viewModel.ActiveSlot, viewModel.SelectedAlignment));

            AssignPreviewLayer(_currentPreviewRoot);
            FitPartToPreviewRoot(_currentPreviewRoot, partObj);
            HasPreview = true;
            RenderPreviewFrame();
            return true;
        }

        private void Awake()
        {
            EnsureCamera();
            EnsureRenderTexture();
        }

        private void Reset()
        {
            EnsureCamera();
        }

        private void LateUpdate()
        {
            if (_currentPreviewRoot == null)
                return;

            GarageUnitPreviewAssembly.SetYaw(_currentPreviewRoot, Time.unscaledTime * _autoRotationSpeed);
            RenderPreviewFrame();
        }

        private void RenderPreviewFrame()
        {
            if (_previewCamera == null)
                return;

            EnsureRenderTexture();
            _previewCamera.Render();
        }

        private static Vector3 ResolvePartPreviewEuler(
            GarageNovaPartPanelSlot slot,
            GaragePanelCatalog.PartAlignment alignment)
        {
            var euler = alignment != null && alignment.CanApply
                ? alignment.SocketEuler
                : Vector3.zero;

            return slot == GarageNovaPartPanelSlot.Firepower
                ? euler + new Vector3(0f, 0f, -90f)
                : euler;
        }

        private static void FitPartToPreviewRoot(GameObject previewRoot, GameObject partObj)
        {
            if (previewRoot == null || partObj == null)
                return;

            if (!TryGetBounds(partObj, out var bounds))
                return;

            var centerLocal = previewRoot.transform.InverseTransformPoint(bounds.center);
            partObj.transform.localPosition -= centerLocal;

            float maxExtent = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);
            if (maxExtent > 0.0001f)
            {
                float scale = Mathf.Clamp(1.12f / maxExtent, 0.45f, 3.4f);
                partObj.transform.localScale *= scale;
            }
        }

        private static void FitAssemblyToPreviewRoot(GameObject previewRoot, float scaleMultiplier)
        {
            if (previewRoot == null)
                return;

            if (!TryGetBounds(previewRoot, out var bounds))
                return;

            var centerLocal = previewRoot.transform.InverseTransformPoint(bounds.center);
            for (int i = 0; i < previewRoot.transform.childCount; i++)
            {
                var child = previewRoot.transform.GetChild(i);
                child.localPosition -= centerLocal;
            }

            float maxExtent = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);
            if (maxExtent > 0.0001f)
            {
                float scale = Mathf.Clamp((1.32f * Mathf.Max(0.1f, scaleMultiplier)) / maxExtent, 0.55f, 5.8f);
                previewRoot.transform.localScale *= scale;
            }
        }

        private static bool TryGetBounds(GameObject root, out Bounds bounds)
        {
            bounds = default;
            if (root == null)
                return false;

            var renderers = root.GetComponentsInChildren<Renderer>(includeInactive: false);
            if (renderers == null || renderers.Length == 0)
                return false;

            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return true;
        }

        private void EnsureCamera()
        {
            if (_previewCamera == null)
                _previewCamera = ComponentAccess.Get<Camera>(gameObject);

            if (_previewCamera == null)
                return;

            var mask = 1 << ResolvePreviewLayer();
            _previewCamera.cullingMask = mask;
            _previewCamera.backgroundColor = _transparentBackground
                ? new Color(0f, 0f, 0f, 0f)
                : ThemeColors.PreviewBackground;
            _previewCamera.clearFlags = CameraClearFlags.SolidColor;
            _previewKeyLight = GaragePreviewAssembler.EnsurePreviewLighting(_previewCamera, _previewKeyLight);
            if (_previewKeyLight != null)
                _previewKeyLight.cullingMask = mask;
        }

        private int ResolvePreviewLayer()
        {
            if (_previewLayer >= 0 && _previewLayer <= 30)
                return _previewLayer;

            return gameObject != null &&
                   gameObject.name.IndexOf("Part", StringComparison.OrdinalIgnoreCase) >= 0
                ? PartPreviewLayer
                : AssemblyPreviewLayer;
        }

        private void AssignPreviewLayer(GameObject root)
        {
            if (root == null)
                return;

            SetLayerRecursively(root.transform, ResolvePreviewLayer());
        }

        private static void SetLayerRecursively(Transform root, int layer)
        {
            if (root == null)
                return;

            root.gameObject.layer = layer;
            for (var i = 0; i < root.childCount; i++)
            {
                SetLayerRecursively(root.GetChild(i), layer);
            }
        }

        private void EnsureRenderTexture()
        {
            if (_previewCamera == null)
                return;

            var size = Mathf.Max(128, _textureSize);
            if (_renderTexture == null)
            {
                _renderTexture = new RenderTexture(size, size, 16, RenderTextureFormat.ARGB32)
                {
                    antiAliasing = 2,
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
                _renderTexture.Create();
            }

            if (_previewCamera.targetTexture != _renderTexture)
                _previewCamera.targetTexture = _renderTexture;
        }

        private void DestroyCurrentPreview()
        {
            if (_currentPreviewRoot == null)
                return;

            DisposeUnityObject(_currentPreviewRoot);
            _currentPreviewRoot = null;
        }

        private void OnDestroy()
        {
            DestroyCurrentPreview();
            if (_renderTexture != null)
            {
                _renderTexture.Release();
                DisposeUnityObject(_renderTexture);
            }
        }

        private static void DisposeUnityObject(UnityEngine.Object target)
        {
            if (target == null)
                return;

            if (UnityEngine.Application.isPlaying)
                Destroy(target);
            else
                DestroyImmediate(target);
        }
    }
}
