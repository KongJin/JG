# 계정 시스템 기획

> 생성일: 2026-04-11
> 최종 업데이트: 2026-04-11

## 개요

Firebase Authentication + Firestore 기반 계정 시스템.
**WebGL 호환을 위해 Firebase REST API를 직접 호출한다** (Unity SDK 미사용).
익명 로그인 기본 → 소셜(Google) 계정 업그레이드 가능.
크로스 디바이스 데이터 동기화 지원.

---

## 1. 인증 플로우

### 1.1 익명 로그인 (기본)
```
[앱 시작]
  → 로딩 화면 ("로그인 중...")
    → FirebaseAuthRestAdapter.SignInAnonymously()
      → POST https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={apiKey}
        → idToken + localId (UID) 발급
          → Firestore에서 프로필 로드 (신규 시 자동 생성)
            → Lobby 진입
```

**로그인 실패 시:**
- 자동 재시도 3회 (1초 간격)
- 3회 모두 실패 → "네트워크 연결을 확인해주세요" 메시지 + 재시도 버튼

### 1.2 계정 업그레이드 (선택)
```
[설정 화면 → 계정 업그레이드]
  → Google 로그인 (Popup/Redirect)
    → POST https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={apiKey}
         + idToken (Google)
      → 기존 익명 UID → Google 계정 링크
        → Firestore 데이터 그대로 유지
```

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
  → Google 로그인
    → 기존 UID로 로그인 (동일 계정)
      → Firestore 데이터 로드
```

### 1.5 계정 삭제
```
[설정 화면 → 계정 삭제]
  → 확인 다이얼로그 ("모든 데이터가 삭제됩니다")
    → 1. Firestore 계정 문서 전체 삭제 (profile, garage, stats, settings)
    → 2. Firebase Auth 계정 삭제 (Admin SDK 필요 or REST API)
    → 단계별 실패 시 2회 재시도
    → 완료 시 앱 초기 화면으로 복귀
```

---

## 2. 데이터 구조 (Firestore)

### 2.1 컬렉션 구조
```
accounts/{uid}/
  ├── profile
  │     ├── displayName: string           // 월 1회 변경 제한
  │     ├── lastNicknameChange: timestamp (serverTimestamp)
  │     ├── createdAt: timestamp (serverTimestamp)
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
[로컬 캐시] ←→ [Firestore REST API]
  │
  ├── 읽기: REST API → JSON 파싱 → 로컬 캐시 → 게임 사용
  ├── 쓰기: 게임 변경 → 로컬 캐시 → REST API PATCH
  └── 충돌: 단일 클라이언트 가정, last-write-wins
```

### 2.4 오프라인 대응
```
WebGL은 브라우저 sessionStorage/localStorage 활용
  - 로컬 캐시: JSON 직렬화 후 localStorage 저장
  - 네트워크 끊김: localStorage에서 읽기
  - 복구 시: localStorage → REST API 동기화
```

### 2.5 기존 로컬 데이터 마이그레이션
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
      IAuthPort.cs          // SignInAnonymously, SignInWithGoogle, SignOut, LinkWithGoogle, DeleteAccount, GetToken
      IAccountDataPort.cs   // LoadProfile, SaveProfile, LoadStats, SaveStats, LoadGarage, SaveGarage, LoadSettings, SaveSettings, DeleteAccount
    SignInAnonymouslyUseCase.cs
    LoadAccountUseCase.cs
    SaveAccountUseCase.cs
    ChangeDisplayNameUseCase.cs   // 월 1회 검증 (서버 timestamp 기준)
    DeleteAccountUseCase.cs
  Presentation/
    LoginLoadingView.cs     // 로딩 스피너 ("로그인 중...") + 실패 시 재시도 버튼
    AccountSettingsView.cs  // 단일 View 내 섹션 구분:
                            //   - 계정 정보 (UID, authType, displayName)
                            //   - 닉네임 변경 (월 1회 제한 표시)
                            //   - 계정 업그레이드 (Google 연동)
                            //   - 로그아웃
                            //   - 계정 삭제 (확인 다이얼로그)
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

### 4.2 GarageRoster 저장 전환
```
기존: GarageJsonPersistence (로컬 JSON) → 삭제
신규: FirestoreRestPort.SaveGarage() / LoadGarage()
```

### 4.3 GameSceneRoot
```csharp
// Photon 진입 시 UID 전달
PhotonNetwork.AuthValues = new AuthenticationValues {
    AuthType = CustomAuthenticationType.Custom,
    UserId = account.UID
};
```
- Photon Custom Authentication 무료 플랜 지원
- Photon Cloud Console에서 Custom Authentication 활성화 필요

### 4.4 SoundPlayer, 설정
```
기존: 로컬 PlayerPrefs 또는 하드코딩
신규: Account.settings → 볼륨, 언어 동기화
```

---

## 5. Firebase 설정

### 5.1 Firebase Console
- Authentication → 익명 로그인 활성화
- Authentication → Google 로그인 활성화
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
    public string storageBucket;        // gs://project.appspot.com
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
WebGL에서는 UnityWebRequest 또는 HttpClient 사용
  - UnityWebRequest: WebGL 호환 확실, Coroutine 기반
  - HttpClient: async/await 깔끔, WebGL 빌드 설정 필요 (linker.xml)

추천: UnityWebRequest + async wrapper
```

