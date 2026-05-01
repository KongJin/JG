using Features.Garage.Runtime;
using Features.Unit.Infrastructure;
using UnityEngine;

namespace Features.Garage.Presentation
{
    internal static class GarageUnitPreviewAssembly
    {
        private static readonly Vector3 FirepowerEulerCorrection = new Vector3(0f, 0f, -90f);

        public static bool HasCompleteLoadout(GarageSlotViewModel viewModel)
        {
            return viewModel != null &&
                   !string.IsNullOrWhiteSpace(viewModel.FrameId) &&
                   !string.IsNullOrWhiteSpace(viewModel.FirepowerId) &&
                   !string.IsNullOrWhiteSpace(viewModel.MobilityId);
        }

        public static bool HasPreviewAssemblyData(GarageSlotViewModel viewModel)
        {
            return HasCompleteLoadout(viewModel) &&
                   CanApply(viewModel.FrameAlignment) &&
                   CanApply(viewModel.FirepowerAlignment) &&
                   CanApply(viewModel.MobilityAlignment) &&
                   viewModel.FrameAlignment.HasFrameTopSocket &&
                   HasMobilitySocket(viewModel.MobilityAlignment, viewModel.MobilityUsesAssemblyPivot);
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
            var firepowerEuler = ResolveFirepowerEuler(viewModel.FirepowerAlignment);
            GaragePreviewAssembler.Attach(
                weaponObj,
                previewRoot.transform,
                ResolveAttachedPartPosition(
                    viewModel.FrameAlignment,
                    viewModel.FirepowerAlignment,
                    viewModel.FrameAssemblyForm,
                    viewModel.FirepowerAssemblyForm,
                    firepowerEuler,
                    weaponObj),
                firepowerEuler);

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

            return true;
        }

        public static void SetYaw(GameObject root, float yawDegrees)
        {
            GaragePreviewAssembler.SetYaw(root, yawDegrees);
        }

        private static Vector3 ResolveFramePosition(GaragePanelCatalog.PartAlignment frameAlignment)
        {
            return frameAlignment.PivotOffset;
        }

        private static Vector3 ResolveAttachedPartPosition(
            GaragePanelCatalog.PartAlignment frameAlignment,
            GaragePanelCatalog.PartAlignment partAlignment,
            AssemblyForm frameAssemblyForm,
            AssemblyForm partAssemblyForm,
            Vector3 partEuler,
            GameObject partObject = null)
        {
            var framePosition = ResolveFramePosition(frameAlignment);
            var frameSocket = framePosition + ResolveFrameTopSocketOffset(
                frameAlignment,
                frameAssemblyForm,
                partAssemblyForm);
            var rotatedSocketOffset = Quaternion.Euler(partEuler) *
                                      ResolveAttachedPartSocketOffset(partAlignment, partObject);
            return frameSocket - rotatedSocketOffset;
        }

        private static Vector3 ResolveAttachedPartSocketOffset(
            GaragePanelCatalog.PartAlignment partAlignment,
            GameObject partObject)
        {
            var socketOffset = partAlignment.SocketOffset;
            if (partObject == null ||
                partAlignment.XfiSocketQuality != "xfi_weapon_direction_only" ||
                !TryGetLocalRendererBounds(partObject.transform, out var bounds))
                return socketOffset;

            return socketOffset.y < bounds.min.y - 0.02f
                ? new Vector3(socketOffset.x, bounds.min.y, socketOffset.z)
                : socketOffset;
        }

        private static Vector3 ResolveFrameTopSocketOffset(
            GaragePanelCatalog.PartAlignment frameAlignment,
            AssemblyForm frameAssemblyForm,
            AssemblyForm partAssemblyForm)
        {
            if (frameAlignment.FrameTopSocketOffset.sqrMagnitude > 0.000001f)
                return frameAlignment.FrameTopSocketOffset;

            if (frameAlignment.HasFrameTopSocket &&
                frameAssemblyForm == AssemblyForm.Humanoid &&
                partAssemblyForm == AssemblyForm.Humanoid)
            {
                return frameAlignment.FrameTopSocketOffset;
            }

            return frameAlignment.SocketOffset;
        }

        private static bool TryGetLocalRendererBounds(Transform root, out Bounds bounds)
        {
            bounds = default;
            var renderers = root != null ? root.GetComponentsInChildren<Renderer>(true) : null;
            if (renderers == null || renderers.Length == 0)
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
                    var local = root.InverseTransformPoint(corners[cornerIndex]);
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

            if (mobilityAlignment.SocketOffset.sqrMagnitude > 0.000001f)
                return mobilityAlignment.SocketOffset;

            return mobilityAlignment.HasXfiAttachSocket
                ? mobilityAlignment.XfiAttachSocketOffset
                : Vector3.zero;
        }

        private static Vector3 ResolveFirepowerEuler(GaragePanelCatalog.PartAlignment firepowerAlignment)
        {
            return firepowerAlignment.SocketEuler + FirepowerEulerCorrection;
        }

        private static bool CanApply(GaragePanelCatalog.PartAlignment alignment)
        {
            return alignment != null && alignment.CanApply;
        }

        private static bool HasMobilitySocket(
            GaragePanelCatalog.PartAlignment mobilityAlignment,
            bool useAssemblyPivot)
        {
            return useAssemblyPivot ||
                   mobilityAlignment.HasGxTreeSocket ||
                   mobilityAlignment.SocketOffset.sqrMagnitude > 0.000001f ||
                   mobilityAlignment.HasXfiAttachSocket;
        }
    }
}
