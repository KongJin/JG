# Bootstrap 규칙

Composition root 및 scene-level wiring 규칙.

---

## 역할

Bootstrap은 **wiring만** 담당합니다.

- 객체 생성
- 의존성 주입
- 초기화 순서
- 구독 (Application EventHandler 위임)

---

## 금지 패턴

Bootstrap은 **절대** 다음을 포함하지 않습니다:

- 비즈니스 로직 실행
- 도메인 엔티티 직접 생성
- 선택/의사결정 로직
- 게임 이벤트 처리
- `Update`, `LateUpdate`, 코루틴 루프로 게임플레이 플로우 구동

**명확한 경계:**
- Bootstrap이 "지금 무엇을 해야 하나?"를 대답하기 시작하면 → Application으로 이동
- Bootstrap이 생성된 객체의 런타임 상태를 일회성 초기화 이상으로 저장하면 → 상태 소유자를 Bootstrap 밖으로 이동
- Bootstrap에 게임플레이 플로우를 구동하는 `Update()` 또는 코루틴이 있으면 → Presentation MonoBehaviour로 분리

---

## 이벤트 핸들러 → Application 레이어

**규칙:** 이벤트 처리 로직은 Bootstrap이 아닌 Application 레이어에 둡니다.

- ❌ Bootstrap에서 `OnXxxEvent()` 메서드로 이벤트를 직접 구독·처리 — 잘못됨
- ✅ Application EventHandler가 생성자에서 EventBus를 직접 구독, Bootstrap은 생성 + `DisposableScope` 수명 관리만 — 올바름

**패턴:**
```
<Feature>Setup.cs / <Feature>Bootstrap.cs (wiring 전용)
  → Application EventHandler 생성 (생성자에 IEventSubscriber 주입)
  → EventHandler가 생성자에서 구독
  → Bootstrap이 소유권 추적: _disposables.Add(EventBusSubscription.ForOwner(eventBus, handler))
```

---

## UseCase를 통한 도메인 엔티티 생성

Bootstrap은 도메인 엔티티를 직접 생성해서는 안 됩니다.

```
❌ Bootstrap이 Domain.Projectile을 직접 생성
✅ Bootstrap이 SpawnProjectileUseCase 호출 → UseCase가 Domain.Projectile 생성
```

---

## 위치

Bootstrap 클래스는 feature root에 위치합니다 (`Bootstrap/` 폴더 내부 X).

```
Assets/Scripts/Features/Lobby/
  Domain/
  Application/
  Presentation/
  Infrastructure/
  LobbySetup.cs        # composition root
  LobbyBootstrap.cs    # scene-level wiring
```

---

## 하나만 규칙

feature root에 **Setup 또는 Bootstrap 클래스 하나**만 존재해야 합니다.

- ❌ `LobbySetup` + `LobbyBootstrap` + `LobbyBootstrap2` — 중복
- ✅ `LobbySetup` — composition root 하나

Bootstrap이 god class로 전환되지 않도록 주의하세요.
