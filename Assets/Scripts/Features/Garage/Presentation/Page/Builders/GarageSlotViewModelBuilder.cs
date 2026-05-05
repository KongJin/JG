using System.Collections.Generic;
using Features.Unit.Domain;
using Shared.Gameplay;

namespace Features.Garage.Presentation
{
    internal sealed class GarageSlotViewModelBuilder
    {
        private readonly GaragePanelCatalog _catalog;

        public GarageSlotViewModelBuilder(GaragePanelCatalog catalog)
        {
            _catalog = catalog;
        }

        public IReadOnlyList<GarageSlotViewModel> Build(
            GaragePageState state,
            IReadOnlyDictionary<string, GarageUnitServiceTag> serviceTagsByLoadoutKey = null)
        {
            var slotViewModels = new List<GarageSlotViewModel>(Domain.GarageRoster.MaxSlots);

            for (int i = 0; i < Domain.GarageRoster.MaxSlots; i++)
            {
                var committed = state.CommittedRoster.GetSlot(i);
                var draft = state.DraftRoster.GetSlot(i);
                bool hasCommittedLoadout = committed.IsComplete;
                bool hasDraftLoadout = draft.IsComplete;
                bool hasDraftChanges =
                    committed.frameId != draft.frameId ||
                    committed.firepowerModuleId != draft.firepowerModuleId ||
                    committed.mobilityModuleId != draft.mobilityModuleId;
                bool isEmpty = !draft.HasAnySelection;
                bool hasDraftSelection = draft.HasAnySelection;
                string loadoutKey = hasDraftLoadout
                    ? LoadoutKey.Build(
                        draft.frameId,
                        draft.firepowerModuleId,
                        draft.mobilityModuleId)
                    : null;
                var serviceTag = ResolveServiceTag(loadoutKey, serviceTagsByLoadoutKey);

                string title = GarageUnitIdentityFormatter.BuildEmptySlotTitle();
                string summary = "빈 슬롯";
                string statusBadgeText = GarageUnitIdentityFormatter.BuildEmptyStatusBadge();

                // csharp-guardrails: allow-null-defense
                var frame = hasDraftLoadout ? _catalog?.FindFrame(draft.frameId) : null;
                // csharp-guardrails: allow-null-defense
                var firepower = hasDraftLoadout ? _catalog?.FindFirepower(draft.firepowerModuleId) : null;
                // csharp-guardrails: allow-null-defense
                var mobility = hasDraftLoadout ? _catalog?.FindMobility(draft.mobilityModuleId) : null;
                // csharp-guardrails: allow-null-defense
                var previewFrame = hasDraftSelection ? _catalog?.FindFrame(draft.frameId) : null;
                // csharp-guardrails: allow-null-defense
                var previewFirepower = hasDraftSelection ? _catalog?.FindFirepower(draft.firepowerModuleId) : null;
                // csharp-guardrails: allow-null-defense
                var previewMobility = hasDraftSelection ? _catalog?.FindMobility(draft.mobilityModuleId) : null;

                if (hasDraftLoadout)
                {
                    // csharp-guardrails: allow-null-defense
                    var frameName = frame?.DisplayName ?? draft.frameId;
                    title = GarageUnitIdentityFormatter.BuildTitle(i, frameName, hasDraftLoadout);
                    summary = GarageUnitIdentityFormatter.BuildSlotSummary(
                        firepower,
                        mobility,
                        draft.firepowerModuleId,
                        draft.mobilityModuleId);
                }
                else if (draft.HasAnySelection)
                {
                    title = "조립 중";
                    summary = "조립 중";
                }

                if (hasDraftChanges)
                {
                    statusBadgeText = GarageUnitIdentityFormatter.BuildDraftStatusBadge(hasDraftLoadout);
                    summary = hasDraftLoadout
                        ? GarageUnitIdentityFormatter.BuildSlotSummary(
                            firepower,
                            mobility,
                            draft.firepowerModuleId,
                            draft.mobilityModuleId)
                        : "조립 중";
                }
                else if (hasCommittedLoadout)
                {
                    statusBadgeText = GarageUnitIdentityFormatter.BuildActiveStatusBadge();
                }

                slotViewModels.Add(new GarageSlotViewModel(
                    new GarageSlotDisplayData(
                        GarageUnitIdentityFormatter.BuildSlotLabel(i, hasDraftLoadout),
                        title,
                        summary,
                        statusBadgeText,
                        hasCommittedLoadout,
                        hasDraftChanges,
                        isEmpty,
                        i == state.SelectedSlotIndex,
                        showArrow: i == state.SelectedSlotIndex,
                        callsign: hasDraftLoadout ? GarageUnitIdentityFormatter.BuildCallsign(i) : null,
                        roleLabel: hasDraftLoadout
                            ? GarageUnitIdentityFormatter.BuildRoleLabel(
                                firepower,
                                mobility)
                            : null,
                        serviceTagText: hasDraftLoadout
                            ? GarageUnitIdentityFormatter.BuildServiceTagText(serviceTag)
                            : null),
                    new GarageSlotPreviewData(
                        loadoutKey,
                        // csharp-guardrails: allow-null-defense
                        previewFrame != null ? draft.frameId : null,
                        // csharp-guardrails: allow-null-defense
                        previewFirepower != null ? draft.firepowerModuleId : null,
                        // csharp-guardrails: allow-null-defense
                        previewMobility != null ? draft.mobilityModuleId : null,
                        // csharp-guardrails: allow-null-defense
                        previewFrame?.AssemblyPrefab ?? previewFrame?.PreviewPrefab,
                        // csharp-guardrails: allow-null-defense
                        previewFirepower?.AssemblyPrefab ?? previewFirepower?.PreviewPrefab,
                        // csharp-guardrails: allow-null-defense
                        previewMobility?.AssemblyPrefab ?? previewMobility?.PreviewPrefab,
                        // csharp-guardrails: allow-null-defense
                        previewFrame?.Alignment,
                        // csharp-guardrails: allow-null-defense
                        previewFirepower?.Alignment,
                        // csharp-guardrails: allow-null-defense
                        previewMobility?.Alignment,
                        // csharp-guardrails: allow-null-defense
                        previewMobility?.UseAssemblyPivot ?? false,
                        // csharp-guardrails: allow-null-defense
                        previewFrame?.AssemblyForm ?? AssemblyForm.Unspecified,
                        // csharp-guardrails: allow-null-defense
                        previewFirepower?.AssemblyForm ?? AssemblyForm.Unspecified)));
            }

            return slotViewModels;
        }

        private static GarageUnitServiceTag ResolveServiceTag(
            string loadoutKey,
            IReadOnlyDictionary<string, GarageUnitServiceTag> serviceTagsByLoadoutKey)
        {
            if (string.IsNullOrWhiteSpace(loadoutKey) || serviceTagsByLoadoutKey == null)
                return GarageUnitServiceTag.Pending();

            return serviceTagsByLoadoutKey.TryGetValue(loadoutKey, out var tag)
                ? tag
                : GarageUnitServiceTag.Pending();
        }
    }
}
