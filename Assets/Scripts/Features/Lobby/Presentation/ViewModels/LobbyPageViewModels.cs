using System;
using System.Collections.Generic;
using Shared.Kernel;

namespace Features.Lobby.Presentation
{
    internal sealed class LobbyRoomListViewModel
    {
        public static readonly LobbyRoomListViewModel Empty = new(
            "0 open rooms",
            Array.Empty<LobbyRoomRowViewModel>());

        public LobbyRoomListViewModel(
            string countText,
            IReadOnlyList<LobbyRoomRowViewModel> rows,
            string emptyText = "열린 방이 없습니다.")
        {
            CountText = string.IsNullOrWhiteSpace(countText) ? "0 open rooms" : countText;
            Rows = rows ?? Array.Empty<LobbyRoomRowViewModel>();
            EmptyText = string.IsNullOrWhiteSpace(emptyText) ? "열린 방이 없습니다." : emptyText;
        }

        public string CountText { get; }
        public IReadOnlyList<LobbyRoomRowViewModel> Rows { get; }
        public string EmptyText { get; }
    }

    internal readonly struct LobbyRoomRowViewModel
    {
        public LobbyRoomRowViewModel(DomainEntityId roomId, string text, bool isEnabled)
        {
            RoomId = roomId;
            Text = text ?? string.Empty;
            IsEnabled = isEnabled;
        }

        public DomainEntityId RoomId { get; }
        public string Text { get; }
        public bool IsEnabled { get; }
    }

    internal sealed class LobbyRoomDetailViewModel
    {
        public static readonly LobbyRoomDetailViewModel Empty = new(
            string.Empty,
            string.Empty,
            Array.Empty<string>(),
            localIsReady: false,
            readyButtonText: "Ready",
            canStartGame: false);

        public LobbyRoomDetailViewModel(
            string titleText,
            string metaText,
            IReadOnlyList<string> memberRows,
            bool localIsReady,
            string readyButtonText,
            bool canStartGame)
        {
            TitleText = titleText ?? string.Empty;
            MetaText = metaText ?? string.Empty;
            MemberRows = memberRows ?? Array.Empty<string>();
            LocalIsReady = localIsReady;
            ReadyButtonText = string.IsNullOrWhiteSpace(readyButtonText) ? "Ready" : readyButtonText;
            CanStartGame = canStartGame;
        }

        public string TitleText { get; }
        public string MetaText { get; }
        public IReadOnlyList<string> MemberRows { get; }
        public bool LocalIsReady { get; }
        public string ReadyButtonText { get; }
        public bool CanStartGame { get; }
    }

    internal sealed class LobbyAccountViewModel
    {
        public static readonly LobbyAccountViewModel Empty = new(
            "LOCAL PILOT",
            "G-LINK WAIT",
            "UID WAIT",
            "로컬",
            "0/5",
            "대기",
            "Google 연결 필요",
            "편성 대기",
            "0/5",
            "정상",
            "READY",
            "80%",
            "100%",
            "LOCAL FIRST",
            "WAIT");

        public LobbyAccountViewModel(
            string pilotIdText,
            string googleLinkStatusText,
            string uidStatusText,
            string garageSyncStateText,
            string operationSyncStateText,
            string cloudSyncStateText,
            string blockedReasonBodyText,
            string garageSummaryText,
            string operationBufferText,
            string conflictStateText,
            string loadingStateText,
            string bgmValueText,
            string sfxValueText,
            string saveModeText,
            string cloudModeText)
        {
            PilotIdText = pilotIdText ?? string.Empty;
            GoogleLinkStatusText = googleLinkStatusText ?? string.Empty;
            UidStatusText = uidStatusText ?? string.Empty;
            GarageSyncStateText = garageSyncStateText ?? string.Empty;
            OperationSyncStateText = operationSyncStateText ?? string.Empty;
            CloudSyncStateText = cloudSyncStateText ?? string.Empty;
            BlockedReasonBodyText = blockedReasonBodyText ?? string.Empty;
            GarageSummaryText = garageSummaryText ?? string.Empty;
            OperationBufferText = operationBufferText ?? string.Empty;
            ConflictStateText = conflictStateText ?? string.Empty;
            LoadingStateText = loadingStateText ?? string.Empty;
            BgmValueText = bgmValueText ?? string.Empty;
            SfxValueText = sfxValueText ?? string.Empty;
            SaveModeText = saveModeText ?? string.Empty;
            CloudModeText = cloudModeText ?? string.Empty;
        }

