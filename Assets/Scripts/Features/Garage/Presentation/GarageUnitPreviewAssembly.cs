using Features.Garage.Runtime;
using Features.Unit.Infrastructure;
using UnityEngine;

namespace Features.Garage.Presentation
{
    internal static class GarageUnitPreviewAssembly
    {
        private static readonly Vector3 FirepowerEulerCorrection = new Vector3(0f, 0f, -90f);
        private const float MinVectorSqrMagnitude = 0.000001f;
        private const string XfiWeaponDirectionOnly = "xfi_weapon_direction_only";
        private const string CanonicalHumanoidRigAnchorMode = "CanonicalHumanoidRig";

        public static bool HasCompleteLoadout(GarageSlotViewModel viewModel)
        {
            return viewModel != null &&
                   !string.IsNullOrWhiteSpace(viewModel.FrameId) &&
                   !string.IsNullOrWhiteSpace(viewModel.FirepowerId) &&
                   !string.IsNullOrWhiteSpace(viewModel.MobilityId);
        }

        public static bool HasPreviewAssemblyData(GarageSlotViewModel viewModel)
        {
            if (!HasCompleteLoadout(viewModel) ||
                !CanApply(viewModel.FrameAlignment) ||
                !CanApply(viewModel.FirepowerAlignment) ||
                !CanApply(viewModel.MobilityAlignment) ||
                !HasMobilitySocket(viewModel.MobilityAlignment, viewModel.MobilityUsesAssemblyPivot))
                return false;

            if (UsesCanonicalHumanoidRig(viewModel))
                return HasCanonicalHumanoidRigData(viewModel.FrameAlignment, viewModel.FirepowerAlignment);

            if (IsDirectionOnlyWeapon(viewModel.FirepowerAlignment))
                return false;

            return viewModel.FrameAlignment.HasFrameTopSocket;
        }

        public static bool TryCreatePreviewRoot(
            GarageSlotViewModel viewModel,
            Camera previewCamera,
            GameObject framePrefab,
            GameObject firepowerPrefab,
            GameObject mobilityPrefab,
            out GameObject previewRoot)
        {
            previewRoot = null;
            if (!HasPreviewAssemblyData(viewModel) ||
                previewCamera == null ||
                framePrefab == null ||
                firepowerPrefab == null ||
                mobilityPrefab == null)
                return false;

            previewRoot = new GameObject("PreviewRoot");
            GaragePreviewAssembler.AttachToPreviewCamera(
                previewRoot,
                previewCamera,
                new Vector3(0f, -0.04f, 6f),
                Vector3.zero);

            var frameObj = Object.Instantiate(framePrefab);
            frameObj.SetActive(true);
            GaragePreviewAssembler.Attach(
                frameObj,
                previewRoot.transform,
                ResolveFramePosition(viewModel.FrameAlignment),
                Vector3.zero);

            var weaponObj = Object.Instantiate(firepowerPrefab);
            weaponObj.SetActive(true);
            var usesCanonicalHumanoidRig = UsesCanonicalHumanoidRig(viewModel);
            var firepowerEuler = ResolveFirepowerEuler(viewModel.FirepowerAlignment);
            GaragePreviewAssembler.Attach(
                weaponObj,
                previewRoot.transform,
                usesCanonicalHumanoidRig
                    ? Vector3.zero
                    : ResolveAttachedPartPosition(
                        viewModel.FrameAlignment,
                        viewModel.FirepowerAlignment,
                        viewModel.FrameAssemblyForm,
                        viewModel.FirepowerAssemblyForm,
                        firepowerEuler),
                firepowerEuler);
            if (usesCanonicalHumanoidRig &&
                !TryApplyCanonicalHumanoidFirepowerRig(
                    previewRoot.transform,
                    frameObj.transform,
                    weaponObj.transform,
                    viewModel.FirepowerAlignment))
            {
                DestroyPreviewRoot(previewRoot);
                previewRoot = null;
                return false;
            }

            var mobilityObj = Object.Instantiate(mobilityPrefab);
            mobilityObj.SetActive(true);
            GaragePreviewAssembler.Attach(
                mobilityObj,
                previewRoot.transform,
                ResolveMobilityPosition(
                    viewModel.FrameAlignment,
                    viewModel.MobilityAlignment,
                    viewModel.MobilityUsesAssemblyPivot),
                viewModel.MobilityAlignment.SocketEuler);
            ApplyMobilityVisualBoundsFallback(
                previewRoot.transform,
                frameObj.transform,
                mobilityObj.transform,
                viewModel.MobilityAlignment,
                viewModel.MobilityUsesAssemblyPivot);

            return true;
        }

