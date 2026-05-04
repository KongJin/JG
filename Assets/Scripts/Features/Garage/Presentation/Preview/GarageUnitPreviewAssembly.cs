using Features.Garage.Runtime;
using Features.Unit.Domain;
using UnityEngine;

namespace Features.Garage.Presentation
{
    internal static class GarageUnitPreviewAssembly
    {
        private static readonly Vector3 FirepowerEulerCorrection = new Vector3(0f, 0f, -90f);
        private const float MinVectorSqrMagnitude = 0.000001f;
        private const string XfiWeaponDirectionOnly = "xfi_weapon_direction_only";
        private const string FrameTopSocketAnchorMode = "FrameTopSocket";
        private const string ShoulderPairAnchorMode = "ShoulderPair";

        private static bool HasCompleteLoadout(GarageSlotViewModel viewModel)
        {
            return viewModel?.Preview?.HasCompleteLoadout ?? false;
        }

        /// <summary>
        /// PreviewData를 사용하는 개선된 버전 - ViewModel 의존성 제거
        /// </summary>
        public static bool HasPreviewAssemblyData(GarageSlotPreviewData preview)
        {
            if (preview == null || !preview.HasCompleteLoadout)
                return false;

            return !CanApply(preview.FrameAlignment) ||
                   !CanApply(preview.FirepowerAlignment) ||
                   !CanApply(preview.MobilityAlignment) ||
                   !HasMobilitySocket(preview.MobilityAlignment, preview.MobilityUsesAssemblyPivot)
                ? false
                : HasFrameFirepowerAnchorData(
                    preview.FrameAlignment,
                    preview.FirepowerAlignment,
                    preview.FrameAssemblyForm,
                    preview.FirepowerAssemblyForm);
        }

        public static bool HasPreviewAssemblyData(GarageSlotViewModel viewModel)
        {
            if (!HasCompleteLoadout(viewModel) ||
                !CanApply(viewModel.FrameAlignment) ||
                !CanApply(viewModel.FirepowerAlignment) ||
                !CanApply(viewModel.MobilityAlignment) ||
                !HasMobilitySocket(viewModel.MobilityAlignment, viewModel.MobilityUsesAssemblyPivot))
                return false;

            return HasFrameFirepowerAnchorData(
                viewModel.FrameAlignment,
                viewModel.FirepowerAlignment,
                viewModel.FrameAssemblyForm,
                viewModel.FirepowerAssemblyForm);
        }

        /// <summary>
        /// PreviewData를 사용하는 개선된 버전 - ViewModel 의존성 제거
        /// </summary>
        public static bool TryCreatePreviewRoot(
            GarageSlotPreviewData preview,
            Camera previewCamera,
            GameObject framePrefab,
            GameObject firepowerPrefab,
            GameObject mobilityPrefab,
            out GameObject previewRoot)
        {
            previewRoot = null;
            if (!HasPreviewAssemblyData(preview) ||
                previewCamera == null ||
                framePrefab == null ||
                firepowerPrefab == null ||
                mobilityPrefab == null)
                return false;

            previewRoot = new GameObject("PreviewRoot");
            GaragePreviewAssembler.Attach(
                previewRoot,
                previewCamera.transform,
                new Vector3(0f, -0.04f, 6f),
                Vector3.zero);

            var frameObj = Object.Instantiate(framePrefab);
            frameObj.SetActive(true);
            GaragePreviewAssembler.Attach(
                frameObj,
                previewRoot.transform,
                ResolveFramePosition(preview.FrameAlignment),
                Vector3.zero);

            var weaponObj = Object.Instantiate(firepowerPrefab);
            weaponObj.SetActive(true);
            var firepowerEuler = ResolveFirepowerEuler(preview.FirepowerAlignment);
            GaragePreviewAssembler.Attach(
                weaponObj,
                previewRoot.transform,
                ResolveAttachedPartPosition(
                    preview.FrameAlignment,
                    preview.FirepowerAlignment,
                    firepowerEuler),
                firepowerEuler);

            var mobilityObj = Object.Instantiate(mobilityPrefab);
            mobilityObj.SetActive(true);
            GaragePreviewAssembler.Attach(
                mobilityObj,
                previewRoot.transform,
                ResolveMobilityPosition(
                    preview.FrameAlignment,
                    preview.MobilityAlignment,
                    preview.MobilityUsesAssemblyPivot),
                preview.MobilityAlignment.SocketEuler);

            return true;
        }

