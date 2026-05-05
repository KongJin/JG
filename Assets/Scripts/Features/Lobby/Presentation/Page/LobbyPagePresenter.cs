using System;
using System.Collections.Generic;
using Features.Account.Application;
using Features.Account.Domain;
using Features.Lobby.Application.Events;
using Features.Lobby.Application.Ports;
using Features.Lobby.Domain;
using Features.Player.Domain;
using Shared.Gameplay;
using Shared.Kernel;
using Shared.Localization;
using UnityEngine;

namespace Features.Lobby.Presentation
{
    internal sealed class LobbyPagePresenter
    {
        public LobbyRoomListViewModel BuildRooms(
            IReadOnlyList<RoomSnapshot> rooms,
            DomainEntityId selectedRoomId = default)
        {
// csharp-guardrails: allow-null-defense
            var count = rooms?.Count ?? 0;
            var rows = new List<LobbyRoomRowViewModel>(count);
            if (rooms != null)
            {
                for (var i = 0; i < rooms.Count; i++)
                {
                    var room = rooms[i];
// csharp-guardrails: allow-null-defense
                    var memberCount = room.Members?.Count ?? 0;
                    rows.Add(new LobbyRoomRowViewModel(
                        room.Id,
                        room.Name,
                        BuildRoomMeta(memberCount, room.Capacity, room.DifficultyPresetId),
                        BuildRoomStatus(memberCount < room.Capacity),
                        memberCount < room.Capacity,
                        room.Id == selectedRoomId,
                        memberCount,
                        room.Capacity));
                }
            }

            return new LobbyRoomListViewModel(FormatRoomCount(count), rows);
        }

        public LobbyRoomListViewModel BuildRooms(
            IReadOnlyList<RoomListItem> rooms,
            DomainEntityId selectedRoomId = default)
        {
// csharp-guardrails: allow-null-defense
            var count = rooms?.Count ?? 0;
            var rows = new List<LobbyRoomRowViewModel>(count);
            if (rooms != null)
            {
                for (var i = 0; i < rooms.Count; i++)
                {
                    var room = rooms[i];
                    var canJoin = room.IsOpen && room.PlayerCount < room.MaxPlayers;
                    rows.Add(new LobbyRoomRowViewModel(
                        room.RoomId,
                        room.RoomName,
                        BuildRoomMeta(room.PlayerCount, room.MaxPlayers, room.DifficultyPresetId),
                        BuildRoomStatus(canJoin),
                        canJoin,
                        room.RoomId == selectedRoomId,
                        room.PlayerCount,
                        room.MaxPlayers));
                }
            }

            return new LobbyRoomListViewModel(FormatRoomCount(count), rows);
        }

        public LobbyRoomSelectionViewModel BuildRoomSelection(
            IReadOnlyList<RoomSnapshot> rooms,
            DomainEntityId selectedRoomId)
        {
            if (rooms == null || string.IsNullOrWhiteSpace(selectedRoomId.Value))
                return LobbyRoomSelectionViewModel.Empty;

            for (var i = 0; i < rooms.Count; i++)
            {
                var room = rooms[i];
                if (room.Id != selectedRoomId)
                    continue;

// csharp-guardrails: allow-null-defense
                var memberCount = room.Members?.Count ?? 0;
                var canJoin = memberCount < room.Capacity;
                return new LobbyRoomSelectionViewModel(
                    room.Id,
                    room.Name,
                    BuildRoomMeta(memberCount, room.Capacity, room.DifficultyPresetId),
                    BuildRoomStatus(canJoin),
                    canJoin
                        ? GameText.Get("lobby.room_join_hint")
                        : GameText.Get("lobby.full_room_body"),
                    memberCount,
                    room.Capacity,
                    canJoin);
            }

            return LobbyRoomSelectionViewModel.Empty;
        }

