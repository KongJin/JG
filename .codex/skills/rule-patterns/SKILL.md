---
name: rule-patterns
description: 금지 패턴, 이벤트 체인, 예외 처리, ErrorCode, 로깅 전략 관련 질문 시 이 스킬 사용. "금지 패턴인지?", "예외 처리", "에러 핸들링", "로그 정책", "이벤트 순환" 등을 묻는다면 항상 이 스킬을 사용하세요.
---

# Patterns

코드 패턴 및 금지 항목.

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

**중요: 사용자가 금지 패턴, 이벤트 체인, 예외 처리, 로깅 등에 대해 질문하면 반드시 해당 하위 문서를 먼저 읽어온 후에 답변하세요.**

| 사용자 질문 키워드 | 읽어올 하위 문서 |
|-------------------|-------------------|
| 금지 패턴, anti-pattern, Bootstrap 책임, 네이밍 충돌, 단일 호출 private 함수 | [anti_patterns.md](anti_patterns.md) |
| 이벤트 체인, 이벤트 순환, 순환 방지, 체인 깊이 제한, 단방향 규칙 | [event_rules.md](event_rules.md) |
| 예외 처리, ErrorCode, 에러 핸들링, GameException, Result<T>, ErrorDetail | [error_handling.md](error_handling.md) |
| 로깅 전략, 로그 정책, Log 진입점, 레이어별 허용 규칙 | [logging_rules.md](logging_rules.md) |
