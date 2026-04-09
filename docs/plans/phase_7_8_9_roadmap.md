# Phase 7~9 로드맵

> **마지막 업데이트**: 2026-04-09
> Phase 0~6 완료 이후의 작업 우선순위

## 우선순위

```
Phase 7: 배치 시스템 완성 ──── 플레이어 조작 완성
    ↓
Phase 8: Energy 재생 증가 곡선 ─ 게임 밸런싱
    ↓
Phase 9: 네트워크 완성 ──────── remote 동기화 + 통계 UI
```

---

## Phase 7: 배치 시스템 완성

**목표**: 플레이어가 배치 가능 영역을 시각적으로 확인하고, 정확한 판정으로 소환할 수 있다.

| # | 작업 | 담당 | 우선순위 | 상세 |
|---|---|---|---|---|
| P7-1 | 배치 영역 정의 | Game Design | 🟥 필수 | 아군 진영 내 고정 영역 (월드 공간 좌표/크기) |
| P7-2 | 배치 영역 판정 개선 | Unit/Presentation | 🟥 필수 | `Screen.height * 0.5f` 하드코딩 제거 → 직렬화 필드 |
| P7-3 | 배치 영역 시각화 | Unit/Presentation | 🟥 필수 | 반투명 사각형/그리드 오버레이로 배치 가능 영역 표시 |
| P7-4 | 드래그 중 시각 피드백 | Unit/Presentation | 🟨 중요 | 배치 영역 진입 시 하이라이트, 영역 밖 시 빨간 오버레이 |
| P7-5 | 월드-스크린 변환 개선 | Unit/Presentation | 🟨 중요 | `Camera.main` 직렬화, 지면 Y 평면 교차 계산 |

### P7-1 배치 영역 정의

| 항목 | 값 | 비고 |
|---|---|---|
| 중심 X | 0 | 필드 중앙 |
| 중심 Z | Player 스폰 위치 기준 | GameSceneRoot에서 설정 |
| 너비 (X) | 8m | 조정 가능 |
| 깊이 (Z) | 5m | 조정 가능 |

### 완료 조건
- [ ] Inspector에서 배치 영역 위치/크기 설정 가능
- [ ] 게임 실행 시 배치 영역이 반투명 오버레이로 표시
- [ ] 드래그 중 영역 진입/이탈 시각 피드백
- [ ] 영역 밖 드롭 시 소환 취소

---

## Phase 8: Energy 재생 증가 곡선

**목표**: 게임이 진행될수록 Energy 재생 속도가 증가하여 후반 페이싱 조절.

| # | 작업 | 담당 | 우선순위 | 상세 |
|---|---|---|---|---|
| P8-1 | Energy 재생 곡선 설계 | Game Design | 🟥 필수 | 시간에 따른 재생량 증가 함수 정의 |
| P8-2 | `EnergyRegenUseCase` 확장 | Player/Application | 🟥 필수 | 게임 경과 시간을 인자로 받는 재생 로직 |
| P8-3 | 게임 시작 시간 기록 | GameSceneRoot | 🟥 필수 | `_sceneStartTime`을 Energy 시스템에 전달 |
| P8-4 | 초기 Energy 검증 | Game Design | 🟨 중요 | 가장 저렴한 유닛 1회 소환 가능한지 확인 |

### P8-1 재생 곡선 (제안)

| 시간 | 재생량 (초당) | 비고 |
|---|---|---|
| 0~60초 | 3.0 | 초반: 저렴한 유닛 20초마다 소환 가능 |
| 60~180초 | 3.0 → 5.0 | 중반: 선형 증가 |
| 180초+ | 5.0 | 후반: 고정 최대치 |

### 완료 조건
- [ ] 재생 함수 정의 및 문서화
- [ ] `EnergyRegenTicker`가 경과 시간을 `EnergyRegenUseCase`에 전달
- [ ] Unity Profiler에서 재생량 곡선 검증
- [ ] 초기 Energy가 가장 저렴한 유닛 소환 비용 이상

---

## Phase 9: 네트워크 완성

**목표**: 멀티플레이어에서 모든 클라이언트가 동기화된 게임 상태를 경험.

| # | 작업 | 담당 | 우선순위 | 상세 |
|---|---|---|---|---|
| P9-1 | BattleEntity 사망 시 remote 프리팹 파괴 | Unit/Infrastructure | 🟥 필수 | Owner `PhotonNetwork.Destroy` → remote도 시각적 파괴 |
| P9-2 | WaveEndView 실제 통계 표시 | Wave/Presentation | 🟨 중요 | 도달 Wave, 시간, 소환 횟수, 처치 수 표시 |
| P9-3 | UnitSlotsContainer 로테이션 버튼 Inspector 연결 | Unit/Presentation | 🟨 중요 | `UnitRotationControls` 직렬화 필드 연결 |
| P9-4 | 드래그 고스트 Prefab화 | Unit/Presentation | 🟨 중요 | 런타임 GameObject 생성 → prefab 참조로 변경 |

### P9-1 사망 동기화 흐름

```
Owner: BattleEntity HP <= 0 → UnitDiedEvent → PhotonNetwork.Destroy(gameObject)
Remote: PhotonNetwork.OnDestroy 콜백 → 프리팹 자동 소멸
```

### 완료 조건
- [ ] remote 클라이언트에서 BattleEntity 파괴 시각 확인
- [ ] WaveEndView에 승리/패배 외 통계 텍스트 표시
- [ ] 로테이션 버튼 Inspector에서 설정 가능
- [ ] 드래그 고스트가 prefab 기반으로 변경됨

---

## 참조

- 전체 로드맵: [`game_scene_entry_plan.md`](./game_scene_entry_plan.md)
- 현재 진행률: [`progress.md`](./progress.md)
