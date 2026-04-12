# 계정 시스템 기획

> 생성일: 2026-04-11
> 최종 업데이트: 2026-04-12

## 개요

Firebase Authentication + Firestore 기반 계정 시스템.
**WebGL 호환을 위해 Firebase REST API를 직접 호출한다** (Unity SDK 미사용).
Phase 10은 익명 로그인 + Firestore 저장/동기화에 집중.
Google 계정 업그레이드는 후속 Phase로 분리.

## 현재 구현 스냅샷 (2026-04-12)

- 완료: `IAuthPort.SignInWithGoogle`, `FirebaseAuthRestAdapter.signInWithIdp`, `SignInWithGoogleUseCase`, `AccountSetup` wiring, `AccountConfig.googleWebClientId`, WebGL JS 브리지, `AccountSettingsView` Google 로그인 버튼 코드, Firebase Auth linking 요청
- 완료: `CodexLobbyScene`의 `GaragePageRoot`에 `AccountSettingsView`를 배치하고 `_googleSignInButton`, `_statusMessageText`, `_authTypeText`를 씬 자산에 직렬화 wiring
- 미완료: 실제 WebGL smoke 테스트, 익명→Google 업그레이드 시 UID 유지 검증
- 현재 실제 앱 시작 플로우: `LobbySetup` 진입 시 익명 로그인 자동 실행 후 Garage 우측 상단 계정 영역에서 Google 로그인 버튼으로 업그레이드 가능

---

## 1. 인증 플로우

### 1.1 익명 로그인 (기본)
```
[앱 시작 — LobbySetup 내부 오버레이]
  → 로딩 화면 ("로그인 중...")
    → FirebaseAuthRestAdapter.SignInAnonymously()
      → POST https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={apiKey}
        → idToken + localId (UID) 발급
          → Firestore에서 프로필 로드 (신규 시 자동 생성)
            → Lobby 진입
```

**중요:** 로그인 화면은 별도 씬이 아닌 **LobbySetup 내부 오버레이**다.
SoundPlayer(DDOL), GameSceneRoot 초기화 계약을 깨지 않기 위함.

**로그인 실패 시:**
- 자동 재시도 3회 (1초 간격)
- 3회 모두 실패 → "네트워크 연결을 확인해주세요" 메시지 + 재시도 버튼

### 1.2 Google 로그인 (Phase 11)
```
[설정 화면 → Google 로그인]
  → WebGL: google.accounts.id.initialize/prompt → Google ID 토큰 수집
  → POST https://identitytoolkit.googleapis.com/v1/accounts:signInWithIdp?key={apiKey}
    → Body: {
        "postBody": "id_token={GOOGLE_ID_TOKEN}&providerId=google.com",
        "requestUri": "http://localhost",
        "returnSecureToken": true,
        "idToken": "{CURRENT_FIREBASE_ID_TOKEN}" // 익명 계정 linking 시 포함
      }
    → idToken + refreshToken + localId 발급
      → 기존 UID와 다르면 계정 linking 또는 신규 계정 생성
        → Firestore 프로필 로드
```

**WebGL에서 Google ID 토큰 수집:**
```html
<!-- index.html에 Google Sign-In SDK 추가 -->
<script src="https://accounts.google.com/gsi/client"></script>
```
```csharp
// Unity C# extern
[DllImport("__Internal")]
private static extern void AccountGoogleSignIn_RequestIdToken(
    string clientId,
    string callbackObjectName,
    string successMethodName,
    string errorMethodName);
```

**PC 에디터에서 테스트:**
- WebGL이 아닌 경우: 수동으로 Google ID 토큰을 입력받거나, EditorWebRequest로 대체
- 또는 Firebase Admin SDK로 토큰 발급 (로컬 테스트용)

**계정 linking 전략:**
- 익명 로그인 → Google 로그인: 기존 UID 유지, authType만 "google"로 업그레이드
- 이미 Google로 로그인: 기존 프로필 로드

**현재 구현 상태 (2026-04-12):**
- 완료: Firebase Auth `signInWithIdp` 호출, UseCase, `AccountSetup` wiring, `googleWebClientId` 설정 필드, WebGL JS 브리지, `AccountSettingsView` 버튼/콜백 코드, 기존 Firebase ID 토큰 전달 기반 linking 요청
- 완료: `CodexLobbyScene`에 `AccountSettingsView`와 Google 버튼/상태 텍스트 wiring 반영
- 미완료: WebGL smoke 테스트, UID 유지 검증

