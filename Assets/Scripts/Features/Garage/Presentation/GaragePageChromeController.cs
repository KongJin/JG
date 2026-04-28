using UnityEngine;

namespace Features.Garage.Presentation
{
    internal sealed class GaragePageChromeController
    {
        public bool IsSettingsOverlayOpen { get; private set; }
        public bool IsSaving { get; private set; }
        public int SelectedSlotIndex { get; private set; }
        public int CommittedRosterCount { get; private set; }
        public string HeaderSummary { get; private set; } = string.Empty;
        public string SaveStateText { get; private set; } = string.Empty;

        public void ApplyState(
            Transform pageRoot,
            bool isSettingsOverlayOpen,
            bool isSaving,
            bool isEditActive,
            bool isFirepowerActive,
            bool isSummaryActive,
            int selectedSlotIndex,
            int committedRosterCount,
            GarageResultViewModel resultViewModel)
        {
            IsSettingsOverlayOpen = isSettingsOverlayOpen;
            IsSaving = isSaving;
            SelectedSlotIndex = selectedSlotIndex;
            CommittedRosterCount = committedRosterCount;
            HeaderSummary = BuildHeaderSummary(
                selectedSlotIndex,
                committedRosterCount,
                isEditActive,
                isFirepowerActive,
                resultViewModel);
            SaveStateText = BuildSaveStateText(resultViewModel, isSaving);
        }

        public void HideSettingsOverlay()
        {
            IsSettingsOverlayOpen = false;
        }

        internal static string ExtractOperationSummary(string statsText)
        {
            if (string.IsNullOrWhiteSpace(statsText))
                return null;

            var lines = statsText.Split('\n');
            for (var i = lines.Length - 1; i >= 0; i--)
            {
                var line = lines[i].Trim();
                if (line.StartsWith("작전 ", System.StringComparison.Ordinal))
                    return line;
            }

            return null;
        }

        private static string BuildHeaderSummary(
            int selectedSlotIndex,
            int committedRosterCount,
            bool isEditActive,
            bool isFirepowerActive,
            GarageResultViewModel resultViewModel)
        {
            string focusSummary = isEditActive ? "프레임 포커스" : isFirepowerActive ? "무장 포커스" : "기동 포커스";
            string readySummary = resultViewModel == null
                ? $"현역 {committedRosterCount}/6"
                : resultViewModel.IsReady
                    ? "출격 편성 최신"
                    : resultViewModel.IsDirty
                        ? resultViewModel.CanSave ? "저장 가능" : "조립 진행 중"
                        : $"현역 {committedRosterCount}/6";

            return $"{GarageUnitIdentityFormatter.BuildSlotLabel(selectedSlotIndex, hasLoadout: true)} | {focusSummary} | {readySummary}";
        }

        private static string BuildSaveStateText(GarageResultViewModel resultViewModel, bool isSaving)
        {
            if (isSaving)
                return "빌드 동기화 중...";

            if (resultViewModel == null)
                return string.Empty;

            var operationSummary = ExtractOperationSummary(resultViewModel.StatsText);
            if (!string.IsNullOrWhiteSpace(operationSummary))
                return operationSummary;

            if (resultViewModel.IsDirty && resultViewModel.CanSave)
                return "현재 조합을 저장해 출격 편성에 반영";

            if (resultViewModel.IsReady)
                return "저장본이 최신입니다 | 룸 패널에서 바로 출격 가능";

            return resultViewModel.ValidationText ?? string.Empty;
        }
    }
}
