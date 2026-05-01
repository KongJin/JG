using System;
using System.Collections.Generic;
using Features.Account.Application;
using Features.Account.Domain;
using Features.Garage.Presentation;
using Features.Lobby.Application.Events;
using Features.Lobby.Application.Ports;
using Features.Lobby.Domain;
using Features.Player.Domain;
using Shared.Kernel;
using UnityEngine;
using UnityEngine.UIElements;

namespace Features.Lobby.Presentation
{
    internal sealed class LobbyUitkRuntimeAdapter
    {
        private readonly UIDocument _document;
        private readonly UIDocument _garageDocument;
        private readonly GarageSetBUitkRuntimeAdapter _garageAdapter;
        private readonly VisualTreeAsset _lobbyShellTree;
        private readonly VisualTreeAsset _operationMemoryTree;
        private readonly VisualTreeAsset _accountSyncTree;
        private readonly VisualTreeAsset _connectionReconnectTree;
        private readonly UnityEngine.Object _logContext;

        private VisualElement _root;
        private VisualElement _lobbyPage;
        private VisualElement _garagePage;
        private VisualElement _recordsPage;
        private VisualElement _accountPage;
        private VisualElement _connectionPage;
        private VisualElement _roomList;
        private VisualElement _memberList;
        private Label _shellTitle;
        private Label _shellState;
        private Label _roomCountLabel;
        private Label _roomDetailTitle;
        private Label _roomDetailMeta;
        private TextField _roomNameInput;
        private TextField _displayNameInput;
        private IntegerField _capacityInput;
        private IntegerField _difficultyInput;
        private Button _readyButton;
        private Button _startButton;
        private Button _lobbyNav;
        private Button _garageNav;
        private Button _recordsNav;

        public LobbyUitkRuntimeAdapter(
            UIDocument document,
            UIDocument garageDocument,
            GarageSetBUitkRuntimeAdapter garageAdapter,
            VisualTreeAsset lobbyShellTree,
            VisualTreeAsset operationMemoryTree,
            VisualTreeAsset accountSyncTree,
            VisualTreeAsset connectionReconnectTree,
            UnityEngine.Object logContext)
        {
            _document = document;
            _garageDocument = garageDocument;
            _garageAdapter = garageAdapter;
            _lobbyShellTree = lobbyShellTree;
            _operationMemoryTree = operationMemoryTree;
            _accountSyncTree = accountSyncTree;
            _connectionReconnectTree = connectionReconnectTree;
            _logContext = logContext;
        }

        public event Action CreateRoomRequested;
        public event Action<DomainEntityId> RoomSelected;
        public event Action LeaveRoomRequested;
        public event Action<TeamType> TeamChangeRequested;
        public event Action ReadyToggled;
        public event Action GameStartRequested;
        public event Action LobbyPageRequested;
        public event Action GaragePageRequested;
        public event Action RecordsPageRequested;
        public event Action AccountPageRequested;
        public event Action ConnectionPageRequested;
        public event Action AccountRefreshRequested;

        public string DisplayNameText => _displayNameInput?.value ?? string.Empty;

        public LobbyCreateRoomInput CreateRoomInput => new LobbyCreateRoomInput(
            _roomNameInput?.value ?? "Room",
            Mathf.Max(1, _capacityInput?.value ?? 4),
            DisplayNameText,
            Mathf.Max(0, _difficultyInput?.value ?? 0));

        public bool Bind()
        {
            if (_root != null)
                return true;

            if (_document == null)
                return false;

            _root = _document.rootVisualElement;
            if (_root == null)
                return false;

            if (_root.Q<VisualElement>("LobbyShellScreen") == null && _lobbyShellTree != null)
            {
                _root.Clear();
                _lobbyShellTree.CloneTree(_root);
            }

            if (_root.Q<VisualElement>("LobbyShellScreen") == null)
            {
                Debug.LogError(
                    "[LobbyView] LobbyShellScreen is missing. Assign LobbyShell UXML; runtime generated UI is not available.",
                    _logContext);
                _root = null;
                return false;
            }

            BindAuthoredTree();
            return true;
        }