### 1.3 토큰 관리
```
idToken 유효기간: 1시간
  → 만료 5분 전에 자동 갱신
  → POST https://securetoken.googleapis.com/v1/token?key={apiKey}
       grant_type=refresh_token
       refresh_token={refreshToken}
```

### 1.4 로그아웃 / 재로그인
```
[설정 화면 → 로그아웃]
  → 로컬 토큰/UID 정리
    → 앱 재시작 시 익명 로그인 반복

[재로그인]
  → Google 로그인 (향후)
    → 기존 UID로 로그인 (동일 계정)
      → Firestore 데이터 로드
```

### 1.5 계정 삭제
```
[설정 화면 → 계정 삭제]
  → 확인 다이얼로그 ("모든 데이터가 삭제됩니다")
    → 1. Firestore 계정 문서 전체 삭제 (profile, garage, stats, settings)
    → 2. Firebase Auth 계정 삭제 (REST API)
    → 순서 중요: Firestore 먼저 삭제 → Auth 삭제
    → Auth 삭제 실패 시 "데이터 정리 필요" 플래그 로컬에 남김
      (다음 로그인 시 재시도)
```
> 완전한 원자성은 Cloud Functions 도입 시 처리. Phase 10은 순차 삭제 + 실패 플래그.

---

## 2. 데이터 구조 (Firestore)

### 2.1 컬렉션 구조
```
accounts/{uid}/
  ├── profile
  │     ├── displayName: string           // 월 1회 변경 제한 (클라이언트 timestamp 기준)
  │     ├── lastNicknameChange: timestamp (클라이언트)
  │     ├── createdAt: timestamp (클라이언트)
  │     └── authType: "anonymous" | "google"
  │
  ├── garage
  │     └── roster: { loadout: [...] }    // 기존 GarageRoster JSON
  │
  ├── stats
  │     ├── totalPlayTimeSeconds: number
  │     ├── totalGames: number
  │     ├── totalVictories: number
  │     ├── totalDefeats: number
  │     ├── highestWave: number
  │     ├── totalSummons: number
  │     └── totalUnitKills: number
  │
  └── settings
        ├── masterVolume: number (0~1)
        ├── bgmVolume: number (0~1)
        ├── sfxVolume: number (0~1)
        └── language: string ("ko" | "en" | "ja")
```

### 2.2 Firestore REST API 엔드포인트
```
읽기:
  GET https://firestore.googleapis.com/v1/projects/{project}/databases/(default)/documents/accounts/{uid}/{collectionId}/{documentId}?key={apiKey}
  Header: Authorization: Bearer {idToken}

쓰기:
  PATCH https://firestore.googleapis.com/v1/projects/{project}/databases/(default)/documents/accounts/{uid}/{collectionId}/{documentId}?key={apiKey}
  Header: Authorization: Bearer {idToken}
  Body: { "fields": { ... }, "updateMask": { "fieldPaths": [...] } }

삭제:
  DELETE https://firestore.googleapis.com/v1/projects/{project}/databases/(default)/documents/accounts/{uid}/{collectionId}/{documentId}?key={apiKey}
  Header: Authorization: Bearer {idToken}
```

### 2.3 데이터 동기화 전략
```
[로컬 캐시] ←→ [Firestore REST API] ←→ [Photon CustomProperties]
  │                        │
  ├── 읽기: REST API → JSON 파싱 → 로컬 캐시 → 게임 사용
  ├── 쓰기 (Garage): 게임 변경 → 로컬 캐시 → REST API PATCH → Photon CustomProperties 동기화
  └── 충돌: 단일 클라이언트 가정, last-write-wins
```

### 2.4 GarageRoster 저장 + Photon 연동 흐름
```
[Garage에서 편성 저장]
  → SaveRosterUseCase.Execute(roster)
    1. FirestoreRestPort.SaveGarage(roster)  // 클라우드 저장
    2. Photon 네트워크 동기화               // SaveRosterUseCase 수정
       → IGarageNetworkPort.SyncRoster(roster)
       → CustomProperties["garageRoster"] 갱신

[GameScene 진입]
  → RestoreGarageRosterUseCase.Execute()
    → IGarageNetworkPort.GetLocalPlayerRoster()
      → Photon CustomProperties에서 읽기 (late-join 대응)
```

