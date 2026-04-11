# Runtime Validation Checklist

하네스는 runtime UI flow를 자동 스모크하지 않는다. 이 문서는 Lobby/Garage/Game 진입 관련 회귀를 수동으로 확인하고 기록하는 현재 SSOT다.

씬 구조, Inspector wiring, Bootstrap 순서의 SSOT는 계속 각 feature `README.md`, `*Setup.cs`, `*Bootstrap.cs`, 실제 scene/prefab contract가 소유한다. 이 문서는 hierarchy path가 아니라 "행동/결과" 기준으로만 적는다.

---

## 메타

| 항목 | 값 |
|---|---|
| 날짜 | |
| 브랜치/커밋 | |
| 빌드/에디터 | |
| 플레이 인원 | |
| 검증자 | |

---

## 체크리스트

| 항목 | 기대 결과 | 결과 (Pass/Fail/Skip) | 메모 |
|---|---|---|---|
| Lobby 진입 후 기본 UI 렌더링 | Lobby 진입 직후 핵심 패널과 기본 상호작용이 정상적으로 보인다. | | |
| Garage 탭 진입/복귀 | Garage 탭으로 이동했다가 Lobby로 돌아와도 화면과 입력 상태가 깨지지 않는다. | | |
| invalid draft 격리 | 유효하지 않은 draft 편집이 committed roster를 오염시키지 않는다. | | |
| valid draft 저장 반영 | 유효한 draft 저장 후 다시 열었을 때 최신 roster가 반영된다. | | |
| `Clear` 동작 | `Clear` 뒤 roster count가 기대한 만큼 감소한다. | | |
| Ready auto-cancel / Ready gating | 조건이 깨지면 Ready가 자동 해제되고, 조건 충족 전에는 Ready/Start 흐름이 막힌다. | | |
| 신규 console error 없음 | 검증 중 새 `Error/Exception/Assert`가 발생하지 않는다. | | |

---

## 기록 메모

- 재현 절차:
- 실제 결과:
- 관련 로그/스크린샷 경로:
- 후속 조치:
