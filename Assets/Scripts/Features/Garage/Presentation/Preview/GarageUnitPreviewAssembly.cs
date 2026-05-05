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
// csharp-guardrails: allow-null-defense
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

        public static bool HasCurrentSelectionPreviewData(GarageSlotPreviewData preview)
        {
            if (preview == null)
                return false;

            if (preview.HasCompleteLoadout)
                return HasPreviewAssemblyData(preview);

            // csharp-guardrails: allow-null-defense
            return preview.FramePreviewPrefab != null ||
                   // csharp-guardrails: allow-null-defense
                   preview.FirepowerPreviewPrefab != null ||
                   // csharp-guardrails: allow-null-defense
                   preview.MobilityPreviewPrefab != null;
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
                preview.MobilityAlignment.Socket.Euler);

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

        public static bool TryCreateCurrentSelectionPreviewRoot(
            GarageSlotPreviewData preview,
            Camera previewCamera,
            out GameObject previewRoot)
        {
            previewRoot = null;
            if (previewCamera == null || !HasCurrentSelectionPreviewData(preview))
                return false;

            if (HasPreviewAssemblyData(preview))
            {
                return TryCreatePreviewRoot(
                    preview,
                    previewCamera,
                    preview.FramePreviewPrefab,
                    preview.FirepowerPreviewPrefab,
                    preview.MobilityPreviewPrefab,
                    out previewRoot);
            }

            if (preview.HasCompleteLoadout)
                return false;

            previewRoot = new GameObject("PreviewRoot");
            GaragePreviewAssembler.Attach(
                previewRoot,
                previewCamera.transform,
                new Vector3(0f, -0.04f, 6f),
                Vector3.zero);

            int standaloneCount = CountStandaloneParts(preview);
            int standaloneIndex = 0;
            // csharp-guardrails: allow-null-defense
            bool hasFramePreviewPrefab = preview.FramePreviewPrefab != null;
            bool canUseFrameAsAnchor = hasFramePreviewPrefab && CanApply(preview.FrameAlignment);

            if (hasFramePreviewPrefab)
            {
                var frameObj = Object.Instantiate(preview.FramePreviewPrefab);
                frameObj.SetActive(true);
                GaragePreviewAssembler.Attach(
                    frameObj,
                    previewRoot.transform,
                    canUseFrameAsAnchor
                        ? ResolveFramePosition(preview.FrameAlignment)
                        : ResolveStandalonePartPosition(standaloneCount, standaloneIndex++),
                    Vector3.zero);
            }

            // csharp-guardrails: allow-null-defense
            if (preview.FirepowerPreviewPrefab != null)
            {
                var firepowerObj = Object.Instantiate(preview.FirepowerPreviewPrefab);
                firepowerObj.SetActive(true);
                bool canAttachFirepower =
                    canUseFrameAsAnchor &&
                    CanApply(preview.FirepowerAlignment) &&
                    HasFrameFirepowerAnchorData(
                        preview.FrameAlignment,
                        preview.FirepowerAlignment,
                        preview.FrameAssemblyForm,
                        preview.FirepowerAssemblyForm);
                var firepowerEuler = CanApply(preview.FirepowerAlignment)
                    ? ResolveFirepowerEuler(preview.FirepowerAlignment)
                    : FirepowerEulerCorrection;
                GaragePreviewAssembler.Attach(
                    firepowerObj,
                    previewRoot.transform,
                    canAttachFirepower
                        ? ResolveAttachedPartPosition(
                            preview.FrameAlignment,
                            preview.FirepowerAlignment,
                            firepowerEuler)
                        : ResolveStandalonePartPosition(standaloneCount, standaloneIndex++),
                    firepowerEuler);
            }

            // csharp-guardrails: allow-null-defense
            if (preview.MobilityPreviewPrefab != null)
            {
                var mobilityObj = Object.Instantiate(preview.MobilityPreviewPrefab);
                mobilityObj.SetActive(true);
                bool canAttachMobility =
                    canUseFrameAsAnchor &&
                    CanApply(preview.MobilityAlignment) &&
                    HasMobilitySocket(preview.MobilityAlignment, preview.MobilityUsesAssemblyPivot);
                GaragePreviewAssembler.Attach(
                    mobilityObj,
                    previewRoot.transform,
                    canAttachMobility
                        ? ResolveMobilityPosition(
                            preview.FrameAlignment,
                            preview.MobilityAlignment,
                            preview.MobilityUsesAssemblyPivot)
                        : ResolveStandalonePartPosition(standaloneCount, standaloneIndex++),
                    CanApply(preview.MobilityAlignment)
                        ? preview.MobilityAlignment.Socket.Euler
                        : Vector3.zero);
            }

            return previewRoot.transform.childCount > 0;
        }

        public static void SetYaw(GameObject root, float yawDegrees)
        {
            if (root == null)
                return;

            root.transform.localEulerAngles = new Vector3(0f, yawDegrees, 0f);
        }

        private static Vector3 ResolveFramePosition(GaragePanelCatalog.PartAlignment frameAlignment)
        {
            var visualBounds = frameAlignment.VisualBounds;
            return visualBounds.HasBounds
                ? visualBounds.PivotOffset - visualBounds.Center
                : visualBounds.PivotOffset;
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
            var assembly = partAlignment.Assembly;
            var visualBounds = partAlignment.VisualBounds;

            if ((assembly.AnchorMode == FrameTopSocketAnchorMode ||
                 assembly.AnchorMode == ShoulderPairAnchorMode) &&
                visualBounds.HasBounds)
            {
                return new Vector3(
                    visualBounds.Center.x,
                    visualBounds.Min.y,
                    visualBounds.Center.z);
            }

            return partAlignment.Socket.Offset;
        }

        private static Vector3 ResolveFrameTopSocketOffset(GaragePanelCatalog.PartAlignment frameAlignment)
        {
            var socket = frameAlignment.Socket;
            if (socket.FrameTopSocketOffset.sqrMagnitude > MinVectorSqrMagnitude)
                return socket.FrameTopSocketOffset;

            return socket.Offset;
        }

        private static Vector3 ResolveMobilityPosition(
            GaragePanelCatalog.PartAlignment frameAlignment,
            GaragePanelCatalog.PartAlignment mobilityAlignment,
            bool useAssemblyPivot)
        {
            var framePosition = ResolveFramePosition(frameAlignment);
            var rotatedSocketOffset = Quaternion.Euler(mobilityAlignment.Socket.Euler) *
                                      ResolveMobilitySocketOffset(mobilityAlignment, useAssemblyPivot);
            return framePosition - rotatedSocketOffset;
        }

        private static Vector3 ResolveMobilitySocketOffset(
            GaragePanelCatalog.PartAlignment mobilityAlignment,
            bool useAssemblyPivot)
        {
            if (useAssemblyPivot)
                return Vector3.zero;

            var socket = mobilityAlignment.Socket;
            if (socket.HasGxTreeSocket)
                return socket.GxTreeSocketOffset;

            if (socket.Offset.sqrMagnitude > MinVectorSqrMagnitude)
                return socket.Offset;

            var xfi = mobilityAlignment.Xfi;
            return xfi.HasAttachSocket
                ? xfi.AttachSocketOffset
                : Vector3.zero;
        }

        private static Vector3 ResolveFirepowerEuler(GaragePanelCatalog.PartAlignment firepowerAlignment)
        {
            return firepowerAlignment.Socket.Euler + FirepowerEulerCorrection;
        }

        private static int CountStandaloneParts(GarageSlotPreviewData preview)
        {
            if (preview == null)
                return 0;

            int count = 0;
            // csharp-guardrails: allow-null-defense
            if (preview.FramePreviewPrefab != null)
                count++;
            // csharp-guardrails: allow-null-defense
            if (preview.FirepowerPreviewPrefab != null)
                count++;
            // csharp-guardrails: allow-null-defense
            if (preview.MobilityPreviewPrefab != null)
                count++;

            return count;
        }

        private static Vector3 ResolveStandalonePartPosition(int partCount, int partIndex)
        {
            if (partCount <= 1)
                return Vector3.zero;

            const float spacing = 0.72f;
            float centerOffset = (partCount - 1) * 0.5f;
            return new Vector3((partIndex - centerOffset) * spacing, 0f, 0f);
        }

        private static bool HasFrameFirepowerAnchorData(
            GaragePanelCatalog.PartAlignment frameAlignment,
            GaragePanelCatalog.PartAlignment firepowerAlignment,
            AssemblyForm frameAssemblyForm,
            AssemblyForm firepowerAssemblyForm)
        {
            var firepowerAssembly = firepowerAlignment.Assembly;
            var frameSocket = frameAlignment.Socket;

            if (firepowerAssembly.AnchorMode == FrameTopSocketAnchorMode)
                return frameSocket.HasFrameTopSocket;

            if (firepowerAssembly.AnchorMode == ShoulderPairAnchorMode)
                return frameSocket.HasFrameTopSocket;

            if (IsDirectionOnlyWeapon(firepowerAlignment))
                return false;

            return frameSocket.HasFrameTopSocket &&
                   string.IsNullOrWhiteSpace(firepowerAssembly.AnchorMode);
        }

        private static bool CanApply(GaragePanelCatalog.PartAlignment alignment)
        {
            return alignment != null && alignment.CanApply;
        }

        private static bool IsDirectionOnlyWeapon(GaragePanelCatalog.PartAlignment alignment)
        {
            return alignment != null && alignment.Xfi.SocketQuality == XfiWeaponDirectionOnly;
        }

        private static bool HasMobilitySocket(
            GaragePanelCatalog.PartAlignment mobilityAlignment,
            bool useAssemblyPivot)
        {
            var socket = mobilityAlignment.Socket;
            var xfi = mobilityAlignment.Xfi;
            return useAssemblyPivot ||
                   socket.HasGxTreeSocket ||
                   socket.Offset.sqrMagnitude > MinVectorSqrMagnitude ||
                   xfi.HasAttachSocket;
        }

    }
}
