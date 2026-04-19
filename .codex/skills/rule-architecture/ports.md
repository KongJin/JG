# 포트 배치 규칙

크로스 피처 포트 인터페이스 배치 방법.

---

## 포트 인터페이스 배치 패턴

Feature A가 Feature B의 기능을 사용할 때:

1. **Port 인터페이스**는 **소비자 (A)**의 `Application/Ports/`에 정의
2. **구현**은 **제공자 (B)**의 `Infrastructure/`에 둠
3. **Bootstrap**이 구현체를 생성하고 소비자에 주입

```
Combat/Application/Ports/ICombatTargetProvider.cs   ← Combat 정의 (소비자)
Player/Infrastructure/PlayerCombatTargetProvider.cs  ← Player 구현 (제공자)
```

**금지:** 제공자의 Application에 port를 정의하고 소비자가 import하는 것

---

## 타입 의존에 따른 Port 배치

**규칙:** Unity 타입을 사용하지 않는 port 인터페이스만 Application/Ports에 둡니다. UnityEngine 타입(Sprite, GameObject, AudioClip, Color 등)을 참조하는 port는 Presentation에 둡니다.

- ✅ `Application/Ports/IZoneEffectPort.cs` — `Float3`만 사용 (Shared), Application 배치 가능
- ✅ `Presentation/ISkillEffectPort.cs` — `GameObject`, `AudioClip` 사용 (Unity), Presentation 배치 필수
- ✅ `Presentation/ISkillIconPort.cs` — `Sprite` 사용 (Unity), Presentation 배치 필수
- ❌ `Application/Ports/ISkillEffectPort.cs` — Application 레이어에 Unity 타입 포함은 레이어 규칙 위반

**이유:** Application 레이어는 UnityEngine에 의존해서는 안 됩니다. Unity 타입 port를 Application으로 옮기면 이를 참조하는 모든 Application 클래스가 "감염"됩니다.

> **Bootstrap 위치**: [`bootstrap.md`](bootstrap.md#위치)
> **의존성 필드 규칙**: [`../unity/coding-rules.md`](../unity/coding-rules.md)

---

## Consumer-owned port

피처 의존성 그래프에서 consumer-owned `Application/Ports` 참조는 승인된 DIP seam으로 보고, DAG edge로 세지 않습니다.
