# Nova1492 Resource Integration Plan

> 마지막 업데이트: 2026-04-25
> 상태: reference
> doc_id: plans.nova1492-resource-integration
> role: plan
> owner_scope: Nova1492 설치 리소스를 JG Unity 프로젝트에 들여오는 실행 순서, 분류 기준, 검증 기준
> upstream: docs.index, design.game-design, ops.document-management-workflow
> artifacts: `Assets/Art/Nova1492/`, `Assets/Audio/Nova1492/`, `artifacts/nova1492/`

## 목적

Nova1492 설치본의 원본 이미지, UI, 사운드, 음악 리소스를 JG 프로젝트의 Garage/전투 프로토타입 리소스로 가져와, 현재 게임 방향인 `Nova1492식 유닛 조립 감각 + Clash Royale식 짧은 소환 전투`를 더 빠르게 체감 가능하게 만든다.

이 문서는 원본 리소스 사용 권리/허가가 확보된 전제를 둔다. 권리 판단 자체는 이 문서가 소유하지 않고, 실제 배포 전에 별도 release gate에서 재확인한다.

## 현재 상태

- 설치 폴더는 확인했고, 전체 5,999개 파일 인벤토리를 `artifacts/nova1492/resource_inventory.csv`로 고정했다.
- 확장자별 요약은 `artifacts/nova1492/resource_inventory_summary.md`에 남겼다.
- Garage 후보 40개와 unit/effect 후보 40개를 분류했고, 각각 후보 목록과 contact sheet를 생성했다.
- Unity 프로젝트 내부에는 첫 staging 후보 40개만 `Assets/Art/Nova1492/` 아래로 복사했고, `Garage Backgrounds / Accents / CommonPanels`, `Units/Atlases`, `Effects/Atlases`, `Reference/Textures`로 정리했다.
- staging 후보 40개는 Unity TextureImporter 기준 `Sprite (2D and UI)` 설정을 적용했고, 결과를 `artifacts/nova1492/unity_import_settings_report.md`에 남겼다.
- Garage 첫 적용 후보 세트는 `artifacts/nova1492/garage_first_application_selection.md`에 고정했다.
- Audio 첫 적용 후보로 `.wav` SFX 11개와 `.mp3` BGM 3개를 `Assets/Audio/Nova1492/`에 staging했고, `SoundCatalog.asset`에 `ui_*`, `garage_select`, `battle_*`, `core_*`, `bgm_*` 키를 연결했다.
- 후속 조사에서 `.GX` 871개 중 865개를 OBJ로 변환했고, `Assets/Art/Nova1492/GXConverted/` 아래에 category별로 정리했다. 변환/분류 결과는 `artifacts/nova1492/gx_conversion_summary.md`, `artifacts/nova1492/gx_asset_classification_summary.md`를 기준으로 본다.
- `LobbyScene` 적용 순서는 별도 active plan인 [`lobby_scene_nova1492_model_application_plan.md`](./lobby_scene_nova1492_model_application_plan.md)가 맡고, 첫 shortlist 산출물은 `artifacts/nova1492/lobby_model_shortlist.csv`다.
- 아직 prefab 적용과 WebGL 이미지/오디오 로드 검증은 하지 않았다.
- `npm run --silent rules:lint`는 통과했다.
- `Invoke-UnityUiAuthoringWorkflowPolicy.ps1`는 현재 dirty worktree의 UI prefab/evidence 변경 감지 때문에 blocked다. 이번 Nova pass는 새 UI prefab을 만들지 않았고, 기존 Garage prefab도 수정하지 않았다.
- Phase 0~2는 완료됐고, Phase 5의 SFX/BGM 코드·리소스 첫 pass도 완료했다. Phase 3/4 prefab 적용과 WebGL 오디오 로드 검증은 남아 있다.

## 범위

포함:

- Nova1492 설치 리소스의 인벤토리 작성
- Garage와 전투 HUD에 쓸 후보 이미지/사운드 선정
- 선정 후보의 Unity staging import
- Garage prototype과 전투 소환 UX에 제한 적용
- 적용 결과의 capture, Console, WebGL smoke 기준 검증