        public LobbyRoomSelectionViewModel BuildRoomSelection(
            IReadOnlyList<RoomListItem> rooms,
            DomainEntityId selectedRoomId)
        {
            if (rooms == null || string.IsNullOrWhiteSpace(selectedRoomId.Value))
                return LobbyRoomSelectionViewModel.Empty;

            for (var i = 0; i < rooms.Count; i++)
            {
                var room = rooms[i];
                if (room.RoomId != selectedRoomId)
                    continue;

                var canJoin = room.IsOpen && room.PlayerCount < room.MaxPlayers;
                return new LobbyRoomSelectionViewModel(
                    room.RoomId,
                    room.RoomName,
                    BuildRoomMeta(room.PlayerCount, room.MaxPlayers, room.DifficultyPresetId),
                    BuildRoomStatus(canJoin),
                    canJoin
                        ? GameText.Get("lobby.open_room_body")
                        : GameText.Get("lobby.closed_room_body"),
                    room.PlayerCount,
                    room.MaxPlayers,
                    canJoin);
            }

            return LobbyRoomSelectionViewModel.Empty;
        }

        public LobbyRoomDetailViewModel BuildRoomDetail(RoomSnapshot room, DomainEntityId localMemberId)
        {
// csharp-guardrails: allow-null-defense
            var members = room.Members ?? Array.Empty<RoomMemberSnapshot>();
            var memberRows = new List<string>(members.Count);
            var localIsReady = false;
            for (var i = 0; i < members.Count; i++)
            {
                var member = members[i];
                if (member.Id.Equals(localMemberId))
                    localIsReady = member.IsReady;

                var state = member.IsReady ? GameText.Get("common.ready") : GameText.Get("common.waiting");
                memberRows.Add($"{member.DisplayName} | {member.Team} | {state}");
            }

            return new LobbyRoomDetailViewModel(
                room.Name,
                $"{members.Count}/{room.Capacity} | {DifficultyPreset.ToShortLabel(room.DifficultyPresetId)}",
                memberRows,
                localIsReady,
                localIsReady ? "취소" : GameText.Get("common.ready"),
                room.OwnerId.Equals(localMemberId));
        }

        public LobbyRoomWaitingViewModel BuildRoomWaiting(
            RoomSnapshot room,
            DomainEntityId localMemberId,
            LobbyGarageSummaryViewModel deckSummary)
        {
            deckSummary ??= LobbyGarageSummaryViewModel.Empty;
// csharp-guardrails: allow-null-defense
            var members = room.Members ?? Array.Empty<RoomMemberSnapshot>();
            var totalSlots = Mathf.Max(room.Capacity, members.Count);
            var participants = new List<LobbyRoomParticipantViewModel>(totalSlots);
            var localIsReady = false;
            var localIsOwner = room.OwnerId.Equals(localMemberId);
            var localTeam = TeamType.None;
            var allReady = members.Count > 0;
            var hostName = "호스트 대기";

            for (var i = 0; i < members.Count; i++)
            {
                var member = members[i];
                if (!member.IsReady)
                    allReady = false;

                var isLocal = member.Id.Equals(localMemberId);
                if (isLocal)
                {
                    localIsReady = member.IsReady;
                    localTeam = member.Team;
                }

                if (member.Id.Equals(room.OwnerId))
                    hostName = DisplayNameOrWaiting(member.DisplayName);

                participants.Add(new LobbyRoomParticipantViewModel(
                    DisplayNameOrWaiting(member.DisplayName),
                    TeamLabel(member.Team),
                    member.IsReady ? GameText.Get("common.ready") : GameText.Get("common.waiting"),
                    member.IsReady,
                    isLocal,
                    isEmpty: false));
            }

            for (var i = members.Count; i < totalSlots; i++)
            {
                participants.Add(new LobbyRoomParticipantViewModel(
                    "참가 대기",
                    "빈 슬롯",
                    GameText.Get("common.waiting"),
                    isReady: false,
                    isLocal: false,
                    isEmpty: true));
            }

            return new LobbyRoomWaitingViewModel(
                room.Name,
                BuildRoomMeta(members.Count, room.Capacity, room.DifficultyPresetId),
                allReady ? "모두 준비 완료" : "참가자 대기 중",
                $"호스트 {hostName}",
                "연결 안정",
                participants,
                deckSummary,
                localTeam,
                localIsOwner,
                localIsReady,
                canStartGame: localIsOwner && allReady,
                isVisible: true);
        }

