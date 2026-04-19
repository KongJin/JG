---
name: rule-architecture
description: 아키텍처, 레이어, 의존 방향, 포트 배치, Bootstrap, Entity, UseCase, 피처 구조 관련 질문 시 이 스킬 사용. "코드 어디에 둬야 하나", "이 레이어에서 호출 가능해?", "폴더 구조", "아키텍처 위반 체크" 등을 묻는다면 항상 이 스킬을 사용하세요.
---

# Architecture

Clean Architecture 기반 레이어, 포트 배치, Bootstrap, 네이밍 규칙.

---

## 빠른 찾기

| 주제 | 상세 |
|-------|--------|
| 레이어 규칙 | [layers.md](layers.md) - Domain, Application, Presentation, Infrastructure, Bootstrap 정의 |
| Bootstrap 역할 | [bootstrap.md](bootstrap.md) - composition root 및 scene-level wiring 규칙 |
| 포트 배치 | [ports.md](ports.md) - 크로스 피처 포트 인터페이스 배치 방법 |
| 네이밍 규칙 | [naming.md](naming.md) - 타입별 네이밍 패턴 및 안전 규칙 |

---

## 핵심 원칙

이 프로젝트는 Clean Architecture 기반 5개 레이어를 따릅니다.

```
Domain (순수 비즈니스 로직)
  ↓
Application (UseCase, port, 이벤트)
  ↓
Infrastructure (외부 시스템: Photon, 영속성, SDK)
Presentation (사용자 상호작용, UI, InputHandler)
Bootstrap (composition root, wiring만)
```

---

## 하위 문서 읽어오기

**중요: 사용자가 레이어, Bootstrap, 포트, 네이밍 등에 대해 질문하면 반드시 해당 하위 문서를 먼저 읽어온 후에 답변하세요.**

| 사용자 질문 키워드 | 읽어올 하위 문서 |
|-------------------|-------------------|
| 레이어 구조, 의존 방향, Domain, Application, Presentation, Infrastructure | [layers.md](layers.md) |
| Bootstrap 역할, composition root, scene-level wiring, UseCase 통한 도메인 엔티티 생성 | [bootstrap.md](bootstrap.md) |
| 포트 배치, 포트 인터페이스, 크로스 피처 포트, Unity 타입 포트 | [ports.md](ports.md) |
| 네이밍 규칙, 타입별 네이밍 패턴, type naming safety | [naming.md](naming.md) |

| 문서 | 내용 |
|-------|--------|
| [layers.md](layers.md) | 레이어 구조, 의존 방향, EventBus 소유권 |
| [bootstrap.md](bootstrap.md) | Bootstrap 역할, 금지 패턴, UseCase를 통한 도메인 엔티티 생성 |
| [ports.md](ports.md) | 포트 인터페이스 배치 패턴, Unity 타입 포트 처리 규칙 |
| [naming.md](naming.md) | 타입별 네이밍 패턴, type naming safety 규칙 |