**중요:** Firestore 저장만 하고 Photon 동기화를 안 하면 전투에서 빈 편성이 들어간다.
`SaveRosterUseCase`는 기존 구조(로컬 저장 + 네트워크 동기화)를 유지하되,
로컬 저장 → Firestore 저장으로만 변경한다.

### 2.5 오프라인 대응
```
WebGL은 브라우저 sessionStorage/localStorage 활용
  - 로컬 캐시: JSON 직렬화 후 localStorage 저장
  - 네트워크 끊김: localStorage에서 읽기
  - 복구 시: localStorage → REST API 동기화
```

### 2.6 기존 로컬 데이터 마이그레이션
- **진행 안 함**. 기존 `garage_roster.json`은 무시.
- 계정 시스템 도입 후 모든 데이터는 Firestore에서 관리.
- 영향: 기존 Garage 사용자는 편성 데이터 초기화됨.

---

## 3. Feature 구조

```
Features/Account/
  Domain/
    Account.cs              // UID, displayName, authType, createdAt
    PlayerStats.cs          // 전적 데이터 (ValueObject)
    UserSettings.cs         // 음량, 언어 등 설정 (ValueObject)
  Application/
    Ports/
      IAuthPort.cs          // SignInAnonymously, SignInWithGoogle, SignOut, DeleteAccount, GetToken
      IAccountDataPort.cs   // LoadProfile, SaveProfile, LoadStats, SaveStats, LoadGarage, SaveGarage, LoadSettings, SaveSettings, DeleteAccount
    SignInAnonymouslyUseCase.cs
    SignInWithGoogleUseCase.cs
    LoadAccountUseCase.cs
    SaveAccountUseCase.cs
    ChangeDisplayNameUseCase.cs   // 월 1회 검증 (클라이언트 timestamp 기준)
    DeleteAccountUseCase.cs
  Presentation/
    LoginLoadingView.cs     // 로딩 스피너 ("로그인 중...") + 실패 시 재시도 버튼
                            // LobbySetup 내부 오버레이로 동작 (별도 씬 아님)
    AccountSettingsView.cs  // 단일 View 내 섹션 구분:
                            //   - 계정 정보 (UID, authType, displayName)
                            //   - 닉네임 변경 (월 1회 제한 표시)
                            //   - 로그아웃
                            //   - 계정 삭제 (확인 다이얼로그)
                            //   - Google 로그인 버튼 (CodexLobbyScene wiring 반영)
  Infrastructure/
    FirebaseAuthRestAdapter.cs    // Firebase Auth REST API (IAuthPort 구현)
                                  // 내부: 토큰 관리, 자동 갱신 포함
    FirestoreRestPort.cs          // Firestore REST API (IAccountDataPort 구현)
                                  // 내부: localStorage 캐시 포함
  AccountSetup.cs                 // Composition Root
```

---

## 4. 기존 코드 변경 영향

### 4.1 LobbySetup
```csharp
// AccountSetup 초기화 추가
_accountSetup.Initialize(_eventBus);
await _accountSetup.SignInAnonymously.Execute();  // 익명 로그인 후 진입
```
- LoginLoadingView는 LobbySetup 내부 오버레이로 표시
- 기존 SoundPlayer(DDOL), GameSceneRoot 초기화 흐름 유지

### 4.2 SaveRosterUseCase 수정
```csharp
// 기존: 로컬 JSON 저장 + Photon 동기화
// 변경: Firestore 저장 + Photon 동기화

public Result Execute(GarageRoster roster, out string errorMessage)
{
    // 1. Firestore 저장 (클라우드 SSOT)
    _accountDataPort.SaveGarage(roster);

    // 2. Photon 네트워크 동기화 (전투 진입용)
    _network.SyncRoster(roster);
    _network.SyncReady(roster.IsValid);

    _eventBus.Publish(new RosterSavedEvent(roster));
    return Result.Success();
}
```
- `GarageJsonPersistence` 클래스 삭제
- `IGarageNetworkPort` 의존성 유지 (Photon 동기화 책임)

### 4.3 GameSceneRoot
- 추가적인 Photon Custom Authentication 설정은 Phase 10 범위에 포함하지 않음
- `RestoreGarageRosterUseCase`는 기존대로 Photon CustomProperties에서 읽음