        public static void SetYaw(GameObject root, float yawDegrees)
        {
            GaragePreviewAssembler.SetYaw(root, yawDegrees);
        }

        private static Vector3 ResolveFramePosition(GaragePanelCatalog.PartAlignment frameAlignment)
        {
            return frameAlignment.HasVisualBounds
                ? frameAlignment.PivotOffset - frameAlignment.VisualBoundsCenter
                : frameAlignment.PivotOffset;
        }

        private static Vector3 ResolveAttachedPartPosition(
            GaragePanelCatalog.PartAlignment frameAlignment,
            GaragePanelCatalog.PartAlignment partAlignment,
            AssemblyForm frameAssemblyForm,
            AssemblyForm partAssemblyForm,
            Vector3 partEuler)
        {
            var framePosition = ResolveFramePosition(frameAlignment);
            var frameSocket = framePosition + ResolveFrameTopSocketOffset(
                frameAlignment,
                frameAssemblyForm,
                partAssemblyForm);
            var rotatedSocketOffset = Quaternion.Euler(partEuler) *
                                      ResolveAttachedPartSocketOffset(partAlignment);
            return frameSocket - rotatedSocketOffset;
        }

        private static Vector3 ResolveAttachedPartSocketOffset(GaragePanelCatalog.PartAlignment partAlignment)
        {
            if (partAlignment.AssemblyAnchorMode == "FrameTopSocket" && partAlignment.HasVisualBounds)
            {
                return new Vector3(
                    partAlignment.VisualBoundsCenter.x,
                    partAlignment.VisualBoundsMin.y,
                    partAlignment.VisualBoundsCenter.z);
            }

            return partAlignment.SocketOffset;
        }

        private static bool TryApplyCanonicalHumanoidFirepowerRig(
            Transform previewRoot,
            Transform frameObject,
            Transform firepowerObject,
            GaragePanelCatalog.PartAlignment firepowerAlignment)
        {
            if (previewRoot == null ||
                frameObject == null ||
                firepowerObject == null ||
                firepowerAlignment == null ||
                !TryGetRootSpaceRendererBounds(previewRoot, frameObject, out var frameBounds) ||
                !TryGetRootSpaceRendererBounds(previewRoot, firepowerObject, out var firepowerBounds))
                return false;

            var frameGrip = ResolveCanonicalHumanoidRightHandSocket(frameBounds);
            var weaponGrip = ResolveCanonicalHumanoidWeaponGrip(firepowerBounds);
            firepowerObject.localPosition += frameGrip - weaponGrip;
            firepowerObject.localPosition += ResolveApprovedCanonicalHumanoidOffset(firepowerAlignment);

            return true;
        }

        private static Vector3 ResolveCanonicalHumanoidRightHandSocket(Bounds frameBounds)
        {
            var frameWidth = Mathf.Max(frameBounds.size.x, 0.01f);
            var frameHeight = Mathf.Max(frameBounds.size.y, 0.01f);
            var frameDepth = Mathf.Max(frameBounds.size.z, 0.01f);
            return new Vector3(
                frameBounds.max.x + frameWidth * 0.35f,
                frameBounds.center.y + frameHeight * 0.05f,
                frameBounds.max.z + frameDepth * 0.12f);
        }