        public LobbyAccountViewModel BuildAccount(
            AccountProfile profile,
            AccountData accountData,
            int operationCount)
        {
            var displayName = string.IsNullOrWhiteSpace(profile?.displayName)
                ? "LOCAL PILOT"
                : profile.displayName.Trim();
            var authType = string.IsNullOrWhiteSpace(profile?.authType)
                ? "LOCAL"
                : profile.authType.Trim().ToUpperInvariant();
            var uidText = string.IsNullOrWhiteSpace(profile?.uid)
                ? "계정 연결 대기 중"
                : $"계정 {Shorten(profile.uid)}";
// csharp-guardrails: allow-null-defense
            var garageCount = accountData?.GarageRoster?.Count ?? 0;
            var settings = accountData?.Settings;

            return new LobbyAccountViewModel(
                displayName,
                authType == "GOOGLE" ? "Google 연결됨" : "Google 연결 대기 중",
                uidText,
                garageCount > 0 ? $"{garageCount}/4" : "로컬",
                $"{operationCount}/5",
                authType == "GOOGLE" ? "준비" : "대기",
                authType == "GOOGLE" ? "저장 가능" : "Google 연결 필요",
                garageCount > 0 ? $"유닛 {garageCount}개" : GameText.Get("lobby.deck_waiting"),
                $"{operationCount}/5",
                "정상",
                GameText.Get("common.ready"),
                $"{Mathf.RoundToInt((settings?.bgmVolume ?? 0.8f) * 100f)}%",
                $"{Mathf.RoundToInt((settings?.sfxVolume ?? 1f) * 100f)}%",
                "로컬 우선",
                authType == "GOOGLE" ? GameText.Get("common.ready") : GameText.Get("common.waiting"));
        }

        public LobbyGarageSummaryViewModel BuildGarageSummary(AccountData accountData)
        {
            var roster = accountData?.GarageRoster;
// csharp-guardrails: allow-null-defense
            var activeCount = roster?.Count ?? 0;
// csharp-guardrails: allow-null-defense
            var isReady = roster != null && roster.IsValid;
            if (isReady)
            {
                return new LobbyGarageSummaryViewModel(
                    GameText.Get("lobby.deck_ready"),
                    $"저장된 유닛 {activeCount}/{Features.Garage.Domain.GarageRoster.MaxSlots}",
                    GameText.Get("lobby.deck_saved_ready"),
                    activeCount,
                    Features.Garage.Domain.GarageRoster.MaxSlots,
                    isReady: true);
            }

            var missingCount = Mathf.Max(0, Features.Garage.Domain.GarageRoster.MinReadySlots - activeCount);
            return new LobbyGarageSummaryViewModel(
                activeCount > 0 ? GameText.Get("lobby.deck_need_more") : GameText.Get("lobby.deck_waiting"),
                $"저장된 유닛 {activeCount}/{Features.Garage.Domain.GarageRoster.MaxSlots}",
                missingCount > 0
                    ? $"최소 {Features.Garage.Domain.GarageRoster.MinReadySlots}기까지 {missingCount}기 부족합니다."
                    : "저장된 덱을 확인하세요.",
                activeCount,
                Features.Garage.Domain.GarageRoster.MaxSlots,
                isReady: false);
        }

        public LobbyOperationMemoryViewModel BuildOperationMemory(RecentOperationRecords records)
        {
            records ??= new RecentOperationRecords();
            var list = records.Records;
            var latest = list.Count > 0
                ? BuildLatestOperation(list[0])
                : LobbyOperationLatestViewModel.Empty;
            return new LobbyOperationMemoryViewModel(
                latest,
                BuildRecentOperations(list),
                new LobbyOperationTraceViewModel(
                    $"기록 {list.Count}/5 저장됨",
                    list.Count > 0 ? "최근 기록 불러옴" : GameText.Get("common.records_empty")));
        }