### 4.4 SoundPlayer, 설정
```
기존: 로컬 PlayerPrefs 또는 하드코딩
신규: Account.settings → 볼륨, 언어 동기화
```

---

## 5. Firebase 설정

### 5.1 Firebase Console
- Authentication → 익명 로그인 활성화
- Firestore Database 생성
- **Web API Key** 복사 (프로젝트 설정 → Web API Key)
- `google-services.json` 불필요 (REST API 사용)

### 5.2 필요한 설정 값
```csharp
// AccountConfig.cs (ScriptableObject 또는 하드코딩)
public sealed class AccountConfig
{
    public string firebaseApiKey;       // Web API Key
    public string projectId;            // Firebase 프로젝트 ID
    public string googleWebClientId;    // Google OAuth Web Client ID
}
```

### 5.3 보안 규칙 (Firestore)
```javascript
match /accounts/{uid} {
  match /{document=**} {
    allow read, write: if request.auth != null && request.auth.uid == uid;
  }
}
```
> 현재는 단일 클라이언트 가정. 추후 조작 방지 필요 시 Cloud Functions로 서버 사이드 검증 추가 가능.

### 5.4 비용 예상
- Firebase Auth: 익명 로그인 무료
- Firestore: 일일 무료 할당량 50,000 읽기 / 20,000 쓰기
- 1인당 세션당 약 5~10회 읽기/쓰기 → 월 1,000명까지 무료

