using UnityEngine;

namespace Shared.Ui
{
    /// <summary>
    /// 게임 전역 UI 색상 팔레트 — 색상 일관성 및 테마 변경 지원
    /// 모든 UI 색상은 이 팔레트를 통해统一管理한다.
    /// </summary>
    public static class GameColorPalette
    {
        // =====================================================================
        // Toast / 피드백 색상
        // =====================================================================

        /// <summary>성공 메시지 배경 — 어두운 그린 톤</summary>
        public static Color ToastSuccessBg => new(0.08f, 0.30f, 0.12f, 0.95f);

        /// <summary>성공 메시지 텍스트 — 부드러운 초록</summary>
        public static Color ToastSuccessText => new(0.7f, 1f, 0.7f, 1f);

        /// <summary>에러 메시지 배경 — 어두운 레드 톤</summary>
        public static Color ToastErrorBg => new(0.35f, 0.08f, 0.08f, 0.95f);

        /// <summary>에러 메시지 텍스트 — 부드러운 레드</summary>
        public static Color ToastErrorText => new(1f, 0.7f, 0.7f, 1f);

        // =====================================================================
        // Garage 슬롯 색상
        // =====================================================================

        /// <summary>선택된 슬롯 배경</summary>
        public static Color SlotSelected => new(0.24f, 0.47f, 0.89f, 1f);

        /// <summary>채워진 슬롯 배경</summary>
        public static Color SlotFilled => new(0.17f, 0.21f, 0.32f, 1f);

        /// <summary>빈 슬롯 배경</summary>
        public static Color SlotEmpty => new(0.10f, 0.12f, 0.18f, 0.92f);

        /// <summary>선택 글로우 효과</summary>
        public static Color SlotGlow => new(0.24f, 0.47f, 0.89f, 0.8f);

        // =====================================================================
        // 3D 미리보기 배경
        // =====================================================================

        /// <summary>3D 미리보기 카메라 배경색</summary>
        public static Color PreviewBg => new(0.05f, 0.06f, 0.10f, 1f);

        // =====================================================================
        // 공통 UI 색상
        // =====================================================================

        /// <summary>패널 배경 — 어두운 네이비</summary>
        public static Color PanelBg => new(0.07f, 0.10f, 0.16f, 0.97f);

        /// <summary>버튼 기본 — 블루</summary>
        public static Color ButtonPrimary => new(0.2f, 0.4f, 0.9f, 1f);

        /// <summary>버튼 비활성화 — 회색</summary>
        public static Color ButtonDisabled => new(0.3f, 0.3f, 0.3f, 0.5f);

        /// <summary>텍스트 기본 — 흰색</summary>
        public static Color TextDefault => Color.white;

        /// <summary>텍스트 비활성화 — 어두운 회색</summary>
        public static Color TextDisabled => new(0.5f, 0.5f, 0.5f, 1f);

        /// <summary>Validation 에러 — 부드러운 레드</summary>
        public static Color ValidationText => new(1f, 0.5f, 0.5f, 1f);

        // =====================================================================
        // 헬퍼 메서드
        // =====================================================================

        /// <summary>Toast 색상을 에러/성공에 따라 반환</summary>
        public static (Color bg, Color text) GetToastColors(bool isError)
        {
            return isError
                ? (ToastErrorBg, ToastErrorText)
                : (ToastSuccessBg, ToastSuccessText);
        }

        /// <summary>슬롯 색상을 상태에 따라 반환</summary>
        public static Color GetSlotColor(bool isSelected, bool hasLoadout)
        {
            if (isSelected) return SlotSelected;
            if (hasLoadout) return SlotFilled;
            return SlotEmpty;
        }
    }
}
