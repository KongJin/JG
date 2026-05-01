using Features.Garage.Runtime;
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
                    firepowerEuler),
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
            Vector3 partEuler)
        {
            var framePosition = ResolveFramePosition(frameAlignment);
            var frameSocket = framePosition + frameAlignment.FrameTopSocketOffset;
            var rotatedSocketOffset = Quaternion.Euler(partEuler) * partAlignment.SocketOffset;
            return frameSocket - rotatedSocketOffset;
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
