using Shared.Localization;

namespace Features.Garage.Presentation
{
    public enum GarageUnitServiceTagKind
    {
        None,
        Pending,
        LongestFrontlineHold,
        CoreNearBlocks,
        MostRedeployed,
        CrisisSurvivor,
        RecentOperationContributor,
    }

    public readonly struct GarageUnitServiceTag
    {
        public GarageUnitServiceTag(GarageUnitServiceTagKind kind, int value)
        {
            Kind = kind;
            Value = value;
        }

        public GarageUnitServiceTagKind Kind { get; }
        public int Value { get; }

        public static GarageUnitServiceTag Pending() => new(GarageUnitServiceTagKind.Pending, 0);
        public static GarageUnitServiceTag LongestFrontlineHold(int seconds) => new(GarageUnitServiceTagKind.LongestFrontlineHold, seconds);
        public static GarageUnitServiceTag CoreNearBlocks(int count) => new(GarageUnitServiceTagKind.CoreNearBlocks, count);
        public static GarageUnitServiceTag MostRedeployed() => new(GarageUnitServiceTagKind.MostRedeployed, 0);
        public static GarageUnitServiceTag CrisisSurvivor() => new(GarageUnitServiceTagKind.CrisisSurvivor, 0);
    }

    public static class GarageUnitIdentityFormatter
    {
        private const float FrontlineHoldMoveRangeMax = 3.1f;
        private const float LongRangeFirepowerMinRange = 6f;
        private const float InfiltrationMoveRangeMin = 6f;

        public const string EmptyCallsign = "유닛 대기";
        public const string PendingServiceTag = "최근 기록 없음";

        public static string BuildCallsign(int slotIndex)
        {
            var normalizedSlot = slotIndex < 0 ? 0 : slotIndex;
            return $"A-{normalizedSlot + 1:00}";
        }

        public static string BuildSlotLabel(int slotIndex, bool hasLoadout)
        {
            return hasLoadout ? BuildCallsign(slotIndex) : $"유닛 {slotIndex + 1:00}";
        }

        public static string BuildEmptySlotTitle() => GameText.Get("garage.unit_waiting");

        public static string BuildEmptyStatusBadge() => GameText.Get("garage.status_waiting");

        public static string BuildDraftStatusBadge(bool hasLoadout) => hasLoadout ? GameText.Get("garage.status_draft") : "조립 중";

        public static string BuildActiveStatusBadge() => GameText.Get("garage.status_saved");

        public static string BuildTitle(int slotIndex, string frameName, bool hasLoadout)
        {
            if (!hasLoadout)
                return "EMPTY";

            return $"{BuildCallsign(slotIndex)} {CompactPartName(frameName)}";
        }

        public static string BuildRoleLabel(
            GaragePanelCatalog.FirepowerOption firepower,
            GaragePanelCatalog.MobilityOption mobility)
        {
            if (mobility == null)
                return "역할 산출 대기";

            if (mobility.MoveRange <= FrontlineHoldMoveRangeMax)
            {
                if (firepower != null && firepower.Range >= LongRangeFirepowerMinRange)
                    return GameText.Get("garage.fixed_firepower");

                return "안정형";
            }

            if (mobility.MoveRange >= InfiltrationMoveRangeMin)
                return "빠른 추적";

            return "균형 지원";
        }

        public static string BuildModuleShorthand(string firepowerName, string mobilityName)
        {
            return $"{CompactPartName(firepowerName)} / {CompactPartName(mobilityName)}";
        }

        public static string BuildSlotSummary(
            GaragePanelCatalog.FirepowerOption firepower,
            GaragePanelCatalog.MobilityOption mobility,
            string firepowerNameWhenCatalogMissing = null,
            string mobilityNameWhenCatalogMissing = null)
        {
            var role = BuildRoleLabel(firepower, mobility);
// csharp-guardrails: allow-null-defense
            var firepowerDisplayName = firepower?.DisplayName ?? firepowerNameWhenCatalogMissing;
// csharp-guardrails: allow-null-defense
            var mobilityDisplayName = mobility?.DisplayName ?? mobilityNameWhenCatalogMissing;
            var modules = BuildModuleShorthand(
                firepowerDisplayName,
                mobilityDisplayName);
            return $"{role} | {modules}";
        }

        public static string BuildServiceTagText(GarageUnitServiceTag tag)
        {
            return tag.Kind switch
            {
                GarageUnitServiceTagKind.LongestFrontlineHold => $"가장 오래 버틴 유닛 {ClampNonNegative(tag.Value)}초",
                GarageUnitServiceTagKind.CoreNearBlocks => $"코어 근접 차단 {ClampNonNegative(tag.Value)}회",
                GarageUnitServiceTagKind.MostRedeployed => GameText.Get("garage.most_reused_unit"),
                GarageUnitServiceTagKind.CrisisSurvivor => "위기 순간 생존",
                GarageUnitServiceTagKind.RecentOperationContributor => GameText.Get("garage.recent_contributor_unit"),
                GarageUnitServiceTagKind.Pending => PendingServiceTag,
                _ => PendingServiceTag,
            };
        }

        public static string BuildRosterStatusText(int activeCount, int missingCount, bool readyEligible, bool hasDraftChanges, bool canSave)
        {
            if (readyEligible)
                return GameText.Format("garage.deck_sync_status", activeCount, Domain.GarageRoster.MaxSlots);

            if (hasDraftChanges)
            {
                return canSave
                    ? GameText.Get("garage.deck_update_ready")
                    : GameText.Get("garage.deck_update_pending");
            }

            return GameText.Format("garage.deck_missing_units", activeCount, Domain.GarageRoster.MinReadySlots, missingCount);
        }

        public static string BuildPrimaryActionLabel(GarageDraftEvaluation evaluation)
        {
            if (evaluation.CanSave)
                return GameText.Get("garage.deck_save");

            if (!evaluation.HasDraftChanges)
                return GameText.Get("garage.deck_saved");

            return GameText.Get("garage.deck_draft");
        }

        public static string CompactPartName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Unknown";

            int separatorIndex = value.IndexOf(' ');
            if (separatorIndex > 0)
                return value[..separatorIndex];

            return value;
        }

        private static int ClampNonNegative(int value) => value < 0 ? 0 : value;
    }
}
