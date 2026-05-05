using System;
using Features.Garage.Presentation.Theme;
using Features.Garage.Runtime;
using Shared.Runtime;
using Shared.Attributes;
using UnityEngine;

namespace Features.Garage.Presentation
{
    public sealed class GarageSetBUitkPreviewRenderer : MonoBehaviour
    {
        // csharp-guardrails: allow-serialized-field-without-required
        [SerializeField] private Camera _previewCamera;
        // csharp-guardrails: allow-serialized-field-without-required
        [SerializeField] private Light _previewKeyLight;
        // csharp-guardrails: allow-serialized-field-without-required
        // csharp-guardrails: allow-serialized-field-without-required
        [SerializeField] private RenderTexture _renderTexture;
        [SerializeField] private int _textureSize = GarageUitkConstants.Preview.TextureSize;
        [SerializeField] private float _autoRotationSpeed = 20f;
        [SerializeField] private int _previewLayer = -1;
        [SerializeField] private float _assemblyFitScale = 1f;
        [SerializeField] private float _assemblyHorizontalOffset;
        [SerializeField] private bool _transparentBackground;

        private GameObject _currentPreviewRoot;
        private string _currentPreviewKey;
        private float _lastRenderedYaw;
        private bool _needsRender;

        internal Texture PreviewTexture => _renderTexture;
        internal bool HasPreview { get; private set; }
        internal GameObject CurrentPreviewRoot => _currentPreviewRoot;

        internal void ClearPreview()
        {
            DestroyCurrentPreview();
            _currentPreviewKey = null;
            HasPreview = false;
        }

        internal void ConfigurePreviewLayer(int previewLayer)
        {
            _previewLayer = Mathf.Clamp(previewLayer, 0, 30);
            EnsureCamera();
            // csharp-guardrails: allow-null-defense
            if (_currentPreviewRoot != null)
                AssignPreviewLayer(_currentPreviewRoot);
        }

        internal void ConfigureAssemblyFitScale(float scale)
        {
            _assemblyFitScale = Mathf.Max(0.1f, scale);
        }

        internal void ConfigureAssemblyHorizontalOffset(float offset)
        {
            _assemblyHorizontalOffset = offset;
            // csharp-guardrails: allow-null-defense
            if (_currentPreviewRoot != null)
                ApplyAssemblyHorizontalOffset(_currentPreviewRoot);
        }

        internal void ConfigureTransparentBackground(bool isTransparent)
        {
            _transparentBackground = isTransparent;
            EnsureCamera();
        }

        /// <summary>
        /// PreviewData를 사용하는 개선된 버전 - ViewModel 의존성 제거
        /// </summary>
        internal bool Render(GarageSlotPreviewData previewData, string loadoutKey)
        {
            EnsureCamera();
            EnsureRenderTexture();

            // csharp-guardrails: allow-null-defense
            if (_previewCamera == null ||
                !GarageUnitPreviewAssembly.HasCurrentSelectionPreviewData(previewData))
            {
                ClearPreview();
                return false;
            }

            var previewKey = BuildAssemblyPreviewKey(previewData, loadoutKey);
            // csharp-guardrails: allow-null-defense
            if (_currentPreviewRoot != null && string.Equals(_currentPreviewKey, previewKey, StringComparison.Ordinal))
            {
                HasPreview = true;
                // 회전 초기화 후 한 번만 렌더링 (LateUpdate에서 회전으로 변경됨)
                _lastRenderedYaw = Time.unscaledTime * _autoRotationSpeed;
                RenderPreviewFrame();
                return true;
            }

            DestroyCurrentPreview();
            _currentPreviewKey = null;
            HasPreview = false;

            if (!GarageUnitPreviewAssembly.TryCreateCurrentSelectionPreviewRoot(
                    previewData,
                    _previewCamera,
                    out _currentPreviewRoot))
                return false;

            _currentPreviewKey = previewKey;
            AssignPreviewLayer(_currentPreviewRoot);
            FitAssemblyToPreviewRoot(_currentPreviewRoot, _assemblyFitScale);
            ApplyAssemblyHorizontalOffset(_currentPreviewRoot);
            HasPreview = true;
            RenderPreviewFrame();
            return true;
        }

        internal bool Render(GarageSlotViewModel viewModel)
        {
            // 하위 호환성을 위한 위임
            return Render(viewModel?.Preview, viewModel?.LoadoutKey);
        }

        internal bool RenderPart(GarageNovaPartsPanelViewModel viewModel)
        {
            EnsureCamera();
            EnsureRenderTexture();

            // csharp-guardrails: allow-null-defense
            if (_previewCamera == null ||
                viewModel == null ||
                // csharp-guardrails: allow-null-defense
                viewModel.SelectedPreviewPrefab == null)
            {
                ClearPreview();
                return false;
            }

            var previewKey = BuildPartPreviewKey(viewModel);
            // csharp-guardrails: allow-null-defense
            if (_currentPreviewRoot != null && string.Equals(_currentPreviewKey, previewKey, StringComparison.Ordinal))
            {
                HasPreview = true;
                // 회전 초기화 후 한 번만 렌더링 (LateUpdate에서 회전으로 변경됨)
                _lastRenderedYaw = Time.unscaledTime * _autoRotationSpeed;
                RenderPreviewFrame();
                return true;
            }

            DestroyCurrentPreview();
            _currentPreviewKey = null;
            HasPreview = false;

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
            _currentPreviewKey = previewKey;
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
            // csharp-guardrails: allow-null-defense
            if (_currentPreviewRoot == null)
                return;

            float currentYaw = Time.unscaledTime * _autoRotationSpeed;
            GarageUnitPreviewAssembly.SetYaw(_currentPreviewRoot, currentYaw);

            // 회전 각도가 유의미하게 변경된 경우에만 렌더링
            float yawDelta = Mathf.Abs(Mathf.DeltaAngle(_lastRenderedYaw, currentYaw));
            if (yawDelta > GarageUitkConstants.Rendering.RotationThreshold)
            {
                _lastRenderedYaw = currentYaw;
                RenderPreviewFrame();
            }
        }

