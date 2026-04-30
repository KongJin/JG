using Features.Garage.Runtime;
using UnityEngine;

namespace Features.Garage.Presentation
{
    internal static class GarageUnitPreviewAssembly
    {
        private static readonly Vector3 FallbackFramePosition = Vector3.zero;
        private static readonly Vector3 FallbackWeaponPosition = new Vector3(0f, 0.62f, 0f);
        private static readonly Vector3 FallbackWeaponEuler = Vector3.zero;
        private static readonly Vector3 FirepowerEulerCorrection = new Vector3(0f, 0f, -90f);
        private static readonly Vector3 FallbackMobilityPosition = new Vector3(0f, -0.58f, 0f);

        public static bool HasCompleteLoadout(GarageSlotViewModel viewModel)
        {
            return viewModel != null &&
                   !string.IsNullOrWhiteSpace(viewModel.FrameId) &&
                   !string.IsNullOrWhiteSpace(viewModel.FirepowerId) &&
                   !string.IsNullOrWhiteSpace(viewModel.MobilityId);
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
            if (!HasCompleteLoadout(viewModel) ||
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
                    firepowerEuler,
                    FallbackWeaponPosition),
                firepowerEuler);

            var mobilityObj = Object.Instantiate(mobilityPrefab);
            mobilityObj.SetActive(true);
            GaragePreviewAssembler.Attach(
                mobilityObj,
                previewRoot.transform,
                ResolveMobilityPosition(
                    viewModel.FrameAlignment,
                    viewModel.MobilityAlignment,
                    viewModel.MobilityUsesAssemblyPivot,
                    FallbackMobilityPosition),
                ResolvePartEuler(viewModel.MobilityAlignment, Vector3.zero));

            return true;
        }

        public static void SetYaw(GameObject root, float yawDegrees)
        {
            GaragePreviewAssembler.SetYaw(root, yawDegrees);
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
            Vector3 partEuler,
            Vector3 fallbackPosition)
        {
            if (!CanApply(frameAlignment) || !CanApply(partAlignment))
                return fallbackPosition;

            var framePosition = ResolveFramePosition(frameAlignment);
            if (!TryResolveFrameTopSocket(frameAlignment, framePosition, out var frameSocket))
                return fallbackPosition;

            var rotatedSocketOffset = Quaternion.Euler(partEuler) * partAlignment.SocketOffset;
            return frameSocket - rotatedSocketOffset;
        }

        private static Vector3 ResolveMobilityPosition(
            GaragePanelCatalog.PartAlignment frameAlignment,
            GaragePanelCatalog.PartAlignment mobilityAlignment,
            bool useAssemblyPivot,
            Vector3 fallbackPosition)
        {
            if (!CanApply(frameAlignment) || !CanApply(mobilityAlignment))
                return fallbackPosition;

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

        private static Vector3 ResolvePartEuler(GaragePanelCatalog.PartAlignment partAlignment, Vector3 fallbackEuler)
        {
            return CanApply(partAlignment)
                ? partAlignment.SocketEuler
                : fallbackEuler;
        }

        private static Vector3 ResolveFirepowerEuler(GaragePanelCatalog.PartAlignment firepowerAlignment)
        {
            if (!CanApply(firepowerAlignment))
                return FallbackWeaponEuler;

            return firepowerAlignment.SocketEuler + FirepowerEulerCorrection;
        }

        private static bool TryResolveFrameTopSocket(
            GaragePanelCatalog.PartAlignment alignment,
            Vector3 framePosition,
            out Vector3 frameSocket)
        {
            if (alignment.HasFrameTopSocket)
            {
                frameSocket = framePosition + alignment.FrameTopSocketOffset;
                return true;
            }

            frameSocket = default;
            return false;
        }

        private static bool CanApply(GaragePanelCatalog.PartAlignment alignment)
        {
            return alignment != null && alignment.CanApply;
        }
    }
}
