using Features.Garage.Presentation.Theme;
using Features.Garage.Runtime;
using Shared.Runtime;
using Shared.Runtime.Pooling;
using UnityEngine;

namespace Features.Garage.Presentation
{
    public sealed class GarageSetBUitkPreviewRenderer : MonoBehaviour
    {
        [SerializeField] private Camera _previewCamera;
        [SerializeField] private RenderTexture _renderTexture;
        [SerializeField] private int _textureSize = 512;
        [SerializeField] private float _autoRotationSpeed = 20f;

        private GameObject _currentPreviewRoot;

        public Texture PreviewTexture => _renderTexture;
        public bool HasPreview { get; private set; }

        public bool Render(GarageSlotViewModel viewModel)
        {
            EnsureCamera();
            EnsureRenderTexture();
            DestroyCurrentPreview();
            HasPreview = false;

            if (_previewCamera == null ||
                !GarageUnitPreviewAssembly.HasCompleteLoadout(viewModel) ||
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

        private void EnsureCamera()
        {
            if (_previewCamera == null)
                _previewCamera = ComponentAccess.Get<Camera>(gameObject);

            if (_previewCamera == null)
                return;

            _previewCamera.backgroundColor = ThemeColors.PreviewBackground;
            _previewCamera.clearFlags = CameraClearFlags.SolidColor;
            GaragePreviewAssembler.EnsurePreviewLighting(_previewCamera);
        }

        private void EnsureRenderTexture()
        {
            if (_previewCamera == null)
                return;

            var size = Mathf.Max(128, _textureSize);
            if (_renderTexture == null)
            {
                _renderTexture = new RenderTexture(size, size, 16)
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

            Destroy(_currentPreviewRoot);
            _currentPreviewRoot = null;
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