        public void RenderRooms(IReadOnlyList<RoomSnapshot> rooms)
        {
            if (!Bind())
                return;

            _roomList?.Clear();
            var count = rooms?.Count ?? 0;
            if (_roomCountLabel != null)
                _roomCountLabel.text = count == 1 ? "1 open room" : $"{count} open rooms";

            if (rooms == null || rooms.Count == 0)
            {
                _roomList?.Add(LobbyUitkElements.Label("열린 방이 없습니다.", "uitk-body"));
                return;
            }

            for (var i = 0; i < rooms.Count; i++)
            {
                var room = rooms[i];
                var row = new Button(() => RoomSelected?.Invoke(room.Id))
                {
                    text = $"{room.Name}  {room.Members.Count}/{room.Capacity}  {DifficultyPresetFormatter.ToShortLabel(room.DifficultyPresetId)}"
                };
                row.AddToClassList("uitk-list-row");
                row.SetEnabled(room.Members.Count < room.Capacity);
                _roomList.Add(row);
            }
        }

        public void RenderRooms(IReadOnlyList<RoomListItem> rooms)
        {
            if (!Bind())
                return;

            _roomList?.Clear();
            var count = rooms?.Count ?? 0;
            if (_roomCountLabel != null)
                _roomCountLabel.text = count == 1 ? "1 open room" : $"{count} open rooms";

            if (rooms == null || rooms.Count == 0)
            {
                _roomList?.Add(LobbyUitkElements.Label("열린 방이 없습니다.", "uitk-body"));
                return;
            }

            for (var i = 0; i < rooms.Count; i++)
            {
                var room = rooms[i];
                var row = new Button(() => RoomSelected?.Invoke(room.RoomId))
                {
                    text = $"{room.RoomName}  {room.PlayerCount}/{room.MaxPlayers}  {DifficultyPresetFormatter.ToShortLabel(room.DifficultyPresetId)}"
                };
                row.AddToClassList("uitk-list-row");
                row.SetEnabled(room.PlayerCount < room.MaxPlayers);
                _roomList.Add(row);
            }
        }

        public bool RenderRoomDetail(RoomSnapshot room, DomainEntityId localMemberId)
        {
            if (!Bind())
                return false;

            if (_roomDetailTitle != null)
                _roomDetailTitle.text = room.Name;
            if (_roomDetailMeta != null)
                _roomDetailMeta.text = $"{room.Members.Count}/{room.Capacity} | {DifficultyPresetFormatter.ToShortLabel(room.DifficultyPresetId)}";

            _memberList?.Clear();
            var localIsReady = false;
            foreach (var member in room.Members)
            {
                if (member.Id.Equals(localMemberId))
                    localIsReady = member.IsReady;
                var state = member.IsReady ? "READY" : "WAIT";
                _memberList?.Add(LobbyUitkElements.Label(
                    $"{member.DisplayName} | {member.Team} | {state}",
                    "uitk-list-row-label"));
            }

            if (_readyButton != null)
                _readyButton.text = localIsReady ? "Cancel" : "Ready";
            if (_startButton != null)
                _startButton.SetEnabled(room.OwnerId.Equals(localMemberId));

            return localIsReady;
        }

        public void ShowLobbyPage()
        {
            if (!Bind())
                return;

            SetPageVisibility(lobby: true, garage: false, records: false, account: false, connection: false);
            SetGarageDocumentVisible(false);
            SetNavState(_lobbyNav);
            SetShell("로비", "동기화 대기");
        }

        public void ShowGaragePage()
        {
            if (!Bind())
                return;

            EnsureGarageSurface();
            SetPageVisibility(lobby: false, garage: true, records: false, account: false, connection: false);
            SetGarageDocumentVisible(false);
            SetNavState(_garageNav);
            SetShell("차고", "출격 편성 동기화");
        }

        public void ShowRecordsPage()
        {
            if (!Bind())
                return;

            EnsureRecordsSurface();
            SetPageVisibility(lobby: false, garage: false, records: true, account: false, connection: false);
            SetGarageDocumentVisible(false);
            SetNavState(_recordsNav);
            SetShell("기록", "LOCAL LOG / SYNC PENDING");
        }

        public void ShowAccountPage()
        {
            if (!Bind())
                return;

            EnsureAccountSurface();
            SetPageVisibility(lobby: false, garage: false, records: false, account: true, connection: false);
            SetGarageDocumentVisible(false);
            SetNavState(null);
            SetShell("계정", "NOVA_SYS / CFG.17");
        }

        public void ShowConnectionPage()
        {
            if (!Bind())
                return;

            EnsureConnectionSurface();
            SetPageVisibility(lobby: false, garage: false, records: false, account: false, connection: true);
            SetGarageDocumentVisible(false);
            SetNavState(null);
            SetShell("연결", "SESSION CHECK");
        }

