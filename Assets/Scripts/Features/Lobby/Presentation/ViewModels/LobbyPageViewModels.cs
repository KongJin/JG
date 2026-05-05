using System;
using System.Collections.Generic;
using Features.Lobby.Domain;
using Shared.Kernel;
using Shared.Localization;

namespace Features.Lobby.Presentation
{
    internal sealed class LobbyRoomListViewModel
    {
        public static readonly LobbyRoomListViewModel Empty = new(
            "열린 방 0개",
            Array.Empty<LobbyRoomRowViewModel>());

        public LobbyRoomListViewModel(
            string countText,
            IReadOnlyList<LobbyRoomRowViewModel> rows,
            string emptyText = "열린 방이 없습니다.")
        {
            CountText = string.IsNullOrWhiteSpace(countText) ? "열린 방 0개" : countText;
            Rows = rows ?? Array.Empty<LobbyRoomRowViewModel>();
            EmptyText = string.IsNullOrWhiteSpace(emptyText) ? "열린 방이 없습니다." : emptyText;
        }

        public string CountText { get; }
        public IReadOnlyList<LobbyRoomRowViewModel> Rows { get; }
        public string EmptyText { get; }
    }

    internal readonly struct LobbyRoomRowViewModel
    {
        public LobbyRoomRowViewModel(
            DomainEntityId roomId,
            string titleText,
            string metaText,
            string statusText,
            bool canJoin,
            bool isSelected,
            int filledSlots,
            int totalSlots)
        {
            RoomId = roomId;
            TitleText = titleText ?? string.Empty;
            MetaText = metaText ?? string.Empty;
            StatusText = statusText ?? string.Empty;
            CanJoin = canJoin;
            IsSelected = isSelected;
            FilledSlots = filledSlots < 0 ? 0 : filledSlots;
            TotalSlots = totalSlots < 0 ? 0 : totalSlots;
        }

        public DomainEntityId RoomId { get; }
        public string TitleText { get; }
        public string MetaText { get; }
        public string StatusText { get; }
        public bool CanJoin { get; }
        public bool IsSelected { get; }
        public int FilledSlots { get; }
        public int TotalSlots { get; }
        public string Text => string.IsNullOrWhiteSpace(MetaText)
            ? TitleText
            : $"{TitleText}  {MetaText}";
    }

    internal sealed class LobbyRoomSelectionViewModel
    {
        public static readonly LobbyRoomSelectionViewModel Empty = new(
            default,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            filledSlots: 0,
            totalSlots: 0,
            canJoin: false);

        public LobbyRoomSelectionViewModel(
            DomainEntityId roomId,
            string titleText,
            string metaText,
            string statusText,
            string bodyText,
            int filledSlots,
            int totalSlots,
            bool canJoin,
            string joinButtonText = null)
        {
            RoomId = roomId;
            TitleText = titleText ?? string.Empty;
            MetaText = metaText ?? string.Empty;
            StatusText = statusText ?? string.Empty;
            BodyText = bodyText ?? string.Empty;
            FilledSlots = filledSlots < 0 ? 0 : filledSlots;
            TotalSlots = totalSlots < 0 ? 0 : totalSlots;
            CanJoin = canJoin;
            JoinButtonText = string.IsNullOrWhiteSpace(joinButtonText) ? GameText.Get("lobby.join_room") : joinButtonText;
        }

        public DomainEntityId RoomId { get; }
        public string TitleText { get; }
        public string MetaText { get; }
        public string StatusText { get; }
        public string BodyText { get; }
        public int FilledSlots { get; }
        public int TotalSlots { get; }
        public bool CanJoin { get; }
        public string JoinButtonText { get; }
        public bool IsVisible => !string.IsNullOrWhiteSpace(RoomId.Value);
    }

    internal sealed class LobbyRoomDetailViewModel
    {
        public static readonly LobbyRoomDetailViewModel Empty = new(
            string.Empty,
            string.Empty,
            Array.Empty<string>(),
            localIsReady: false,
            readyButtonText: null,
            canStartGame: false);

