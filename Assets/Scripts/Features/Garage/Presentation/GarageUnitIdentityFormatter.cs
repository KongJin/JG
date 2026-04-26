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
        public const string EmptyCallsign = "기체 대기";
        public const string PendingServiceTag = "전적 기록 대기중";

        public static string BuildCallsign(int slotIndex)
        {
            var normalizedSlot = slotIndex < 0 ? 0 : slotIndex;
            return $"A-{normalizedSlot + 1:00}";
        }

        public static string BuildSlotLabel(int slotIndex, bool hasLoadout)
        {
            return hasLoadout ? BuildCallsign(slotIndex) : $"기체 {slotIndex + 1:00}";
        }

        public static string BuildEmptySlotTitle() => "기체 대기";

        public static string BuildEmptyStatusBadge() => "대기";

        public static string BuildDraftStatusBadge(bool hasLoadout) => hasLoadout ? "임시" : "조립중";

        public static string BuildActiveStatusBadge() => "현역";

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

            if (mobility.AnchorRange <= 3.1f)
            {
                if (firepower != null && firepower.Range >= 6f)
                    return "고정 화력";

                return "전선 고정";
            }

            if (mobility.AnchorRange >= 6f)
                return "침투 추적";

            return "균형 지원";
        }

        public static string BuildModuleShorthand(string firepowerName, string mobilityName)
        {
            return $"{CompactPartName(firepowerName)} / {CompactPartName(mobilityName)}";
        }

        public static string BuildLoadoutKey(string frameId, string firepowerId, string mobilityId)
        {
            return $"{NormalizeKeyPart(frameId)}|{NormalizeKeyPart(firepowerId)}|{NormalizeKeyPart(mobilityId)}";
        }

        public static string BuildSlotSummary(
            GaragePanelCatalog.FirepowerOption firepower,
            GaragePanelCatalog.MobilityOption mobility,
            string fallbackFirepowerName = null,
            string fallbackMobilityName = null)
        {
            var role = BuildRoleLabel(firepower, mobility);
            var modules = BuildModuleShorthand(
                firepower?.DisplayName ?? fallbackFirepowerName,
                mobility?.DisplayName ?? fallbackMobilityName);
            return $"{role} | {modules}";
        }

        public static string BuildServiceTagText(GarageUnitServiceTag tag)
        {
            return tag.Kind switch
            {
                GarageUnitServiceTagKind.LongestFrontlineHold => $"최장 전선 유지 {ClampNonNegative(tag.Value)}초",
                GarageUnitServiceTagKind.CoreNearBlocks => $"코어 근접 차단 {ClampNonNegative(tag.Value)}회",
                GarageUnitServiceTagKind.MostRedeployed => "최다 재출격 기체",
                GarageUnitServiceTagKind.CrisisSurvivor => "위기 순간 생존",
                GarageUnitServiceTagKind.RecentOperationContributor => "최근 주요 기여 기체",
                GarageUnitServiceTagKind.Pending => PendingServiceTag,
                _ => PendingServiceTag,
            };
        }

        public static string BuildRosterStatusText(int activeCount, int missingCount, bool readyEligible, bool hasDraftChanges, bool canSave)
        {
            if (readyEligible)
                return $"출격 편성 동기화 | 현역 {activeCount}/6";

            if (hasDraftChanges)
            {
                return canSave
                    ? "기체 편성 갱신 대기 | 저장 가능"
                    : "기체 편성 임시안 | 저장 보류";
            }

            return $"현역 {activeCount}/6 | 기체 +{missingCount} 필요";
        }

        public static string BuildPrimaryActionLabel(GarageDraftEvaluation evaluation)
        {
            if (evaluation.CanSave)
                return "출격 편성 저장";

            if (!evaluation.HasDraftChanges)
                return "현역 편성";

            return "임시 편성";
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

        private static string NormalizeKeyPart(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
        }
    }
}