        public void RenderAccountState(
            AccountProfile profile,
            AccountData accountData,
            int operationCount)
        {
            if (!Bind())
                return;

            EnsureAccountSurface();

            var displayName = string.IsNullOrWhiteSpace(profile?.displayName)
                ? "LOCAL PILOT"
                : profile.displayName.Trim();
            var authType = string.IsNullOrWhiteSpace(profile?.authType)
                ? "LOCAL"
                : profile.authType.Trim().ToUpperInvariant();
            var uidText = string.IsNullOrWhiteSpace(profile?.uid)
                ? "UID WAIT"
                : $"UID {Shorten(profile.uid)}";
            var garageCount = accountData?.GarageRoster?.Count ?? 0;
            var settings = accountData?.Settings;

            LobbyUitkElements.SetText(_accountPage, "PilotIdLabel", displayName);
            LobbyUitkElements.SetText(_accountPage, "GoogleLinkStatusLabel", authType == "GOOGLE" ? "G-LINK OK" : "G-LINK WAIT");
            LobbyUitkElements.SetText(_accountPage, "UidStatusLabel", uidText);
            LobbyUitkElements.SetText(_accountPage, "GarageSyncStateLabel", garageCount > 0 ? $"{garageCount}/4" : "로컬");
            LobbyUitkElements.SetText(_accountPage, "OperationSyncStateLabel", $"{operationCount}/5");
            LobbyUitkElements.SetText(_accountPage, "CloudSyncStateLabel", authType == "GOOGLE" ? "준비" : "대기");
            LobbyUitkElements.SetText(_accountPage, "BlockedReasonBodyLabel", authType == "GOOGLE" ? "동기화 가능" : "Google 연결 필요");
            LobbyUitkElements.SetText(_accountPage, "GarageSummaryLabel", garageCount > 0 ? $"편성 {garageCount}기" : "편성 대기");
            LobbyUitkElements.SetText(_accountPage, "OperationBufferLabel", $"{operationCount}/5");
            LobbyUitkElements.SetText(_accountPage, "ConflictStateLabel", "정상");
            LobbyUitkElements.SetText(_accountPage, "LoadingStateLabel", "READY");
            LobbyUitkElements.SetText(_accountPage, "BgmValueLabel", $"{Mathf.RoundToInt((settings?.bgmVolume ?? 0.8f) * 100f)}%");
            LobbyUitkElements.SetText(_accountPage, "SfxValueLabel", $"{Mathf.RoundToInt((settings?.sfxVolume ?? 1f) * 100f)}%");
            LobbyUitkElements.SetText(_accountPage, "SaveModeLabel", "LOCAL FIRST");
            LobbyUitkElements.SetText(_accountPage, "CloudModeLabel", authType == "GOOGLE" ? "READY" : "WAIT");
        }

        public void RenderOperationMemory(RecentOperationRecords records)
        {
            if (!Bind())
                return;

            EnsureRecordsSurface();
            if (_recordsPage == null)
                return;

            records ??= new RecentOperationRecords();
            var list = records.Records;
            RenderLatestOperation(_recordsPage.Q<VisualElement>("LatestOperationCard"), list.Count > 0 ? list[0] : null);
            RenderRecentOperations(_recordsPage.Q<VisualElement>("RecentOperations"), list);
            RenderUnitTrace(_recordsPage.Q<VisualElement>("UnitTrace"), list);
        }