        public LobbyRoomDetailViewModel(
            string titleText,
            string metaText,
            IReadOnlyList<string> memberRows,
            bool localIsReady,
            string readyButtonText,
            bool canStartGame,
            bool readyToggleEnabled = true,
            string readyBlockReason = null)
        {
            TitleText = titleText ?? string.Empty;
            MetaText = metaText ?? string.Empty;
            MemberRows = memberRows ?? Array.Empty<string>();
            LocalIsReady = localIsReady;
            ReadyButtonText = string.IsNullOrWhiteSpace(readyButtonText) ? GameText.Get("common.ready") : readyButtonText;
            CanStartGame = canStartGame;
            ReadyToggleEnabled = readyToggleEnabled || localIsReady;
            ReadyBlockReason = readyBlockReason ?? string.Empty;
        }

        public string TitleText { get; }
        public string MetaText { get; }
        public IReadOnlyList<string> MemberRows { get; }
        public bool LocalIsReady { get; }
        public string ReadyButtonText { get; }
        public bool CanStartGame { get; }
        public bool ReadyToggleEnabled { get; }
        public string ReadyBlockReason { get; }
    }

    internal sealed class LobbyRoomWaitingViewModel
    {
        public static readonly LobbyRoomWaitingViewModel Empty = new(
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            Array.Empty<LobbyRoomParticipantViewModel>(),
            LobbyGarageSummaryViewModel.Empty,
            TeamType.None,
            localIsOwner: false,
            localIsReady: false,
            canStartGame: false,
            isVisible: false);

        public LobbyRoomWaitingViewModel(
            string titleText,
            string metaText,
            string stateText,
            string hostText,
            string connectionText,
            IReadOnlyList<LobbyRoomParticipantViewModel> participants,
            LobbyGarageSummaryViewModel deckSummary,
            TeamType localTeam,
            bool localIsOwner,
            bool localIsReady,
            bool canStartGame,
            bool isVisible,
            bool readyToggleEnabled = true,
            string readyBlockReason = null)
        {
            TitleText = titleText ?? string.Empty;
            MetaText = metaText ?? string.Empty;
            StateText = stateText ?? string.Empty;
            HostText = hostText ?? string.Empty;
            ConnectionText = connectionText ?? string.Empty;
            Participants = participants ?? Array.Empty<LobbyRoomParticipantViewModel>();
            DeckSummary = deckSummary ?? LobbyGarageSummaryViewModel.Empty;
            LocalTeam = localTeam;
            LocalIsOwner = localIsOwner;
            LocalIsReady = localIsReady;
            CanStartGame = canStartGame;
            IsVisible = isVisible;
            ReadyToggleEnabled = readyToggleEnabled || localIsReady;
            ReadyBlockReason = readyBlockReason ?? string.Empty;
        }

        public string TitleText { get; }
        public string MetaText { get; }
        public string StateText { get; }
        public string HostText { get; }
        public string ConnectionText { get; }
        public IReadOnlyList<LobbyRoomParticipantViewModel> Participants { get; }
        public LobbyGarageSummaryViewModel DeckSummary { get; }
        public TeamType LocalTeam { get; }
        public bool LocalIsOwner { get; }
        public bool LocalIsReady { get; }
        public bool CanStartGame { get; }
        public bool IsVisible { get; }
        public bool ReadyToggleEnabled { get; }
        public string ReadyBlockReason { get; }
        public string ReadyButtonText => LocalIsReady
            ? "준비 취소"
            : ReadyToggleEnabled
                ? GameText.Get("common.ready")
                : string.IsNullOrWhiteSpace(ReadyBlockReason)
                    ? GameText.Get("common.ready")
                    : ReadyBlockReason;
        public string PrimaryButtonText => ReadyButtonText;
    }