제외:

- Nova1492 게임 규칙, 밸런스, 데이터 구조를 그대로 복제하는 작업
- `.xfi`, `.dat` 변환기를 이 계획 안에서 구현하는 작업
- 변환된 `.GX` 모델을 `LobbyScene`/Garage preview에 실제 적용하는 작업. 이 작업은 [`lobby_scene_nova1492_model_application_plan.md`](./lobby_scene_nova1492_model_application_plan.md)에서 다룬다.
- 원본 리소스 전체를 한 번에 Unity로 import하는 작업
- 기존 Set B Garage recovery surface를 검증 없이 덮어쓰는 작업

## 현재 확인된 원본 위치

설치 경로:

- `C:\Program Files (x86)\Nova1492`

초기 인벤토리:

| 위치 | 관찰 내용 | 우선 용도 |
|---|---|---|
| `datan/common` | `.bmp`, `.tga`, `.GX`, `.xfi` 다수. 유닛/이펙트/부품으로 보이는 파일 포함 | 유닛, 부품, 전투 이펙트 후보 |
| `datan/kr` | 로비, 상점, 연구소, 개조, 스킬 아이콘, 텍스트 데이터 | Garage/로비 UI 레퍼런스 및 임시 UI 리소스 |
| `datan/sound` | `.wav` 448개 | UI 클릭, 소환, 피격, 전투 효과음 |
| `datan/music` | `.mp3` 트랙 | 로비/전투 BGM 후보 |
| `datan/ui` | `confirm.json` 확인 | UI 구조 참고 후보 |

확장자별 초기 규모:

| 확장자 | 개수 | 메모 |
|---|---:|---|
| `.bmp` | 1558 | UI/아이콘/스프라이트 후보 |
| `.tga` | 1385 | 스프라이트/이펙트 후보 |
| `.GX` | 871 | 후속 converter prototype으로 865개 OBJ 변환 성공, 적용 shortlist는 별도 LobbyScene plan/artifact에서 제한 관리 |
| `.xfi` | 859 | 메타/프레임/포맷 보조 파일 가능성 있음 |
| `.wav` | 448 | 효과음 |
| `.png` | 65 | 바로 사용 가능한 이미지 후보 |
| `.jpg` | 13 | 배경/로딩 이미지 후보 |
| `.mp3` | 13 | 음악 |

## 적용 원칙

- 원본 리소스는 먼저 `inventory -> staging -> Unity import -> prefab 적용` 순서로 들여온다.
- 원본 설치 폴더를 Unity가 직접 참조하지 않는다.
- Unity 프로젝트 내부 복사 경로는 원본 출처가 보이도록 `Nova1492` namespace를 붙인다.
- Garage에는 먼저 `부품 조립 감각`을 살리는 UI/아이콘/부품 리소스를 적용한다.
- 전투에는 먼저 `소환 슬롯`, `앵커 반경 프리뷰`, `유닛 실루엣`, `피격/소환 효과음`에 적용한다.
- `.GX`는 후속 조사로 OBJ 변환 후보가 생겼지만, 바로 runtime truth로 승격하지 않고 preview 후보로 제한 검토한다.
- `.xfi`, `.dat`은 바로 runtime에 넣지 않고 포맷 조사 대상으로 분리한다.
- 최종 배포 빌드 포함 여부는 public/release 직전 gate에서 다시 확인한다. 개발용 prototype과 내부 WebGL smoke는 이 계획의 실행 범위에 둔다.

## 권장 Unity 경로