        private void BindAuthoredTree()
        {
            _lobbyPage = _root.Q<VisualElement>("LobbyUitkPage");
            _garagePage = _root.Q<VisualElement>("GarageUitkHost");
            _recordsPage = _root.Q<VisualElement>("RecordsUitkHost");
            _accountPage = _root.Q<VisualElement>("AccountUitkHost");
            _connectionPage = _root.Q<VisualElement>("ConnectionUitkHost");
            _roomList = _root.Q<VisualElement>("RoomList");
            _memberList = _root.Q<VisualElement>("MemberList");
            _shellTitle = _root.Q<Label>("ShellTitleLabel");
            _shellState = _root.Q<Label>("ShellStateLabel");
            _roomCountLabel = _root.Q<Label>("RoomCountLabel");
            _roomDetailTitle = _root.Q<Label>("RoomDetailTitleLabel");
            _roomDetailMeta = _root.Q<Label>("RoomDetailMetaLabel");
            _roomNameInput = _root.Q<TextField>("RoomNameInput");
            _displayNameInput = _root.Q<TextField>("DisplayNameInput");
            _capacityInput = _root.Q<IntegerField>("CapacityInput");
            _difficultyInput = _root.Q<IntegerField>("DifficultyInput");
            _readyButton = _root.Q<Button>("ReadyButton");
            _startButton = _root.Q<Button>("StartButton");
            _lobbyNav = _root.Q<Button>("LobbyNavButton");
            _garageNav = _root.Q<Button>("GarageNavButton");
            _recordsNav = _root.Q<Button>("RecordsNavButton");

            RegisterClick("ShellMenuButton", () => ConnectionPageRequested?.Invoke());
            RegisterClick("ShellSettingsButton", () => AccountPageRequested?.Invoke());
            RegisterClick("CreateRoomButton", () => CreateRoomRequested?.Invoke());
            RegisterClick("GarageSummaryButton", () => GaragePageRequested?.Invoke());
            RegisterClick("RedTeamButton", () => TeamChangeRequested?.Invoke(TeamType.Red));
            RegisterClick("BlueTeamButton", () => TeamChangeRequested?.Invoke(TeamType.Blue));
            RegisterClick("ReadyButton", () => ReadyToggled?.Invoke());
            RegisterClick("StartButton", () => GameStartRequested?.Invoke());
            RegisterClick("LeaveRoomButton", () => LeaveRoomRequested?.Invoke());
            RegisterClick("LobbyNavButton", () => LobbyPageRequested?.Invoke());
            RegisterClick("GarageNavButton", () => GaragePageRequested?.Invoke());
            RegisterClick("RecordsNavButton", () => RecordsPageRequested?.Invoke());

            EnsureRecordsSurface();
            EnsureAccountSurface();
            EnsureConnectionSurface();
            EnsureGarageSurface();
        }

        private bool SetGarageDocumentVisible(bool isVisible)
        {
            if (_garageDocument != null)
            {
                if (_garageAdapter != null && _garageAdapter.SetDocumentRootVisible(isVisible))
                    return isVisible;

                if (!_garageDocument.gameObject.activeSelf)
                    _garageDocument.gameObject.SetActive(true);

                _garageDocument.sortingOrder = 10;
                var root = _garageDocument.rootVisualElement;
                if (root != null)
                    root.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
            }

            return _garageDocument != null && isVisible;
        }

        private void EnsureGarageSurface()
        {
            if (_garagePage == null || _garageAdapter == null)
                return;

            _garageAdapter.BindToHost(_garagePage);
            _garageAdapter.SetDocumentRootVisible(false);
        }

        private void RegisterClick(string buttonName, Action callback)
        {
            var button = _root?.Q<Button>(buttonName);
            if (button != null && callback != null)
                button.clicked += callback;
        }

        private void EnsureRecordsSurface()
        {
            if (_recordsPage == null || _recordsPage.childCount > 0)
                return;

            if (_operationMemoryTree != null)
                _operationMemoryTree.CloneTree(_recordsPage);

            _recordsPage.Q<Button>("BackButton")?.RegisterCallback<ClickEvent>(_ => LobbyPageRequested?.Invoke());
            _recordsPage.Q<Button>("GarageButton")?.RegisterCallback<ClickEvent>(_ => GaragePageRequested?.Invoke());
        }

        private void EnsureAccountSurface()
        {
            if (_accountPage == null || _accountPage.childCount > 0)
                return;

            if (_accountSyncTree != null)
                _accountSyncTree.CloneTree(_accountPage);

            _accountPage.Q<Button>("ManualSyncRetryButton")?.RegisterCallback<ClickEvent>(_ => AccountRefreshRequested?.Invoke());
            _accountPage.Q<Button>("LinkAccountButton")?.RegisterCallback<ClickEvent>(_ => ConnectionPageRequested?.Invoke());
        }

        private void EnsureConnectionSurface()
        {
            if (_connectionPage == null || _connectionPage.childCount > 0)
                return;

            if (_connectionReconnectTree != null)
                _connectionReconnectTree.CloneTree(_connectionPage);

            _connectionPage.Q<Button>("BackButton")?.RegisterCallback<ClickEvent>(_ => LobbyPageRequested?.Invoke());
            _connectionPage.Q<Button>("ReturnLobbyButton")?.RegisterCallback<ClickEvent>(_ => LobbyPageRequested?.Invoke());
            _connectionPage.Q<Button>("ManualRetryButton")?.RegisterCallback<ClickEvent>(_ => LobbyPageRequested?.Invoke());
        }