    internal readonly struct LobbyRoomParticipantViewModel
    {
        public LobbyRoomParticipantViewModel(
            string displayNameText,
            string teamText,
            string statusText,
            bool isReady,
            bool isLocal,
            bool isEmpty)
        {
// csharp-guardrails: allow-null-defense
            DisplayNameText = displayNameText ?? string.Empty;
// csharp-guardrails: allow-null-defense
            TeamText = teamText ?? string.Empty;
// csharp-guardrails: allow-null-defense
            StatusText = statusText ?? string.Empty;
            IsReady = isReady;
            IsLocal = isLocal;
            IsEmpty = isEmpty;
        }

        public string DisplayNameText { get; }
        public string TeamText { get; }
        public string StatusText { get; }
        public bool IsReady { get; }
        public bool IsLocal { get; }
        public bool IsEmpty { get; }
    }

    internal sealed class LobbyGarageSummaryViewModel
    {
        public static readonly LobbyGarageSummaryViewModel Empty = new(
            GameText.Get("lobby.deck_waiting"),
            "저장된 유닛 0/8",
            $"유닛 {Features.Garage.Domain.GarageRoster.MinReadySlots}개 이상 저장하면 게임을 시작할 수 있습니다.",
            filledSlots: 0,
            totalSlots: Features.Garage.Domain.GarageRoster.MaxSlots,
            isReady: false);

        public LobbyGarageSummaryViewModel(
            string statusText,
            string summaryText,
            string detailText,
            int filledSlots,
            int totalSlots,
            bool isReady)
        {
            StatusText = statusText ?? string.Empty;
            SummaryText = summaryText ?? string.Empty;
            DetailText = detailText ?? string.Empty;
            FilledSlots = filledSlots < 0 ? 0 : filledSlots;
            TotalSlots = totalSlots < 0 ? 0 : totalSlots;
            IsReady = isReady;
        }

        public string StatusText { get; }
        public string SummaryText { get; }
        public string DetailText { get; }
        public int FilledSlots { get; }
        public int TotalSlots { get; }
        public bool IsReady { get; }
    }

    internal sealed class LobbyAccountViewModel
    {
        public static readonly LobbyAccountViewModel Empty = new(
            "LOCAL PILOT",
            "Google 연결 대기 중",
            "계정 연결 대기 중",
            "로컬",
            "0/5",
            "대기",
            "Google 연결 필요",
            GameText.Get("lobby.deck_waiting"),
            "0/5",
            "정상",
            GameText.Get("common.ready"),
            "80%",
            "100%",
            "로컬 우선",
            GameText.Get("common.waiting"));

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
            new LobbyOperationTraceViewModel("기록 0/5 저장됨", GameText.Get("common.records_empty")));

        public LobbyOperationMemoryViewModel(
            LobbyOperationLatestViewModel latest,
            IReadOnlyList<LobbyOperationRowViewModel> recentRows,
            LobbyOperationTraceViewModel trace)
        {
            Latest = latest ?? LobbyOperationLatestViewModel.Empty;
            RecentRows = recentRows ?? Array.Empty<LobbyOperationRowViewModel>();
            Trace = trace ?? new LobbyOperationTraceViewModel("기록 0/5 저장됨", GameText.Get("common.records_empty"));
        }

        public LobbyOperationLatestViewModel Latest { get; }
        public IReadOnlyList<LobbyOperationRowViewModel> RecentRows { get; }
        public LobbyOperationTraceViewModel Trace { get; }
    }

    internal sealed class LobbyOperationLatestViewModel
    {
        public static readonly LobbyOperationLatestViewModel Empty = new(
            hasRecord: false,
            resultText: GameText.Get("common.records_empty"),
            resultClass: "memory-result",
            timeText: string.Empty,
            survivalText: string.Empty,
            waveText: string.Empty,
            coreText: string.Empty,
            coreClass: "memory-stat-value memory-stat-value--orange",
            killText: string.Empty,
            pressureText: "게임 종료 후 최근 기록이 여기에 표시됩니다.");

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
