using System.Collections.Generic;
using System.Globalization;

namespace Shared.Localization
{
    public static class GameText
    {
        private static readonly IReadOnlyDictionary<string, string> Korean = new Dictionary<string, string>
        {
            ["common.ready"] = "준비 완료",
            ["common.waiting"] = "대기 중",
            ["common.records_empty"] = "최근 기록 없음",
            ["common.save_waiting"] = "저장 대기 중",
            ["common.connection_checking"] = "연결 확인 중",
            ["common.connection_expired"] = "연결이 끊겼습니다",

            ["lobby.create_room"] = "방 만들기",
            ["lobby.start_game"] = "게임 시작",
            ["lobby.join_room"] = "방 참가",
            ["lobby.room_detail"] = "방 정보",
            ["lobby.participants"] = "참가자 목록",
            ["lobby.room_list_body"] = "참가할 방을 선택하고 정보를 확인하세요.",
            ["lobby.empty_rooms_body"] = "새 방을 만들면 다른 플레이어가 참가할 수 있습니다.",
            ["lobby.create_room_body"] = "인원과 난이도를 정하고 친구를 기다릴 수 있습니다.",
            ["lobby.deck_check"] = "덱 확인",
            ["lobby.deck_waiting"] = "덱 준비 필요",
            ["lobby.deck_ready"] = "덱 준비 완료",
            ["lobby.deck_saved_ready"] = "저장된 덱이 시작 조건을 만족합니다.",
            ["lobby.deck_need_more"] = "덱 보강 필요",
            ["lobby.open_room_body"] = "현재 열린 방입니다. 참가 전에 멤버를 확인하세요.",
            ["lobby.closed_room_body"] = "현재 참가할 수 없는 방입니다. 다른 열린 방을 선택하세요.",
            ["lobby.full_room_body"] = "현재 방 정원이 가득 찼습니다.",
            ["lobby.room_join_hint"] = "방 정보를 확인한 뒤 참가할 수 있습니다.",

            ["garage.unit_empty"] = "빈 슬롯",
            ["garage.unit_waiting"] = "유닛 대기",
            ["garage.record_waiting"] = "최근 기록 없음",
            ["garage.status_waiting"] = "대기",
            ["garage.status_draft"] = "수정 중",
            ["garage.status_saved"] = "저장됨",
            ["garage.fixed_firepower"] = "고정 공격",
            ["garage.most_reused_unit"] = "가장 자주 쓴 유닛",
            ["garage.recent_contributor_unit"] = "최근 활약 유닛",
            ["garage.deck_saved"] = "저장된 덱",
            ["garage.deck_draft"] = "수정 중인 덱",
            ["garage.deck_save"] = "덱 저장",
            ["garage.deck_sync_status"] = "덱 저장 상태 | 유닛 {0}/{1}",
            ["garage.deck_update_ready"] = "덱 변경 대기 | 저장 가능",
            ["garage.deck_update_pending"] = "덱 수정 중 | 저장 필요",
            ["garage.deck_missing_units"] = "저장된 유닛 {0}/{1} | 유닛 +{2} 필요",
            ["garage.save_and_place"] = "저장 및 시작 준비",
            ["garage.roster_initializing"] = "덱 불러오는 중...",
            ["garage.save_in_progress"] = "저장 중...",

            ["records.title"] = "플레이 기록",
            ["records.recent"] = "최근 기록",
            ["records.unit_records"] = "유닛 기록",
            ["records.held"] = "방어 성공",
            ["records.core_destroyed"] = "코어 파괴",
            ["records.core_pressure_held"] = "코어를 지켜냈습니다. 최근 덱의 결과가 기록에 반영되었습니다.",
            ["records.core_pressure_lost"] = "코어가 파괴되었습니다. 다음 덱에서 방어/회복 역할을 보강하세요.",
            ["records.play_time"] = "플레이 시간: {0:F0}분 {1:F0}초",
            ["records.unit_deployed"] = "유닛 사용: {0}",

            ["battle.tap_field_to_place"] = "슬롯 선택 후 필드를 탭해 유닛을 놓으세요.",
            ["battle.tap_place_area"] = "필드를 탭하세요",
            ["battle.outside_place_area"] = "놓을 수 없는 위치입니다",
            ["battle.place_failed"] = "소환 실패",
            ["battle.wave"] = "웨이브",
        };

        public static string Get(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return string.Empty;
            }

            return Korean.TryGetValue(key, out string value) ? value : key;
        }

        public static string Format(string key, params object[] args)
        {
            return string.Format(CultureInfo.CurrentCulture, Get(key), args);
        }
    }
}