        private void SetPageVisibility(
            bool lobby,
            bool garage,
            bool records,
            bool account,
            bool connection)
        {
            LobbyUitkElements.SetPage(_lobbyPage, lobby);
            LobbyUitkElements.SetPage(_garagePage, garage);
            LobbyUitkElements.SetPage(_recordsPage, records);
            LobbyUitkElements.SetPage(_accountPage, account);
            LobbyUitkElements.SetPage(_connectionPage, connection);
        }

        private void SetNavState(Button selected)
        {
            LobbyUitkElements.SetSelected(_lobbyNav, selected == _lobbyNav);
            LobbyUitkElements.SetSelected(_garageNav, selected == _garageNav);
            LobbyUitkElements.SetSelected(_recordsNav, selected == _recordsNav);
        }

        private void SetShell(string title, string state)
        {
            if (_shellTitle != null)
                _shellTitle.text = title;
            if (_shellState != null)
                _shellState.text = state;
        }

        private static void RenderLatestOperation(VisualElement card, OperationRecord record)
        {
            if (card == null)
                return;

            card.Clear();
            if (record == null)
            {
                card.Add(LobbyUitkElements.Label("LATEST_OP", "memory-kicker"));
                card.Add(LobbyUitkElements.Label("작전 기록 없음", "memory-result"));
                card.Add(LobbyUitkElements.Label("전투 종료 후 최근 작전 데이터가 여기에 표시됩니다.", "memory-sitrep-text"));
                return;
            }

            var header = new VisualElement();
            header.AddToClassList("memory-card-header");
            var titleStack = new VisualElement();
            titleStack.Add(LobbyUitkElements.Label("LATEST_OP", "memory-kicker"));
            titleStack.Add(LobbyUitkElements.Label(
                ResultText(record),
                record.result == OperationRecordResult.Held
                    ? "memory-result memory-result--held"
                    : "memory-result operation-title--danger"));
            header.Add(titleStack);
            header.Add(LobbyUitkElements.Label(FormatClock(record.endedAtUnixMs), "memory-time"));
            card.Add(header);

            var stats = new VisualElement();
            stats.AddToClassList("memory-stat-grid");
            AddStat(stats, "생존", FormatDuration(record.survivalSeconds), "memory-stat-value memory-stat-value--blue");
            AddStat(stats, "공세", record.reachedWave.ToString(), "memory-stat-value");
            AddStat(stats, "코어", FormatCore(record), "memory-stat-value memory-stat-value--orange");
            AddStat(stats, "제거", record.unitKillCount.ToString(), "memory-stat-value");
            card.Add(stats);

            var sitrep = new VisualElement();
            sitrep.AddToClassList("memory-sitrep");
            sitrep.Add(LobbyUitkElements.Label("SITREP", "memory-sitrep-label"));
            sitrep.Add(LobbyUitkElements.Label(PressureText(record), "memory-sitrep-text"));
            card.Add(sitrep);
        }

        private static void RenderRecentOperations(VisualElement section, IReadOnlyList<OperationRecord> records)
        {
            if (section == null)
                return;

            section.Clear();
            section.Add(LobbyUitkElements.Label("RECENT OPERATIONS", "memory-section-title"));
            if (records == null || records.Count == 0)
            {
                var empty = new VisualElement();
                LobbyUitkElements.AddClasses(empty, "operation-row operation-row--empty");
                empty.Add(LobbyUitkElements.Label("전적 기록 대기중", "operation-empty-text"));
                section.Add(empty);
                return;
            }

            for (var i = 0; i < records.Count && i < RecentOperationRecords.MaxRecords; i++)
            {
                var record = records[i];
                var row = new VisualElement();
                LobbyUitkElements.AddClasses(
                    row,
                    record.result == OperationRecordResult.Held
                        ? "operation-row operation-row--held"
                        : "operation-row operation-row--danger");

                var line = new VisualElement();
                LobbyUitkElements.AddClasses(
                    line,
                    record.result == OperationRecordResult.Held
                        ? "operation-row-line"
                        : "operation-row-line operation-row-line--danger");
                row.Add(line);

                var main = new VisualElement();
                main.AddToClassList("operation-row-main");
                main.Add(LobbyUitkElements.Label(
                    ResultText(record),
                    record.result == OperationRecordResult.Held
                        ? "operation-title operation-title--held"
                        : "operation-title operation-title--danger"));
                main.Add(LobbyUitkElements.Label(
                    $"{FormatClock(record.endedAtUnixMs)} / 공세 {record.reachedWave:00} / {BuildRosterSummary(record)}",
                    "operation-meta"));
                row.Add(main);

                row.Add(LobbyUitkElements.Label(
                    $"CORE {FormatCore(record)}",
                    record.result == OperationRecordResult.Held
                        ? "operation-core"
                        : "operation-core operation-core--danger"));
                section.Add(row);
            }
        }