| Unity 경로 | 내용 |
|---|---|
| `Assets/Art/Nova1492/UI/Garage/Backgrounds/` | 로비, Garage, 연구소, 상점 계열 전체 패널/배경 후보 |
| `Assets/Art/Nova1492/UI/Garage/Accents/` | 선택 상태, 트림, 패널 하이라이트용 crop 후보 |
| `Assets/Art/Nova1492/UI/Garage/CommonPanels/` | `datan/common` 계열 Garage/부품 패널 후보 |
| `Assets/Art/Nova1492/Units/Atlases/` | 유닛/파츠/실루엣 스프라이트 시트 |
| `Assets/Art/Nova1492/Effects/Atlases/` | 미사일, 폭발, 전투 이펙트 후보 |
| `Assets/Art/Nova1492/Reference/Textures/` | 용도 미확정 base/texture 후보. runtime 적용 전 추가 판단 필요 |
| `Assets/Art/Nova1492/Backgrounds/` | 로딩, 로비, 실험실, 상점 계열 배경 |
| `Assets/Audio/Nova1492/Sfx/` | `.wav` 효과음 |
| `Assets/Audio/Nova1492/Music/` | `.mp3` 음악 |
| `artifacts/nova1492/` | 인벤토리, 후보 목록, 변환 로그, 스크린샷 증거 |

## 실행 단계

### Phase 0: 인벤토리 고정

상태: 완료

목표:

- 설치 폴더에서 파일 목록, 크기, 확장자, 해시를 추출해 기준 인벤토리를 만든다.
- Garage/전투/사운드/포맷조사 후보로 1차 분류한다.

산출물:

- `artifacts/nova1492/resource_inventory.csv`
- `artifacts/nova1492/resource_inventory_summary.md`

Acceptance:

- 파일 수와 확장자별 집계가 현재 설치본과 일치한다.
- 원본 경로, Unity 후보 경로, 용도 태그가 최소 1개 이상 기록된다.
- 이후 phase에서 복사할 후보와 조사만 할 후보가 분리된다.

### Phase 1: Garage 우선 후보 선정

상태: 완료

목표:

- `datan/kr`, `datan/common`에서 Garage에 바로 체감되는 리소스를 고른다.
- `remodel`, `lab`, `shop`, `SkillIcon`, `base`, `part`, `unit` 계열을 우선 탐색한다.

선정 기준:

- 조립 작업대 분위기를 만들 수 있는가
- 부품/프레임/모듈 개념을 플레이어가 빠르게 읽을 수 있는가
- 모바일 UI에서 작은 슬롯으로 줄여도 식별 가능한가

산출물:

- `artifacts/nova1492/garage_candidate_assets.md`
- 후보 이미지 contact sheet 또는 캡처

Acceptance:

- Garage 적용 1차 후보가 UI 배경/패널, 부품 아이콘, 버튼/슬롯, 역할 아이콘으로 분류된다.
- 각 후보에 원본 경로와 Unity 목표 경로가 기록된다.
- 바로 적용할 후보는 20~40개로 제한된다.

### Phase 2: Unity staging import

상태: 완료

목표:

- Phase 1 후보만 Unity 프로젝트 내부로 복사한다.
- `.bmp`, `.tga`, `.png`, `.jpg`, `.wav`, `.mp3`의 import setting을 Unity 용도에 맞춘다.

기본 import 방향:

- UI/아이콘: `Sprite (2D and UI)`
- 전투 스프라이트/이펙트: `Sprite`, 필요 시 atlas 후보
- 배경: 압축 품질 유지, 모바일 크기 제한 검토
- 효과음: 짧은 `.wav`는 Decompress/Compressed 정책을 용도별로 분리
- 음악: Streaming 또는 Compressed In Memory 후보로 검토

Acceptance:

- Unity Console에 import error가 없다.
- Garage 후보 리소스가 Project 창 기준으로 의미 있는 폴더 구조에 들어간다.
- 원본 설치 폴더 의존 없이 Unity 프로젝트만으로 로드 가능하다.
- 복사된 파일 목록과 원본 경로 대응표가 `artifacts/nova1492/`에 남는다.

### Phase 3: Garage prototype 적용

상태: 후보 선정 완료, prefab 적용 전

목표:

- `GaragePageRoot` 또는 현재 Garage runtime surface에 원본 리소스 일부를 적용한다.
- 기능 구현보다 첫인상과 조립 감각을 우선 확인한다.

