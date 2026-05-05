---
name: rule-context
description: "프로젝트 맥락 규칙. Triggers: 배포, 팀 구조, 출시/수익화, Firebase, WebGL, 자동화 우선순위, 제품 전략 배경."
---

# Context

프로젝트의 팀, 일정, 배포 전략, 제품 철학, 기밀 맥락.

JG repo에서는 `AGENTS.md`와 `docs/index.md`로 current owner를 먼저 확인한 뒤 `plans.progress`, Firebase/WebGL owner docs, active design docs를 따른다.
제품/UX 판단 자체는 `jg-game-design`과 active design owner로 라우팅하고, 이 skill은 팀/배포/자동화/전략 배경 맥락만 보조한다.
이 skill은 generic project-context fallback/reference다.

---

## 빠른 찾기

| 주제 | 상세 |
|-------|--------|
| 팀 · 일정 | 팀: 1인 개발 · 고정 출시 일정 없음 (장기: 출시 후 수익화 목표) |
| 배포 · 검증 루프 | WebGL 빌드로 플레이 가능 링크 공유 · Firebase 콘솔에서 지표 수집 |
| WebGL 사용 이유 | 앱 스토어 심사·정책 부담 줄이기 위한 선택 |
| 자동화 우선순위 | 코드 생성·리팩터 · 씬·프리팹 작업 자동화 |
| 제품 전략 배경 | UI/UX와 콘텐츠 재미가 현재 중요한 배경이라는 맥락 |

---

## 하위 문서 읽어오기

사용자가 다음 주제에 대해 질문하면 해당 하위 문서를 읽어와서 답변하세요:

| 사용자 질문 키워드 | 읽어올 하위 문서 |
|-------------------|-------------------|
| 팀 · 일정, 출시 계획, 수익화 전략 | [developer_context.md](developer_context.md) |
| 배포 · 검증 루프, WebGL 빌드, Firebase, 프로젝트 맥락 | [developer_context.md](developer_context.md) |
| 자동화 우선순위, 제품 전략 배경, 현재 고민 맥락 | [developer_context.md](developer_context.md) |