        private void RenderPreviewFrame()
        {
            // csharp-guardrails: allow-null-defense
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
                ? alignment.Socket.Euler
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

        private void ApplyAssemblyHorizontalOffset(GameObject previewRoot)
        {
            if (previewRoot == null)
                return;

            var localPosition = previewRoot.transform.localPosition;
            localPosition.x = _assemblyHorizontalOffset;
            previewRoot.transform.localPosition = localPosition;
        }

        private static bool TryGetBounds(GameObject root, out Bounds bounds)
        {
            bounds = default;
            if (root == null)
                return false;

            var renderers = root.GetComponentsInChildren<Renderer>(includeInactive: false);
            // csharp-guardrails: allow-null-defense
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
            // csharp-guardrails: allow-null-defense
            if (_previewCamera == null)
                _previewCamera = ComponentAccess.Get<Camera>(gameObject);

            // csharp-guardrails: allow-null-defense
            if (_previewCamera == null)
                return;

            var mask = 1 << ResolvePreviewLayer();
            _previewCamera.cullingMask = mask;
            _previewCamera.backgroundColor = _transparentBackground
                ? new Color(0f, 0f, 0f, 0f)
                : ThemeColors.PreviewBackground;
            _previewCamera.clearFlags = CameraClearFlags.SolidColor;
            _previewKeyLight = GaragePreviewAssembler.EnsurePreviewLighting(_previewCamera, _previewKeyLight);
// csharp-guardrails: allow-null-defense
            if (_previewKeyLight != null)
                _previewKeyLight.cullingMask = mask;
        }

        private int ResolvePreviewLayer()
        {
            if (_previewLayer >= GarageUitkConstants.Layers.MinLayer && _previewLayer <= GarageUitkConstants.Layers.MaxLayer)
                return _previewLayer;

            // csharp-guardrails: allow-null-defense
            return gameObject != null &&
                   gameObject.name.IndexOf("Part", StringComparison.OrdinalIgnoreCase) >= 0
                ? GarageUitkConstants.Layers.PartPreview
                : GarageUitkConstants.Layers.AssemblyPreview;
        }

        private static string BuildAssemblyPreviewKey(GarageSlotPreviewData previewData, string loadoutKey)
        {
            if (previewData == null)
                return "assembly:null";

            return string.Join(
                "|",
                "assembly",
                loadoutKey ?? string.Empty,
// csharp-guardrails: allow-null-defense
                previewData.FrameId ?? string.Empty,
// csharp-guardrails: allow-null-defense
                previewData.FirepowerId ?? string.Empty,
// csharp-guardrails: allow-null-defense
                previewData.MobilityId ?? string.Empty,
                GetObjectKey(previewData.FramePreviewPrefab),
                GetObjectKey(previewData.FirepowerPreviewPrefab),
                GetObjectKey(previewData.MobilityPreviewPrefab),
                previewData.MobilityUsesAssemblyPivot ? "pivot" : "no-pivot",
                previewData.FrameAssemblyForm.ToString(),
                previewData.FirepowerAssemblyForm.ToString(),
                GetAlignmentKey(previewData.FrameAlignment),
                GetAlignmentKey(previewData.FirepowerAlignment),
                GetAlignmentKey(previewData.MobilityAlignment));
        }

        private static string BuildAssemblyPreviewKey(GarageSlotViewModel viewModel)
        {
            // 하위 호환성을 위한 위임
            return BuildAssemblyPreviewKey(viewModel?.Preview, viewModel?.LoadoutKey);
        }

        private static string BuildPartPreviewKey(GarageNovaPartsPanelViewModel viewModel)
        {
            if (viewModel == null)
                return "part:null";

            return string.Join(
                "|",
                "part",
                viewModel.ActiveSlot.ToString(),
// csharp-guardrails: allow-null-defense
                viewModel.SelectedPartId ?? string.Empty,
                GetObjectKey(viewModel.SelectedPreviewPrefab),
                GetAlignmentKey(viewModel.SelectedAlignment));
        }

        private static string GetObjectKey(UnityEngine.Object target)
        {
            return target == null ? "0" : target.GetInstanceID().ToString();
        }

        private static string GetAlignmentKey(GaragePanelCatalog.PartAlignment alignment)
        {
            if (alignment == null)
                return string.Empty;

            return string.Join(
                ",",
// csharp-guardrails: allow-null-defense
                alignment.Assembly.QualityFlag ?? string.Empty,
                alignment.Socket.Offset.ToString("F3"),
                alignment.Socket.Euler.ToString("F3"),
                alignment.Assembly.LocalOffset.ToString("F3"),
                alignment.Assembly.LocalEuler.ToString("F3"),
                alignment.Assembly.LocalScale.ToString("F3"));
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
            // csharp-guardrails: allow-null-defense
            if (_previewCamera == null)
                return;

            var size = Mathf.Max(GarageUitkConstants.Preview.TextureMinSize, _textureSize);
            // csharp-guardrails: allow-null-defense
            if (_renderTexture == null)
            {
                _renderTexture = new RenderTexture(size, size, 16, RenderTextureFormat.ARGB32)
                {
                    antiAliasing = GarageUitkConstants.Preview.TextureAntiAliasing,
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
            // csharp-guardrails: allow-null-defense
            if (_currentPreviewRoot == null)
                return;

            DisposeUnityObject(_currentPreviewRoot);
            _currentPreviewRoot = null;
        }

        private void OnDestroy()
        {
            DestroyCurrentPreview();
            // csharp-guardrails: allow-null-defense
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