---

## 7. 테스트 계획

### smoke 테스트 (실제 Firebase + WebGL 빌드)
| 테스트 | 내용 |
|--------|------|
| 익명 로그인 | REST API 호출 → UID 발급 → 프로필 생성 → Lobby 진입 |
| 로그인 실패 | 네트워크 차단 → 3회 재시도 → 실패 UI |
| 탭 왕복 | Lobby → 설정 → Garage → Lobby |
| 닉네임 변경 | 변경 → 1달 이내 재시도 차단 |
| Photon 연동 | UID 기반 Custom Authentication → 룸 진입 |
| WebGL 빌드 | 실제 WebGL 빌드에서 로그인/저장 동작 확인 |

---

## 8. 진행 계획

### Phase 10: 계정 시스템

| 단계 | 내용 | 예상 공수 |
|------|------|----------|
| 10-1 | Firebase Auth REST API + 토큰 관리 (익명 로그인) | 1일 |
| 10-2 | Firestore REST API + localStorage 캐시 (CRUD) | 1일 |
| 10-3 | LoginLoadingView + AccountSettingsView | 1일 |
| 10-4 | 기존 GarageRoster 로컬 → Firestore 전환 | 1일 |
| 10-5 | 닉네임 변경 (월 1회 검증) + 설정 동기화 | 1일 |
| 10-6 | WebGL 빌드 smoke 테스트 + 문서 | 1일 |

**총 예상 공수: 6일**

---

## 9. 리스크 및 대응

| 리스크 | 영향 | 대응 |
|--------|------|------|
| WebGL에서 HttpClient 제한 | CORS, linker 문제 | UnityWebRequest + async wrapper 사용 |
| 토큰 만료 처리 누락 | Firestore 접근 401 | FirebaseAuthRestAdapter 내부 자동 갱신 (만료 5분 전) |
| localStorage 용량 제한 | 캐시 저장 실패 | 계정 데이터 작음 (~5KB), 문제 없음 |
| Firestore 오프라인 | 네트워크 끊김 시 지연 | localStorage 캐시로 읽기/쓰기, 복구 시 동기화 |
| 로그인 실패 (네트워크 불안) | 앱 진입 차단 | 자동 재시도 3회 + 재시도 버튼 |
| 계정 충돌 (동시 로그인) | 데이터 덮어쓰기 | 단일 클라이언트 가정, serverTimestamp 검증 |
| Google 로그인 한국 비중 낮음 | 실제 사용률 ↓ | 익명 로그인 기본으로 커버 |

---

## 10. 결정 사항

| 항목 | 결정 |
|------|------|
| 로그인 방식 | 하이브리드 (익명 기본 → Google 업그레이드) |
| 저장 데이터 | 편성 + 전적 + 설정 |
| 기기 변경 | 크로스 디바이스 동기화 |
| 우선순위 | Phase 9와 병행 |
| 계정 삭제 | 필요 (GDPR/스토어 심사 대응) |
| 닉네임 변경 | 월 1회 제한 (서버 timestamp 기준) |
| 로컬 데이터 이전 | 안 함 (기존 garage_roster.json 무시) |
| 로그인 화면 | 로딩 스피너 (1~2초 자동 진입) |
| 로그인 실패 | 자동 재시도 3회 + 재시도 버튼 |
| 설정 화면 구조 | 단일 View 내 섹션 구분 |
| 오프라인 대응 | WebGL localStorage 캐시 + REST API 동기화 |
| 테스트 방식 | WebGL 빌드 smoke 테스트 |
| Firebase SDK | **Unity SDK 미사용, REST API 직접 호출** |
| 빌드 사이즈 영향 | 없음 (순수 C# HTTP 통신) |
| 공수 | 6일 (핵심만) |
