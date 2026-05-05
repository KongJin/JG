---
name: rule-patterns
description: "패턴 규칙. Triggers: 금지 패턴, 이벤트 체인, 예외 처리, ErrorCode, 로깅, 이벤트 순환."
---

# Patterns

코드 패턴 및 금지 항목.

JG repo에서는 `AGENTS.md`와 `docs/index.md`로 current owner를 먼저 확인한다.
이 skill은 repo owner 문서가 없거나 배경 설명이 필요할 때 쓰는 fallback/reference다.

---

## 빠른 찾기

| 주제 | 상세 |
|-------|--------|
| 금지 패턴 | [anti_patterns.md](anti_patterns.md) - Bootstrap 책임 위반, 네이밍 충돌, 단일 호출 private 함수 |
| 이벤트 체인 | [event_rules.md](event_rules.md) - 단방향 규칙, 체인 깊이 제한, 순환 방지 |
| 예외/ErrorCode | [error_handling.md](error_handling.md) - GameException<TCode>, ErrorCode enum, ErrorDetail, Result<T> |
| 로깅 전략 | [logging_rules.md](logging_rules.md) - 레이어별 허용 규칙, Log 정적 진입점 |

---

## 하위 문서 읽어오기

사용자가 금지 패턴, 이벤트 체인, 예외 처리, 로깅 등을 물으면 repo owner docs를 먼저 확인하고, 부족할 때 아래 fallback 문서를 읽는다.

| 사용자 질문 키워드 | 읽어올 하위 문서 |
|-------------------|-------------------|
| 금지 패턴, anti-pattern, Bootstrap 책임, 네이밍 충돌, 단일 호출 private 함수 | [anti_patterns.md](anti_patterns.md) |
| 이벤트 체인, 이벤트 순환, 순환 방지, 체인 깊이 제한, 단방향 규칙 | [event_rules.md](event_rules.md) |
| 예외 처리, ErrorCode, 에러 핸들링, GameException, Result<T>, ErrorDetail | [error_handling.md](error_handling.md) |
| 로깅 전략, 로그 정책, Log 진입점, 레이어별 허용 규칙 | [logging_rules.md](logging_rules.md) |
