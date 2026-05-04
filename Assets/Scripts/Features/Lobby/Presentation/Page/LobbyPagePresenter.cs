using System;
using System.Collections.Generic;
using Features.Account.Application;
using Features.Account.Domain;
using Features.Lobby.Application.Events;
using Features.Lobby.Application.Ports;
using Features.Player.Domain;
using Shared.Gameplay;
using Shared.Kernel;
using UnityEngine;

namespace Features.Lobby.Presentation
{
    internal sealed class LobbyPagePresenter
    {
        public LobbyRoomListViewModel BuildRooms(
            IReadOnlyList<RoomSnapshot> rooms,
            DomainEntityId selectedRoomId = default)
        {
            var count = rooms?.Count ?? 0;
            var rows = new List<LobbyRoomRowViewModel>(count);
            if (rooms != null)
            {
                for (var i = 0; i < rooms.Count; i++)
                {
                    var room = rooms[i];
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

                var memberCount = room.Members?.Count ?? 0;
                var canJoin = memberCount < room.Capacity;
                return new LobbyRoomSelectionViewModel(
                    room.Id,
                    room.Name,
                    BuildRoomMeta(memberCount, room.Capacity, room.DifficultyPresetId),
                    BuildRoomStatus(canJoin),
                    canJoin
                        ? "작전 세부를 확인한 뒤 참여할 수 있습니다."
                        : "현재 분대 정원이 가득 차 대기 중입니다.",
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
                        ? "현재 열린 작전입니다. 참여 전에 분대 현황을 확인하세요."
                        : "현재 참여할 수 없는 작전입니다. 다른 열린 방을 선택하세요.",
                    room.PlayerCount,
                    room.MaxPlayers,
                    canJoin);
            }

            return LobbyRoomSelectionViewModel.Empty;
        }

        public LobbyRoomDetailViewModel BuildRoomDetail(RoomSnapshot room, DomainEntityId localMemberId)
        {
            var members = room.Members ?? Array.Empty<RoomMemberSnapshot>();
            var memberRows = new List<string>(members.Count);
            var localIsReady = false;
            for (var i = 0; i < members.Count; i++)
            {
                var member = members[i];
                if (member.Id.Equals(localMemberId))
                    localIsReady = member.IsReady;

                var state = member.IsReady ? "READY" : "WAIT";
                memberRows.Add($"{member.DisplayName} | {member.Team} | {state}");
            }

            return new LobbyRoomDetailViewModel(
                room.Name,
                $"{members.Count}/{room.Capacity} | {DifficultyPreset.ToShortLabel(room.DifficultyPresetId)}",
                memberRows,
                localIsReady,
                localIsReady ? "Cancel" : "Ready",
                room.OwnerId.Equals(localMemberId));
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
                ? "UID WAIT"
                : $"UID {Shorten(profile.uid)}";
            var garageCount = accountData?.GarageRoster?.Count ?? 0;
            var settings = accountData?.Settings;

            return new LobbyAccountViewModel(
                displayName,
                authType == "GOOGLE" ? "G-LINK OK" : "G-LINK WAIT",
                uidText,
                garageCount > 0 ? $"{garageCount}/4" : "로컬",
                $"{operationCount}/5",
                authType == "GOOGLE" ? "준비" : "대기",
                authType == "GOOGLE" ? "동기화 가능" : "Google 연결 필요",
                garageCount > 0 ? $"편성 {garageCount}기" : "편성 대기",
                $"{operationCount}/5",
                "정상",
                "READY",
                $"{Mathf.RoundToInt((settings?.bgmVolume ?? 0.8f) * 100f)}%",
                $"{Mathf.RoundToInt((settings?.sfxVolume ?? 1f) * 100f)}%",
                "LOCAL FIRST",
                authType == "GOOGLE" ? "READY" : "WAIT");
        }

        public LobbyGarageSummaryViewModel BuildGarageSummary(AccountData accountData)
        {
            var roster = accountData?.GarageRoster;
            var activeCount = roster?.Count ?? 0;
            var isReady = roster != null && roster.IsValid;
            if (isReady)
            {
                return new LobbyGarageSummaryViewModel(
                    "출격 가능",
                    $"현역 {activeCount}/{Features.Garage.Domain.GarageRoster.MaxSlots}",
                    "저장된 편성이 최소 출격 기준을 충족합니다.",
                    activeCount,
                    Features.Garage.Domain.GarageRoster.MaxSlots,
                    isReady: true);
            }

            var missingCount = Mathf.Max(0, Features.Garage.Domain.GarageRoster.MinReadySlots - activeCount);
            return new LobbyGarageSummaryViewModel(
                activeCount > 0 ? "편성 보강 필요" : "편성 대기",
                $"현역 {activeCount}/{Features.Garage.Domain.GarageRoster.MaxSlots}",
                missingCount > 0
                    ? $"최소 {Features.Garage.Domain.GarageRoster.MinReadySlots}기까지 {missingCount}기 부족합니다."
                    : "저장된 편성을 확인하세요.",
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
                    $"{list.Count}/5 RECORDS STORED",
                    list.Count > 0 ? "RECENT DATA LOADED" : "NO OPERATIONS"));
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
                    $"{FormatClock(record.endedAtUnixMs)} / 공세 {record.reachedWave:00} / {BuildRosterSummary(record)}",
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
}