        private static IReadOnlyList<LobbyOperationRowViewModel> BuildRecentOperations(
            IReadOnlyList<OperationRecord> records)
        {
            if (records == null || records.Count == 0)
                return Array.Empty<LobbyOperationRowViewModel>();

            var rows = new List<LobbyOperationRowViewModel>(Mathf.Min(records.Count, RecentOperationRecords.MaxRecords));
            for (var i = 0; i < records.Count && i < RecentOperationRecords.MaxRecords; i++)
            {
                var record = records[i];
                var held = record.result == OperationRecordResult.Held;
                rows.Add(new LobbyOperationRowViewModel(
                    held ? "operation-row operation-row--held" : "operation-row operation-row--danger",
                    held ? "operation-row-line" : "operation-row-line operation-row-line--danger",
                    ResultText(record),
                    held ? "operation-title operation-title--held" : "operation-title operation-title--danger",
                    $"{FormatClock(record.endedAtUnixMs)} / {GameText.Get("battle.wave")} {record.reachedWave:00} / {BuildRosterSummary(record)}",
                    $"CORE {FormatCore(record)}",
                    held ? "operation-core" : "operation-core operation-core--danger"));
            }

            return rows;
        }

        private static LobbyOperationLatestViewModel BuildLatestOperation(OperationRecord record)
        {
            var held = record.result == OperationRecordResult.Held;
            return new LobbyOperationLatestViewModel(
                true,
                ResultText(record),
                held ? "memory-result memory-result--held" : "memory-result operation-title--danger",
                FormatClock(record.endedAtUnixMs),
                FormatDuration(record.survivalSeconds),
                record.reachedWave.ToString(),
                FormatCore(record),
                "memory-stat-value memory-stat-value--orange",
                record.unitKillCount.ToString(),
                PressureText(record));
        }

        private static string FormatRoomCount(int count)
        {
            return $"열린 방 {count}개";
        }

        private static string BuildRoomMeta(int playerCount, int capacity, int difficultyPresetId)
        {
            return $"{playerCount}/{capacity} | {DifficultyPreset.ToShortLabel(difficultyPresetId)}";
        }

        private static string BuildRoomStatus(bool canJoin)
        {
            return canJoin ? "참가 가능" : "정원 마감";
        }

        private static string DisplayNameOrWaiting(string displayName)
        {
            return string.IsNullOrWhiteSpace(displayName) ? GameText.Get("common.waiting") : displayName.Trim();
        }

        private static string TeamLabel(TeamType team)
        {
            return team switch
            {
                TeamType.Red => "RED",
                TeamType.Blue => "BLUE",
                _ => "팀 미정"
            };
        }

        private static string Shorten(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return GameText.Get("common.waiting");

            value = value.Trim();
            return value.Length <= 8 ? value : value.Substring(0, 8);
        }

        private static string ResultText(OperationRecord record)
        {
            return record?.result == OperationRecordResult.BaseCollapsed ? GameText.Get("records.core_destroyed") : GameText.Get("records.held");
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
                return GameText.Get("common.records_empty");

            if (record.result == OperationRecordResult.BaseCollapsed)
                return GameText.Get("records.core_pressure_lost");

            return record.pressureSummaryKey == "pressure.core-collapsed"
                ? "거점 압박이 치명 단계까지 상승했습니다."
                : GameText.Get("records.core_pressure_held");
        }

        private static string BuildRosterSummary(OperationRecord record)
        {
// csharp-guardrails: allow-null-defense
            if (record?.primaryRosterUnits == null || record.primaryRosterUnits.Count == 0)
                return "덱 없음";

            var units = new List<string>(record.primaryRosterUnits.Count);
            for (var i = 0; i < record.primaryRosterUnits.Count; i++)
                units.Add(record.primaryRosterUnits[i].Replace("|", " / "));

            return string.Join(" + ", units);
        }
    }
}
