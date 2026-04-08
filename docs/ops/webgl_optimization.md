# WebGL Draw Call Optimization Strategy

WebGL 빌드의 드로우콜 최적화 전략. 프로젝트 규모에 맞춰 단계적으로 적용한다.

---

## 현재 베이스라인 (2026-03)

| 항목 | 설정 | 비고 |
|---|---|---|
| 렌더 파이프라인 | URP 17.3 (Mobile_RPAsset) | WebGL은 Mobile 품질 티어 사용 |
| SRP Batcher | ON | 셰이더 단위 배칭. 가장 중요한 최적화 |
| Static Batching | OFF | 활성화 필요 |
| Dynamic Batching | OFF | WebGL에서 CPU 오버헤드 > 이득. 끄는 게 올바름 |
| GPU Instancing | 미설정 | 반복 오브젝트에 적용 필요 |
| 렌더 스케일 | 0.8x | GPU 부하 20% 절감 |
| 포스트 프로세싱 | 없음 (Mobile) | PC만 SSAO 사용 |
| 오클루전 컬링 | 데이터 미베이크 | 씬 규모 커지면 베이크 |
| LOD | 없음 | 현재 씬 규모에서는 불필요 |
| 텍스처 아틀라스 | 없음 | UI/이펙트 증가 시 도입 |

---

## Phase 1: 즉시 적용 (설정 변경)

### 1-1. Static Batching 활성화

- **위치**: Player Settings > Other Settings > Static Batching (WebGL 탭)
- **효과**: 움직이지 않는 메시(바닥, 벽, 배경)를 하나의 드로우콜로 합침
- **조건**: 씬에서 해당 오브젝트를 Static으로 마킹해야 동작
- **트레이드오프**: 메모리 사용량 증가 (합쳤을 때의 메시 데이터를 별도 저장)

### 1-2. GPU Instancing 활성화

- **대상**: 반복 생성되는 오브젝트의 머티리얼
  - 투사체 (`ProjectilePhysicsAdapter` 프리팹)
  - 이펙트 (`ZoneEffect`, `SelfEffect`, `TargetedEffect` 프리팹)
- **방법**: 머티리얼 Inspector > Enable GPU Instancing 체크
- **효과**: 같은 메시+머티리얼 조합을 한 번의 드로우콜로 여러 개 렌더링
- **주의**: SRP Batcher와 GPU Instancing은 동시에 동작하지 않음. URP Lit 셰이더 기준 SRP Batcher가 우선됨. 커스텀 셰이더나 파티클에서 Instancing이 유효

---

## Phase 2: 씬이 커질 때

### 2-1. 머티리얼/셰이더 통합

- **원칙**: SRP Batcher는 셰이더 단위로 배칭하므로, **셰이더 종류를 최소화**하는 것이 핵심
- **실행**:
  - 환경 오브젝트는 가능한 한 URP/Lit 하나로 통일
  - 텍스처만 다른 경우 텍스처 아틀라스로 묶어 머티리얼 1개로 합침
  - 커스텀 셰이더(RoundedCorners 등)는 UI에만 한정

### 2-2. 텍스처 아틀라스

- **대상**: UI 아이콘, 스킬 아이콘, 이펙트 스프라이트
- **방법**: Unity SpriteAtlas 또는 수동 아틀라스
- **효과**: 텍스처 바인딩 횟수 감소 → 드로우콜 감소

### 2-3. 오클루전 컬링 베이크

- **조건**: 맵에 벽/구조물이 생겨 시야 차단이 발생할 때
- **방법**: Window > Rendering > Occlusion Culling > Bake
- **효과**: 카메라에 보이지 않는 오브젝트를 렌더링에서 제외

---

## Phase 3: 콘텐츠 본격 확장 시

### 3-1. LOD 그룹

- **조건**: 폴리곤 수가 많은 3D 모델이 추가될 때
- **방법**: LOD Group 컴포넌트 + 거리별 저폴리 메시
- **효과**: 먼 거리 오브젝트의 버텍스 처리량 감소

### 3-2. 셰이더 배리언트 관리

- **현재**: Global Settings에서 StripUnusedVariants 활성화됨
- **확장**: `ShaderVariantCollection`으로 필요한 배리언트만 명시적으로 보존
- **효과**: 빌드 사이즈 감소, 셰이더 컴파일 시간 감소

### 3-3. 라이트맵 최적화

- **현재**: Baked Lightmap 활성화, 아틀라스 1024px
- **확장**: Lightmap Resolution을 씬 복잡도에 맞게 조절
- **주의**: WebGL에서 라이트맵 텍스처가 크면 메모리 압박

---

## WebGL 특화 주의사항

### 피해야 할 것

- **Dynamic Batching**: WebGL에서 CPU 바운드 배칭은 이득보다 오버헤드가 큼
- **실시간 그림자 남용**: Mobile_RPAsset이 1 cascade + soft shadow OFF로 이미 최적화됨. 추가 라이트 그림자는 지양
- **과도한 포스트 프로세싱**: Mobile 파이프라인에 Renderer Feature 추가 자제
- **큰 텍스처**: WebGL 메모리 제한. 텍스처는 2048px 이하 권장, 가능하면 1024px

### 모니터링 방법

- **Unity Profiler**: WebGL Development Build + Autoconnect Profiler
- **Frame Debugger** (에디터): Window > Analysis > Frame Debugger로 드로우콜 개별 확인
- **브라우저 DevTools**: Performance 탭에서 프레임 타임 측정
- **Stats 패널**: Game 뷰 Stats로 Batches/SetPass calls 실시간 확인

### 목표 지표

| 지표 | 목표 | 비고 |
|---|---|---|
| SetPass Calls | < 50 | 셰이더 교체 횟수 |
| Batches | < 100 | 실제 드로우콜 |
| Triangles | < 100K | 프레임당 폴리곤 |
| 프레임 타임 | < 33ms (30fps) | WebGL 최소 목표 |

---

## 적용 우선순위 요약

```
[즉시]  Static Batching ON → GPU Instancing 설정
[중기]  머티리얼 통합 → 텍스처 아틀라스 → 오클루전 컬링
[장기]  LOD → 셰이더 배리언트 → 라이트맵 튜닝
```

각 Phase 적용 후 Frame Debugger로 드로우콜 변화를 측정하고 기록한다.
