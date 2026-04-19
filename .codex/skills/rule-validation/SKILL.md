---
name: rule-validation
description: 검증, Clean Levels, static-clean, compile-clean, runtime-smoke-clean, webgl-build-clean 관련 질문 시 이 스킬 사용. "검증 통과했어?", "컴파일 에러 체크", "런타임 검증", "LayerDependencyValidator", "Clean Levels" 등을 묻는다면 항상 이 스킬을 사용하세요.
---

# Validation

검증 기준: clean levels, compile/runtime 검증 게이트.

---

## 빠른 찾기

| 주제 | 상세 |
|-------|--------|
| 세부 검증 항목 | [validation_gates.md](validation_gates.md) - 각 레벨별 포함, 성능 게이트, 금지 사항 |

---

## Clean Levels

| 레벨 | 정의 | 검증 |
|-------|------|--------|
| **static-clean** | 정적 스캔 기준 규칙 위반 없음 | LayerDependencyValidator |
| **compile-clean** | Unity 컴파일 에러 0 | `tools/check-compile-errors.ps1` |
| **runtime-smoke-clean** (선택 보고) | 지정된 최소 씬/플로우 런타임 검증 통과 | 수동 체크리스트 |
| **webgl-build-clean** (선택 보고) | WebGL 빌드 성공 및 성능·최적화 기준 통과 | Profiler 검증 |

---

## 하위 문서 읽어오기

사용자가 다음 주제에 대해 질문하면 해당 하위 문서를 읽어와서 답변하세요:

| 사용자 질문 키워드 | 읽어올 하위 문서 |
|-------------------|-------------------|
| 세부 검증 항목, 각 레벨별 포함, 성능 게이트, 금지 사항 | [validation_gates.md](validation_gates.md) |
| LayerDependencyValidator, runtime-smoke-clean, webgl-build-clean | [validation_gates.md](validation_gates.md) |