        public string PilotIdText { get; }
        public string GoogleLinkStatusText { get; }
        public string UidStatusText { get; }
        public string GarageSyncStateText { get; }
        public string OperationSyncStateText { get; }
        public string CloudSyncStateText { get; }
        public string BlockedReasonBodyText { get; }
        public string GarageSummaryText { get; }
        public string OperationBufferText { get; }
        public string ConflictStateText { get; }
        public string LoadingStateText { get; }
        public string BgmValueText { get; }
        public string SfxValueText { get; }
        public string SaveModeText { get; }
        public string CloudModeText { get; }
    }

    internal sealed class LobbyOperationMemoryViewModel
    {
        public static readonly LobbyOperationMemoryViewModel Empty = new(
            LobbyOperationLatestViewModel.Empty,
            Array.Empty<LobbyOperationRowViewModel>(),
            new LobbyOperationTraceViewModel("0/5 RECORDS STORED", "NO OPERATIONS"));

        public LobbyOperationMemoryViewModel(
            LobbyOperationLatestViewModel latest,
            IReadOnlyList<LobbyOperationRowViewModel> recentRows,
            LobbyOperationTraceViewModel trace)
        {
            Latest = latest ?? LobbyOperationLatestViewModel.Empty;
            RecentRows = recentRows ?? Array.Empty<LobbyOperationRowViewModel>();
            Trace = trace ?? new LobbyOperationTraceViewModel("0/5 RECORDS STORED", "NO OPERATIONS");
        }

        public LobbyOperationLatestViewModel Latest { get; }
        public IReadOnlyList<LobbyOperationRowViewModel> RecentRows { get; }
        public LobbyOperationTraceViewModel Trace { get; }
    }

    internal sealed class LobbyOperationLatestViewModel
    {
        public static readonly LobbyOperationLatestViewModel Empty = new(
            hasRecord: false,
            resultText: "작전 기록 없음",
            resultClass: "memory-result",
            timeText: string.Empty,
            survivalText: string.Empty,
            waveText: string.Empty,
            coreText: string.Empty,
            coreClass: "memory-stat-value memory-stat-value--orange",
            killText: string.Empty,
            pressureText: "전투 종료 후 최근 작전 데이터가 여기에 표시됩니다.");

        public LobbyOperationLatestViewModel(
            bool hasRecord,
            string resultText,
            string resultClass,
            string timeText,
            string survivalText,
            string waveText,
            string coreText,
            string coreClass,
            string killText,
            string pressureText)
        {
            HasRecord = hasRecord;
            ResultText = resultText ?? string.Empty;
            ResultClass = string.IsNullOrWhiteSpace(resultClass) ? "memory-result" : resultClass;
            TimeText = timeText ?? string.Empty;
            SurvivalText = survivalText ?? string.Empty;
            WaveText = waveText ?? string.Empty;
            CoreText = coreText ?? string.Empty;
            CoreClass = string.IsNullOrWhiteSpace(coreClass)
                ? "memory-stat-value memory-stat-value--orange"
                : coreClass;
            KillText = killText ?? string.Empty;
            PressureText = pressureText ?? string.Empty;
        }

        public bool HasRecord { get; }
        public string ResultText { get; }
        public string ResultClass { get; }
        public string TimeText { get; }
        public string SurvivalText { get; }
        public string WaveText { get; }
        public string CoreText { get; }
        public string CoreClass { get; }
        public string KillText { get; }
        public string PressureText { get; }
    }

    internal sealed class LobbyOperationRowViewModel
    {
        public LobbyOperationRowViewModel(
            string rowClass,
            string lineClass,
            string titleText,
            string titleClass,
            string metaText,
            string coreText,
            string coreClass)
        {
            RowClass = string.IsNullOrWhiteSpace(rowClass) ? "operation-row" : rowClass;
            LineClass = string.IsNullOrWhiteSpace(lineClass) ? "operation-row-line" : lineClass;
            TitleText = titleText ?? string.Empty;
            TitleClass = string.IsNullOrWhiteSpace(titleClass) ? "operation-title" : titleClass;
            MetaText = metaText ?? string.Empty;
            CoreText = coreText ?? string.Empty;
            CoreClass = string.IsNullOrWhiteSpace(coreClass) ? "operation-core" : coreClass;
        }

        public string RowClass { get; }
        public string LineClass { get; }
        public string TitleText { get; }
        public string TitleClass { get; }
        public string MetaText { get; }
        public string CoreText { get; }
        public string CoreClass { get; }
    }

    internal sealed class LobbyOperationTraceViewModel
    {
        public LobbyOperationTraceViewModel(string countChipText, string recentDataChipText)
        {
            CountChipText = countChipText ?? string.Empty;
            RecentDataChipText = recentDataChipText ?? string.Empty;
        }

        public string CountChipText { get; }
        public string RecentDataChipText { get; }
    }
}