### 5.5 빌드 사이즈 영향
- **Firebase Unity SDK 미사용** → 빌드 사이즈 영향 없음
- HttpClient는 Unity 기본 포함
- **총 증가: ~0MB** (순수 C# HTTP 통신만 추가)

---

## 6. REST API 구현 상세

### 6.1 익명 로그인 요청
```csharp
// POST https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={apiKey}
// Request: { "returnSecureToken": true }
// Response: { "idToken", "refreshToken", "localId", "expiresIn" }
```

### 6.2 토큰 갱신 요청
```csharp
// POST https://securetoken.googleapis.com/v1/token?key={apiKey}
// Request: { "grant_type": "refresh_token", "refresh_token": "..." }
// Response: { "id_token", "refresh_token", "expires_in", "token_type": "Bearer" }
```

### 6.3 Firestore 문서 읽기
```csharp
// GET https://firestore.googleapis.com/v1/projects/{project}/databases/(default)/documents/accounts/{uid}/profile/profile?key={apiKey}
// Header: Authorization: Bearer {idToken}
// Response: { "name", "fields": { "displayName": { "stringValue": "..." }, ... } }
```

### 6.4 HTTP 클라이언트
```
WebGL에서는 UnityWebRequest 사용
  - UnityWebRequest: WebGL 호환 확실, async/await wrapper
  - HttpClient: WebGL 빌드 시 linker.xml 설정 필요

추천: UnityWebRequest + Task wrapper
```

---

## 7. 테스트 계획

### smoke 테스트 (실제 Firebase + WebGL 빌드)
| 테스트 | 내용 |
|--------|------|
| 익명 로그인 | REST API 호출 → UID 발급 → 프로필 생성 → Lobby 진입 |
| Google 로그인 | Google ID 토큰 수집 → signInWithIdp → 프로필 로드/생성 |
| 계정 linking | 익명 → Google 업그레이드 시 기존 UID 유지 |
| 로그인 실패 | 네트워크 차단 → 3회 재시도 → 실패 UI |
| Garage 저장 | 편성 저장 → Firestore 저장 + Photon 동기화 |
| 전투 진입 | GameScene 진입 → Photon CustomProperties에서 roster 복원 |
| 탭 왕복 | Lobby → Garage → Lobby → Garage (데이터 유지) |
| 닉네임 변경 | 변경 → 1달 이내 재시도 차단 |
| WebGL 빌드 | 실제 WebGL 빌드에서 로그인/저장 동작 확인 |

---

## 8. 진행 계획

### Phase 10: 계정 시스템 (익명 로그인)

| 단계 | 내용 | 예상 공수 |
|------|------|----------|
| 10-1 | Firebase Auth REST API + 토큰 관리 (익명 로그인) | 1일 |
| 10-2 | Firestore REST API + localStorage 캐시 (CRUD) | 1일 |
| 10-3 | LoginLoadingView + AccountSettingsView | 1일 |
| 10-4 | SaveRosterUseCase 수정 (Firestore + Photon 동기화) | 1일 |
| 10-5 | 닉네임 변경 (월 1회 검증) + 설정 동기화 | 1일 |
| 10-6 | WebGL 빌드 smoke 테스트 + 문서 | 1일 |

**총 예상 공수: 6일**

### Phase 11: Google 로그인

| 단계 | 내용 | 예상 공수 |
|------|------|----------|
| 11-1 | IAuthPort에 SignInWithGoogle 메서드 추가 | 0.5일 |
| 11-2 | FirebaseAuthRestAdapter에 signInWithIdp REST 구현 | 0.5일 |
| 11-3 | SignInWithGoogleUseCase 생성 + AccountSetup wiring | 0.5일 |
| 11-4 | AccountConfig에 Google webClientId 필드 추가 | 0.25일 |
| 11-5 | WebGL JS 브리지 (google.accounts.id) | 1일 |
| 11-6 | AccountSettingsView에 Google 로그인 버튼 추가 | 0.5일 |
| 11-7 | 계정 linking (익명→Google 업그레이드) | 0.5일 |
| 11-8 | WebGL 빌드 smoke 테스트 | 0.5일 |

**총 예상 공수: 4.25일**

**현재 상태 (2026-04-12)**
- 완료: 11-1 ~ 11-7 코드 반영, `CodexLobbyScene` Inspector wiring 반영
- 남음: WebGL smoke 테스트
- 주의: Firebase Auth linking 요청은 구현됐지만 실제 UID 유지 여부는 WebGL 실기 테스트로 검증이 필요하다.

---

## 9. 리스크 및 대응

| 리스크 | 영향 | 대응 |
|--------|------|------|
| WebGL에서 HttpClient 제한 | CORS, linker 문제 | UnityWebRequest + async wrapper 사용 |
| 토큰 만료 처리 누락 | Firestore 접근 401 | FirebaseAuthRestAdapter 내부 자동 갱신 (만료 5분 전) |
| localStorage 용량 제한 | 캐시 저장 실패 | 계정 데이터 작음 (~5KB), 문제 없음 |
| Firestore 오프라인 | 네트워크 끊김 시 지연 | localStorage 캐시로 읽기/쓰기, 복구 시 동기화 |
| 로그인 실패 (네트워크 불안) | 앱 진입 차단 | 자동 재시도 3회 + 재시도 버튼 |
| 계정 삭제 실패 | 데이터 불일치 | 순차 삭제 + 실패 시 플래그, 다음 로그인 재시도 |
| Photon 동기화 누락 | 전투에서 빈 편성 | SaveRosterUseCase가 Firestore + Photon 동시 동기화 |

---

## 10. 결정 사항

| 항목 | 결정 |
|------|------|
| 로그인 방식 | 익명 로그인 기본 (Google 업그레이드는 후속 Phase) |
| 저장 데이터 | 편성 + 전적 + 설정 |
| 기기 변경 | 크로스 디바이스 동기화 |
| 우선순위 | Phase 9와 병행 |
| 계정 삭제 | 필요 (순차 삭제 + 실패 플래그) |
| 닉네임 변경 | 월 1회 제한 (클라이언트 timestamp 기준) |
| 로컬 데이터 이전 | 안 함 (기존 garage_roster.json 무시) |
| 로그인 화면 | LobbySetup 내부 오버레이 (별도 씬 아님) |
| 로그인 실패 | 자동 재시도 3회 + 재시도 버튼 |
| 설정 화면 구조 | 단일 View 내 섹션 구분 |
| 오프라인 대응 | WebGL localStorage 캐시 + REST API 동기화 |
| 테스트 방식 | WebGL 빌드 smoke 테스트 |
| Firebase SDK | **Unity SDK 미사용, REST API 직접 호출** |
| 빌드 사이즈 영향 | 없음 (순수 C# HTTP 통신) |
| 공수 (Phase 10) | 6일 (핵심만) |
| 공수 (Phase 11) | 4.25일 (Google 로그인) |
| Google 연동 | Phase 11 기반 코드/씬 wiring 반영 완료 (`signInWithIdp`, JS bridge, UI callback, Config, CodexLobbyScene 계정 UI). WebGL 검증이 남음 |
| 계정 linking | Firebase Auth linking 요청 구현 완료. 기존 UID 유지 여부는 WebGL 실기 검증 필요 |