적용 우선순위:

1. 로스터 슬롯 또는 작업대 배경
2. 프레임/모듈/스킬 아이콘 후보
3. 선택 상태와 비교 상태의 시각 강조
4. 저장/확정 CTA의 무게감

Acceptance:

- 첫 화면에서 Garage가 수집 목록보다 조립 작업대로 읽힌다.
- 현재 로스터 3~6기와 선택 유닛 편집 영역이 원본 리소스 덕분에 더 명확해진다.
- Set B Garage visual fidelity 판단과 충돌하지 않고, 필요하면 별도 Nova resource variant로 분리된다.
- 기존 Set B recovery 판단이 끝나기 전에는 직접 덮어쓰기보다 variant 비교를 기본값으로 둔다.

### Phase 4: 전투 HUD/소환 프로토타입 적용

목표:

- 전투 슬롯, 에너지, 소환 가능/불가 상태, 앵커 반경 프리뷰에 Nova 리소스 후보를 적용한다.
- 원본 유닛/이펙트 이미지를 통해 `내가 조립한 유닛을 전장에 내려놓는 감각`을 강화한다.

Acceptance:

- 유닛 슬롯이 비용, 역할, 준비 상태를 더 빠르게 읽히게 한다.
- 선택한 유닛의 배치 가능 영역과 앵커 반경이 전투 오브젝트와 섞이지 않는다.
- 모바일 viewport에서 텍스트/아이콘/전장 오브젝트가 겹치지 않는다.
- 적용 대상은 HUD/slot/preview에 한정하고, 전투 밸런스나 AI 변경은 별도 작업으로 분리한다.

### Phase 5: 사운드/BGM 적용

상태: 코드/리소스 첫 pass 완료, WebGL 재생 검증 전

목표:

- UI 클릭, 소환, 배치 실패, 피격, 코어 위험, 승패 피드백에 `.wav` 후보를 연결한다.
- 로비/Garage/전투 BGM 후보를 분리한다.

산출물:

- `Assets/Audio/Nova1492/Sfx/`
- `Assets/Audio/Nova1492/Music/`
- `Assets/Data/Sound/SoundCatalog.asset`
- `artifacts/nova1492/audio_staging_manifest.md`

Acceptance:

- 같은 액션의 사운드가 중복 재생으로 지저분해지지 않는다.
- Garage와 전투의 사운드 톤이 구분된다.
- WebGL 빌드에서 오디오 로드/재생 문제가 없다.
- BGM은 우선 후보 지정과 재생 검증까지만 하고, 최종 믹싱/루프 편집은 별도 오디오 polish 작업으로 분리한다.

### Phase 6: 포맷 조사

상태: `.GX` 1차 조사/변환 완료, `.xfi/.dat/.ifl` 조사 전

목표:

- `.GX`, `.xfi`, `.dat`, `.ifl`의 용도를 조사하되, runtime 적용과 분리한다.
- `.GX`는 converter prototype 결과를 reference로 두고, 실제 LobbyScene 적용은 별도 plan으로 제한한다.
- 텍스트/부품 설명 데이터가 추출 가능하면 Garage copy나 태그 설계 참고로만 먼저 사용한다.

Acceptance:

- 포맷별로 `사용 가능`, `변환 필요`, `보류` 상태가 기록된다.
- 변환 스크립트나 적용 계획이 필요하면 별도 plan 또는 tool artifact로 분리한다.
- `.dat` 텍스트/부품 설명은 첫 pass에서 레퍼런스로만 두고, 현재 유닛/모듈 데이터 구조에 자동 병합하지 않는다.

## 검증 루프

기본 검증:

- Unity import 후 Console error 확인
- Garage prefab 또는 scene capture 비교
- 모바일 기준 viewport capture
- WebGL smoke에서 이미지/오디오 로드 확인

문서/운영 검증:

- 새 리소스 경로가 `docs/index.md`와 관련 plan에서 찾을 수 있다.
- 진행 상태가 바뀌면 `docs/plans/progress.md`에만 현재 상태를 갱신한다.
- 리소스 인벤토리 결과는 `artifacts/nova1492/`에 두고, 계획 문서가 파일 목록 전체를 중복 소유하지 않는다.

## 리스크와 처리

| 리스크 | 처리 |
|---|---|
| 원본 리소스가 너무 많아 Unity import가 무거워짐 | Phase별 후보만 staged import |
| `.bmp/.tga` alpha 또는 색상 처리 문제 | contact sheet와 Unity preview로 확인 후 importer preset 조정 |
| `.GX` 변환 모델의 scale/pivot/material 불안정 | LobbyScene 적용 전 shortlist와 preview prefab pack에서 검증 |
| `.xfi/.dat/.ifl` 포맷 미해석 | 즉시 적용 대상에서 제외하고 조사 phase로 분리 |
| 기존 Set B Garage fidelity와 충돌 | 기존 recovery surface를 덮지 않고 Nova resource variant로 먼저 비교 |
| WebGL 용량 증가 | 후보 단위로 적용하고 빌드 사이즈를 phase별 기록 |
| 배포 권리 확인 누락 | release gate에서 원본 리소스 포함 여부를 재확인 |

## 첫 실행 권장 순서

1. `artifacts/nova1492/` 인벤토리 생성
2. `datan/kr`의 Garage UI 후보 contact sheet 생성
3. `datan/common`의 부품/유닛 후보 contact sheet 생성
4. 후보 20~40개만 `Assets/Art/Nova1492/UI/`와 `Assets/Art/Nova1492/Units/`에 staging import
5. Garage prefab variant 또는 현재 Set B Garage surface에 제한 적용
6. 캡처 비교 후 계속 가져갈 리소스와 버릴 리소스 분리

## 기본 결정

- 첫 pass는 `Garage visual variant`를 기본값으로 둔다. 기존 Set B recovery surface에 직접 덮어쓰는 것은 비교 capture 이후 결정한다.
- `.dat` 텍스트/부품 설명은 레퍼런스로만 쓰고, 현재 유닛/모듈 데이터 구조와 자동 병합하지 않는다.
- 개발용 prototype과 내부 WebGL smoke에는 원본 리소스를 포함할 수 있다. public/release 빌드는 배포 직전 gate에서 포함 여부를 다시 확인한다.

## Plan Rereview

- 2026-04-25 1차 리뷰: 부족한점 발견. 범위 제외, phase gate, 기본 결정이 약했다.
- 2026-04-25 1차 반영: 제외 범위, 현재 상태, phase별 acceptance 보강, Open Questions를 기본 결정으로 전환했다.
- 2026-04-25 2차 리뷰: plan rereview: clean. 과한점/부족한점 없음.
- 2026-04-25 실행 시작 후 상태 갱신: Phase 0~1 완료와 Phase 2 staging copy 완료를 기록했다. prefab 적용/Unity import 검증은 남은 일로 유지한다.
- 2026-04-25 import 설정 완료 후 상태 갱신: Phase 2를 완료로 전환했고, Phase 3은 후보 선정 완료/prefab 적용 전 상태로 고정했다.
- 2026-04-25 resource 정리: 첫 staging 40개를 용도별 하위 폴더로 재배치했고, `staging_copy_manifest`와 첫 적용 후보 경로를 새 구조에 맞췄다.
- 2026-04-25 audio 첫 pass: SFX 11개/BGM 3개를 제한 staging하고, 기존 `SoundPlayer + SoundCatalog + SoundRequestEvent` 경로에 SFX/BGM 채널, 씬 BGM 전환, 계정 볼륨 소비를 연결했다. WebGL 로드/재생 검증은 후속으로 남긴다.
- 2026-04-25 검증 메모: docs lint와 compile check는 통과했다. Unity UI authoring policy는 기존 dirty worktree의 Lobby prefab 감지로 blocked이며, Nova 리소스 import 자체의 실패는 아니다.