        public static bool TryCreatePreviewRoot(
            GarageSlotViewModel viewModel,
            Camera previewCamera,
            GameObject framePrefab,
            GameObject firepowerPrefab,
            GameObject mobilityPrefab,
            out GameObject previewRoot)
        {
            // 하위 호환성을 위한 위임
            return TryCreatePreviewRoot(
                viewModel?.Preview,
                previewCamera,
                framePrefab,
                firepowerPrefab,
                mobilityPrefab,
                out previewRoot);
        }

        public static void SetYaw(GameObject root, float yawDegrees)
        {
            if (root == null)
                return;

            root.transform.localEulerAngles = new Vector3(0f, yawDegrees, 0f);
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
            Vector3 partEuler)
        {
            var framePosition = ResolveFramePosition(frameAlignment);
            var frameSocket = framePosition + ResolveFrameTopSocketOffset(frameAlignment);
            var rotatedSocketOffset = Quaternion.Euler(partEuler) *
                                      ResolveAttachedPartSocketOffset(partAlignment);
            return frameSocket - rotatedSocketOffset;
        }

        private static Vector3 ResolveAttachedPartSocketOffset(GaragePanelCatalog.PartAlignment partAlignment)
        {
            if ((partAlignment.AssemblyAnchorMode == FrameTopSocketAnchorMode ||
                 partAlignment.AssemblyAnchorMode == ShoulderPairAnchorMode) &&
                partAlignment.HasVisualBounds)
            {
                return new Vector3(
                    partAlignment.VisualBoundsCenter.x,
                    partAlignment.VisualBoundsMin.y,
                    partAlignment.VisualBoundsCenter.z);
            }

            return partAlignment.SocketOffset;
        }

        private static Vector3 ResolveFrameTopSocketOffset(GaragePanelCatalog.PartAlignment frameAlignment)
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

            return mobilityAlignment.HasXfiAttachSocket
                ? mobilityAlignment.XfiAttachSocketOffset
                : Vector3.zero;
        }

        private static Vector3 ResolveFirepowerEuler(GaragePanelCatalog.PartAlignment firepowerAlignment)
        {
            return firepowerAlignment.SocketEuler + FirepowerEulerCorrection;
        }

        private static bool HasFrameFirepowerAnchorData(
            GaragePanelCatalog.PartAlignment frameAlignment,
            GaragePanelCatalog.PartAlignment firepowerAlignment,
            AssemblyForm frameAssemblyForm,
            AssemblyForm firepowerAssemblyForm)
        {
            if (firepowerAlignment.AssemblyAnchorMode == FrameTopSocketAnchorMode)
                return frameAlignment.HasFrameTopSocket;

            if (firepowerAlignment.AssemblyAnchorMode == ShoulderPairAnchorMode)
                return frameAlignment.HasFrameTopSocket;

            if (IsDirectionOnlyWeapon(firepowerAlignment))
                return false;

            return frameAlignment.HasFrameTopSocket &&
                   string.IsNullOrWhiteSpace(firepowerAlignment.AssemblyAnchorMode);
        }

        private static bool CanApply(GaragePanelCatalog.PartAlignment alignment)
        {
            return alignment != null && alignment.CanApply;
        }

        private static bool IsDirectionOnlyWeapon(GaragePanelCatalog.PartAlignment alignment)
        {
            return alignment != null && alignment.XfiSocketQuality == XfiWeaponDirectionOnly;
        }

        private static bool HasMobilitySocket(
            GaragePanelCatalog.PartAlignment mobilityAlignment,
            bool useAssemblyPivot)
        {
            return useAssemblyPivot ||
                   mobilityAlignment.HasGxTreeSocket ||
                   mobilityAlignment.SocketOffset.sqrMagnitude > MinVectorSqrMagnitude ||
                   mobilityAlignment.HasXfiAttachSocket;
        }
    }
}