        private static Vector3 ResolveCanonicalHumanoidWeaponGrip(Bounds firepowerBounds)
        {
            return new Vector3(
                firepowerBounds.min.x,
                Mathf.Lerp(firepowerBounds.min.y, firepowerBounds.max.y, 0.45f),
                firepowerBounds.min.z);
        }

        private static Vector3 ResolveApprovedCanonicalHumanoidOffset(
            GaragePanelCatalog.PartAlignment firepowerAlignment)
        {
            if (firepowerAlignment == null ||
                firepowerAlignment.AssemblyAnchorMode != CanonicalHumanoidRigAnchorMode ||
                !IsApprovedReviewResult(firepowerAlignment.AssemblyReviewResult))
                return Vector3.zero;

            return firepowerAlignment.AssemblyLocalOffset;
        }

        private static void ApplyMobilityVisualBoundsFallback(
            Transform previewRoot,
            Transform frameObject,
            Transform mobilityObject,
            GaragePanelCatalog.PartAlignment mobilityAlignment,
            bool useAssemblyPivot)
        {
            if (previewRoot == null ||
                frameObject == null ||
                mobilityObject == null ||
                !UsesMobilityVisualBoundsFallback(mobilityAlignment, useAssemblyPivot) ||
                !TryGetRootSpaceRendererBounds(previewRoot, frameObject, out var frameBounds) ||
                !TryGetRootSpaceRendererBounds(previewRoot, mobilityObject, out var mobilityBounds))
                return;

            mobilityObject.localPosition += new Vector3(
                frameBounds.center.x - mobilityBounds.center.x,
                frameBounds.min.y - mobilityBounds.max.y,
                frameBounds.center.z - mobilityBounds.center.z);
        }

        private static bool UsesMobilityVisualBoundsFallback(
            GaragePanelCatalog.PartAlignment mobilityAlignment,
            bool useAssemblyPivot)
        {
            return mobilityAlignment != null &&
                   mobilityAlignment.HasVisualBounds &&
                   !useAssemblyPivot &&
                   !mobilityAlignment.HasGxTreeSocket &&
                   mobilityAlignment.SocketOffset.sqrMagnitude <= MinVectorSqrMagnitude &&
                   !mobilityAlignment.HasXfiAttachSocket;
        }

        private static Vector3 ResolveFrameTopSocketOffset(
            GaragePanelCatalog.PartAlignment frameAlignment,
            AssemblyForm frameAssemblyForm,
            AssemblyForm partAssemblyForm)
        {
            if (frameAlignment.FrameTopSocketOffset.sqrMagnitude > MinVectorSqrMagnitude)
                return frameAlignment.FrameTopSocketOffset;

            return frameAlignment.SocketOffset;
        }

        private static Vector3 ResolveMobilityPosition(
            GaragePanelCatalog.PartAlignment frameAlignment,
            GaragePanelCatalog.PartAlignment mobilityAlignment,
            bool useAssemblyPivot)
        {
            var framePosition = ResolveFramePosition(frameAlignment);
            var rotatedSocketOffset = Quaternion.Euler(mobilityAlignment.SocketEuler) *
                                      ResolveMobilitySocketOffset(mobilityAlignment, useAssemblyPivot);
            return framePosition - rotatedSocketOffset;
        }

        private static Vector3 ResolveMobilitySocketOffset(
            GaragePanelCatalog.PartAlignment mobilityAlignment,
            bool useAssemblyPivot)
        {
            if (useAssemblyPivot)
                return Vector3.zero;

            if (mobilityAlignment.HasGxTreeSocket)
                return mobilityAlignment.GxTreeSocketOffset;

            if (mobilityAlignment.SocketOffset.sqrMagnitude > MinVectorSqrMagnitude)
                return mobilityAlignment.SocketOffset;

            if (mobilityAlignment.HasVisualBounds)
            {
                return new Vector3(
                    mobilityAlignment.VisualBoundsCenter.x,
                    mobilityAlignment.VisualBoundsMax.y,
                    mobilityAlignment.VisualBoundsCenter.z);
            }

            return mobilityAlignment.HasXfiAttachSocket
                ? mobilityAlignment.XfiAttachSocketOffset
                : Vector3.zero;
        }