        private static void RenderUnitTrace(VisualElement section, IReadOnlyList<OperationRecord> records)
        {
            if (section == null)
                return;

            section.Clear();
            section.Add(LobbyUitkElements.Label("기체 전적", "memory-section-title"));
            var chips = new VisualElement();
            chips.AddToClassList("memory-chip-row");
            var count = records?.Count ?? 0;
            chips.Add(LobbyUitkElements.Label($"{count}/5 RECORDS STORED", "memory-chip"));
            chips.Add(LobbyUitkElements.Label("LOCAL FIRST", "memory-chip memory-chip--blue"));
            chips.Add(LobbyUitkElements.Label(count > 0 ? "RECENT DATA LOADED" : "NO OPERATIONS", "memory-chip memory-chip--orange"));
            section.Add(chips);
        }

        private static void AddStat(VisualElement parent, string label, string value, string valueClass)
        {
            var cell = new VisualElement();
            cell.AddToClassList("memory-stat-cell");
            cell.Add(LobbyUitkElements.Label(label, "memory-stat-label"));
            cell.Add(LobbyUitkElements.Label(value, valueClass));
            parent.Add(cell);
        }

        private static string Shorten(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "WAIT";

            value = value.Trim();
            return value.Length <= 8 ? value : value.Substring(0, 8);
        }

        private static string ResultText(OperationRecord record)
        {
            return record?.result == OperationRecordResult.BaseCollapsed ? "거점 붕괴" : "버텨냄";
        }

        private static string FormatClock(long unixMs)
        {
            if (unixMs <= 0L)
                return "--:--";

            return DateTimeOffset.FromUnixTimeMilliseconds(unixMs).ToLocalTime().ToString("HH:mm");
        }

        private static string FormatDuration(float seconds)
        {
            var totalSeconds = Mathf.Max(0, Mathf.RoundToInt(seconds));
            return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
        }

        private static string FormatCore(OperationRecord record)
        {
            if (record == null || !record.hasCoreHealthPercent)
                return "--";

            return $"{Mathf.RoundToInt(record.coreHealthPercent * 100f)}%";
        }

        private static string PressureText(OperationRecord record)
        {
            if (record == null)
                return "작전 기록 대기 중";

            if (record.result == OperationRecordResult.BaseCollapsed)
                return "코어 방어선이 붕괴되었습니다. 다음 편성에서 방어/회복 축을 보강하세요.";

            return record.pressureSummaryKey == "pressure.core-collapsed"
                ? "거점 압박이 치명 단계까지 상승했습니다."
                : "방어선 유지. 최근 편성의 성과가 작전 기록에 반영되었습니다.";
        }

        private static string BuildRosterSummary(OperationRecord record)
        {
            if (record?.primaryRosterUnits == null || record.primaryRosterUnits.Count == 0)
                return "NO ROSTER";

            var units = new List<string>(record.primaryRosterUnits.Count);
            for (var i = 0; i < record.primaryRosterUnits.Count; i++)
                units.Add(record.primaryRosterUnits[i].Replace("|", " / "));

            return string.Join(" + ", units);
        }
    }

    internal readonly struct LobbyCreateRoomInput
    {
        public LobbyCreateRoomInput(
            string roomName,
            int capacity,
            string displayName,
            int difficultyPresetId)
        {
            RoomName = roomName;
            Capacity = capacity;
            DisplayName = displayName;
            DifficultyPresetId = difficultyPresetId;
        }

        public string RoomName { get; }
        public int Capacity { get; }
        public string DisplayName { get; }
        public int DifficultyPresetId { get; }
    }
}