        private static bool TryGetRootSpaceRendererBounds(
            Transform previewRoot,
            Transform target,
            out Bounds bounds)
        {
            bounds = default;
            var renderers = target != null ? target.GetComponentsInChildren<Renderer>(true) : null;
            if (previewRoot == null || renderers == null || renderers.Length == 0)
                return false;

            var initialized = false;
            for (var i = 0; i < renderers.Length; i++)
            {
                var worldBounds = renderers[i].bounds;
                var min = worldBounds.min;
                var max = worldBounds.max;
                var corners = new[]
                {
                    new Vector3(min.x, min.y, min.z),
                    new Vector3(min.x, min.y, max.z),
                    new Vector3(min.x, max.y, min.z),
                    new Vector3(min.x, max.y, max.z),
                    new Vector3(max.x, min.y, min.z),
                    new Vector3(max.x, min.y, max.z),
                    new Vector3(max.x, max.y, min.z),
                    new Vector3(max.x, max.y, max.z)
                };

                for (var cornerIndex = 0; cornerIndex < corners.Length; cornerIndex++)
                {
                    var local = previewRoot.InverseTransformPoint(corners[cornerIndex]);
                    if (!initialized)
                    {
                        bounds = new Bounds(local, Vector3.zero);
                        initialized = true;
                    }
                    else
                    {
                        bounds.Encapsulate(local);
                    }
                }
            }

            return initialized;
        }

        private static Vector3 ResolveFirepowerEuler(GaragePanelCatalog.PartAlignment firepowerAlignment)
        {
            return firepowerAlignment.SocketEuler + FirepowerEulerCorrection;
        }

        private static bool UsesCanonicalHumanoidRig(GarageSlotViewModel viewModel)
        {
            return viewModel != null &&
                   viewModel.FrameAssemblyForm == AssemblyForm.Humanoid &&
                   viewModel.FirepowerAssemblyForm == AssemblyForm.Humanoid;
        }

        private static bool HasCanonicalHumanoidRigData(
            GaragePanelCatalog.PartAlignment frameAlignment,
            GaragePanelCatalog.PartAlignment firepowerAlignment)
        {
            return frameAlignment.HasVisualBounds &&
                   firepowerAlignment.HasVisualBounds;
        }

        private static bool CanApply(GaragePanelCatalog.PartAlignment alignment)
        {
            return alignment != null && alignment.CanApply;
        }

        private static bool IsDirectionOnlyWeapon(GaragePanelCatalog.PartAlignment alignment)
        {
            return alignment != null && alignment.XfiSocketQuality == XfiWeaponDirectionOnly;
        }

        private static bool IsApprovedReviewResult(string reviewResult)
        {
            return reviewResult == "approved" || reviewResult == "match";
        }

        private static bool HasMobilitySocket(
            GaragePanelCatalog.PartAlignment mobilityAlignment,
            bool useAssemblyPivot)
        {
            return useAssemblyPivot ||
                   mobilityAlignment.HasGxTreeSocket ||
                   mobilityAlignment.SocketOffset.sqrMagnitude > MinVectorSqrMagnitude ||
                   mobilityAlignment.HasVisualBounds ||
                   mobilityAlignment.HasXfiAttachSocket;
        }

        private static void DestroyPreviewRoot(GameObject previewRoot)
        {
            if (previewRoot == null)
                return;

            if (Application.isPlaying)
                Object.Destroy(previewRoot);
            else
                Object.DestroyImmediate(previewRoot);
        }
    }
}
