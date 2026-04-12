# Garage UI/UX 개선 장기 플랜

> 생성일: 2026-04-12

이 문서는 Garage 화면의 UI/UX 개선을 위한 장기 계획이다.
단기 MVP는 기능 완성도에 집중했으며, 이제 **사용자 경험(Visual Feedback, Interactivity, Preview)**을 개선한다.

공식 진행 상태는 [`progress.md`](./progress.md)에서 관리한다.

---

## 배경

Garage MVP(Phase 7)는 기능적으로 완성됐다:
- 6슬롯 편성, 프레임/무기/기동 선택, 자동 저장, Ready 연동
- 하지만 **사용자 피드백 부족**으로 인해 UX가 딱딱함:
  - "내가 지금 어느 슬롯을 편집 중인가?" → 시각적 확신 부족
  - "저장이 됐나?" → 자동 저장이 체감되지 않음
  - "완성된 유닛이 어떻게 생겼나?" → 텍스트만으로 상상해야 함

이 문서는 이러한 문제를 단계적으로 해결하는 로드맵이다.

---

## 개선 항목

### 1. 저장 버튼 + 토스트 피드백
**문제:** 자동 저장이 체감되지 않음
**해결:**
- 우측 패널 하단에 **"Save Roster" 버튼** 추가
- 클릭 시 `SaveRosterUseCase` 실행
- 성공: **"Roster saved!"** 토스트 메시지 (2초 후 사라짐)
- 실패: 에러 메시지 표시

**우선순위:** 🔴 높음
**예상 공수:** 1-2시간

### 2. 선택 슬롯 시각 강조
**문제:** 선택된 슬롯과 비선택 슬롯 구분이 미미함
**해결:**
- 선택된 슬롯에 **밝은 색 테두리 (2px 글로우)** 추가
- 좌측 슬롯 목록에서 선택된 슬롯 왼쪽에 **▶ 화살표** 표시
- 비선택 슬롯은 어둡게 처리 (opacity 0.7)
- 슬롯 전환 시 **페이드 애니메이션** (선택 사항)

**우선순위:** 🔴 높음
**예상 공수:** 2-3시간

### 3. 조립식 파트 3D 프리뷰
**문제:** 완성된 유닛을 텍스트로만 상상해야 함
**현재 상황:** 프로젝트에 전용 3D 모델(FBX/OBJ)이 없음. `UnitFrameData.unitPrefab` 필드 선언만 있고 모두 `null` 상태.
**해결 (단계별):**

#### Step 3-A: 기본 도형 조립 프리뷰 (즉시 구현)
- 우측 패널 상단에 **3D 뷰포트** 추가 (Camera + RenderTexture)
- 프레임/무기/기동 부품을 **Unity 기본 도형**으로 표현:
  - **Frame**: 직육면체 (프레임별 색상: Striker=주황, Bastion=파랑, Relay=초록)
  - **Weapon**: 원기둥 (무기별 색상: Scatter=빨강, Pulse=노랑, Rail=보라)
  - **Thruster**: 원뿔/타원 (기동별 색상: Treads=회색, Vector=하양, Burst=초록)
- 부품을 **조립 형태**로 배치 (Frame 중심, Weapon 상단, Thruster 하단)
- **마우스 드래그로 회전** 가능
- 빈 슬롯일 때는 "?" 아이콘 또는 빈 뷰포트 표시

**우선순위:** 🟡 중간
**예상 공수:** 4-6시간

#### Step 3-B: FBX 모델 이관 (장기)
- 3D 아티스트 에셋(FBX) 준비 후 `UnitFrameData.unitPrefab` 등에 연결
- 기본 도형 → 실제 모델로 교체 (뷰포트 구조 재사용)
- Frame/Weapon/Thruster 각각 개별 프리팹으로 분리하여 조합 표현

**우선순위:** 🟢 낮음 (외부 의존)
**예상 공수:** 아트 작업 + 2-3시간 (연결 작업)

---

## 구현 계획

### Phase A: 즉시 구현 (1-2일)
| 항목 | 내용 |
|------|------|
| 저장 버튼 | `GaragePageController`에 저장 액션 추가, 토스트 UI |
| 슬롯 강조 | `GarageSlotView`에 선택 상태 시각 피드백 |
| 3D 뷰포트 골격 | `GarageUnitPreviewView` 신규 생성, Camera/RenderTexture 설정 |

### Phase B: 3D 프리뷰 완성 (2-3일)
| 항목 | 내용 |
|------|------|
| 파트 생성 로직 | 프레임/무기/기동별 기본 도형 생성 및 배치 |
| 회전 인터랙션 | 마우스 드래그 → 뷰포트 회전 |
| 색상 매핑 | ScriptableObject 데이터 기반으로 색상 자동 적용 |

### Phase C: FBX 이관 (장기, 아트 작업 완료 후)
| 항목 | 내용 |
|------|------|
| FBX 모델 연결 | `UnitFrameData.unitPrefab` 등에 실제 프리팹 연결 |
| 뷰포트 교체 | 기본 도형 → FBX 모델 교체 (구조 재사용) |

---

## 변경 파일 예상

| 파일 | 변경 내용 |
|------|-----------|
| `CodexLobbyScene.unity` | 저장 버튼, 3D 뷰포트용 Camera/RenderTexture, RawImage 추가 |
| `GaragePageController.cs` | 저장 버튼 콜백, 3D 프리뷰 Render 호출 |
| `GarageSlotItemView.cs` | 선택 상태 시각 피드백 (테두리, 화살표, opacity) |
| `GarageResultPanelView.cs` | 저장 버튼, 토스트 메시지 UI |
| `GarageUnitPreviewView.cs` | **신규**: 3D 뷰포트 컨트롤러 (파티클 생성, 회전, 색상 적용) |
| `GaragePagePresenter.cs` | ViewModel에 선택 상태 필드 추가 |
| `GaragePageViewModels.cs` | `GarageSlotViewModel`에 `IsSelected`, `ShowArrow` 필드 추가 |

---

## 상세 코드 구현 계획

### 1. 저장 버튼 + 토스트 피드백

#### 1.1 `GarageResultPanelView.cs` 수정
```csharp
// 추가 필드
[SerializeField] private Button _saveButton;
[SerializeField] private GameObject _toastPanel;
[SerializeField] private TMP_Text _toastText;
[SerializeField] private float _toastDuration = 2f;

// 추가 메서드
public void ShowToast(string message, bool isError = false)
{
    _toastText.text = message;
    _toastText.color = isError ? Color.red : Color.green;
    _toastPanel.SetActive(true);
    CancelInvoke(nameof(HideToast));
    Invoke(nameof(HideToast), _toastDuration);
}

private void HideToast() => _toastPanel.SetActive(false);

// 이벤트
public event System.Action SaveClicked;

private void OnEnable()
{
    if (_saveButton != null)
        _saveButton.onClick.AddListener(() => SaveClicked?.Invoke());
}
```

#### 1.2 `GaragePageController.cs` 수정
```csharp
// HookCallbacks()에 추가
_resultPanelView.SaveClicked += OnSaveClicked;

// 신규 메서드
private async void OnSaveClicked()
{
    var result = await _setup.SaveRoster.Execute(_state.CommittedRoster);
    if (result.IsSuccess)
    {
        _resultPanelView.ShowToast("Roster saved!");
    }
    else
    {
        _resultPanelView.ShowToast(result.Error, isError: true);
    }
}
```

---

### 2. 선택 슬롯 시각 강조

#### 2.1 `GarageSlotViewModel.cs` 수정
```csharp
// 기존 필드에 추가
public bool IsSelected { get; set; }
public bool ShowArrow { get; set; }  // 좌측 화살표 표시 여부
```

#### 2.2 `GarageSlotItemView.cs` 수정
```csharp
// 추가 필드
[Header("Selection Feedback")]
[SerializeField] private GameObject _arrowIndicator;
[SerializeField] private Image _borderImage;
[SerializeField] private Color _glowColor = new(0.24f, 0.47f, 0.89f, 0.8f);

// Render() 수정
public void Render(GarageSlotViewModel viewModel)
{
    // ... 기존 코드 ...

    // 선택 상태 시각 피드백
    if (_arrowIndicator != null)
        _arrowIndicator.SetActive(viewModel.ShowArrow);

    if (_borderImage != null)
        _borderImage.gameObject.SetActive(viewModel.IsSelected);

    if (viewModel.IsSelected)
    {
        if (_background != null)
        {
            _background.color = _selectedColor;
            if (_borderImage != null)
                _borderImage.color = _glowColor;
        }
    }
    else
    {
        // 비선택 슬롯 어둡게
        if (_background != null)
        {
            Color baseColor = viewModel.HasCommittedLoadout ? _filledColor : _emptyColor;
            _background.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0.7f);
        }
        if (_borderImage != null)
            _borderImage.gameObject.SetActive(false);
    }
}
```

#### 2.3 `GaragePagePresenter.cs` 수정
```csharp
// BuildSlotViewModels() 수정
public IReadOnlyList<GarageSlotViewModel> BuildSlotViewModels(GaragePageState state)
{
    var viewModels = new List<GarageSlotViewModel>();
    for (int i = 0; i < 6; i++)
    {
        var slot = state.CommittedRoster.GetSlot(i);
        viewModels.Add(new GarageSlotViewModel
        {
            SlotIndex = i,
            SlotLabel = $"Slot {i + 1}",
            Title = slot?.UnitName ?? $"Slot {i + 1} | Empty",
            Summary = slot?.Summary ?? "",
            HasCommittedLoadout = slot != null,
            IsSelected = state.SelectedSlotIndex == i,
            ShowArrow = state.SelectedSlotIndex == i
        });
    }
    return viewModels;
}
```

---

### 3. 조립식 파트 3D 프리뷰

#### 3.1 `GarageUnitPreviewView.cs` (신규 생성)
```csharp
using UnityEngine;
using UnityEngine.UI;
using Features.Garage.Domain;

namespace Features.Garage.Presentation
{
    public sealed class GarageUnitPreviewView : MonoBehaviour
    {
        [Header("Viewport")]
        [SerializeField] private Camera _previewCamera;
        [SerializeField] private RenderTexture _renderTexture;
        [SerializeField] private RawImage _rawImage;

        [Header("Part Prefabs (Basic Shapes)")]
        [SerializeField] private GameObject _framePrefab;    // Cube
        [SerializeField] private GameObject _weaponPrefab;   // Cylinder
        [SerializeField] private GameObject _thrusterPrefab; // Cone

        [Header("Part Colors")]
        [SerializeField] private Color _strikerColor = new(0.95f, 0.5f, 0.1f);
        [SerializeField] private Color _bastionColor = new(0.2f, 0.4f, 0.9f);
        [SerializeField] private Color _relayColor = new(0.2f, 0.8f, 0.4f);

        private GameObject _currentPreviewRoot;
        private float _rotationSpeed = 30f;
        private bool _isDragging;
        private Vector2 _lastMousePosition;

        public void Initialize()
        {
            if (_renderTexture == null)
            {
                _renderTexture = new RenderTexture(256, 256, 16);
                _previewCamera.targetTexture = _renderTexture;
                _rawImage.texture = _renderTexture;
            }
        }

        public void Render(GarageSlotViewModel viewModel, GaragePanelCatalog catalog)
        {
            DestroyCurrentPreview();
            if (!viewModel.HasCommittedLoadout) return;
            CreatePreview(viewModel, catalog);
        }

        private void CreatePreview(GarageSlotViewModel viewModel, GaragePanelCatalog catalog)
        {
            _currentPreviewRoot = new GameObject("PreviewRoot");
            _currentPreviewRoot.transform.SetParent(transform, false);

            var frameObj = CreateFrame(viewModel.FrameId, catalog);
            frameObj.transform.SetParent(_currentPreviewRoot.transform, false);
            frameObj.transform.localPosition = Vector3.zero;

            var weaponObj = CreateWeapon(viewModel.FirepowerId);
            weaponObj.transform.SetParent(_currentPreviewRoot.transform, false);
            weaponObj.transform.localPosition = new Vector3(0, 0.6f, 0);

            var thrusterObj = CreateThruster(viewModel.MobilityId);
            thrusterObj.transform.SetParent(_currentPreviewRoot.transform, false);
            thrusterObj.transform.localPosition = new Vector3(0, -0.5f, 0);
        }

        private GameObject CreateFrame(string frameId, GaragePanelCatalog catalog)
        {
            var obj = Instantiate(_framePrefab);
            obj.GetComponent<Renderer>().material.color = GetFrameColor(frameId);
            return obj;
        }

        private GameObject CreateWeapon(string firepowerId)
        {
            var obj = Instantiate(_weaponPrefab);
            obj.GetComponent<Renderer>().material.color = GetWeaponColor(firepowerId);
            obj.transform.localScale = new Vector3(0.15f, 0.3f, 0.15f);
            return obj;
        }

        private GameObject CreateThruster(string mobilityId)
        {
            var obj = Instantiate(_thrusterPrefab);
            obj.GetComponent<Renderer>().material.color = GetThrusterColor(mobilityId);
            obj.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
            return obj;
        }

        private Color GetFrameColor(string frameId) => frameId switch
        {
            "frame_striker" => _strikerColor,
            "frame_bastion" => _bastionColor,
            "frame_relay" => _relayColor,
            _ => Color.white
        };

        private Color GetWeaponColor(string firepowerId) => firepowerId switch
        {
            "fire_scatter" => new Color(0.9f, 0.2f, 0.2f),
            "fire_pulse" => new Color(0.9f, 0.9f, 0.2f),
            "fire_rail" => new Color(0.6f, 0.2f, 0.9f),
            _ => Color.white
        };

        private Color GetThrusterColor(string mobilityId) => mobilityId switch
        {
            "mob_treads" => new Color(0.5f, 0.5f, 0.5f),
            "mob_vector" => Color.white,
            "mob_burst" => new Color(0.2f, 0.8f, 0.4f),
            _ => Color.white
        };

        private void DestroyCurrentPreview()
        {
            if (_currentPreviewRoot != null)
                Destroy(_currentPreviewRoot);
        }

        private void Update()
        {
            if (_currentPreviewRoot == null) return;

            if (Input.GetMouseButtonDown(0))
            {
                _isDragging = true;
                _lastMousePosition = Input.mousePosition;
            }
            if (Input.GetMouseButtonUp(0))
                _isDragging = false;

            if (_isDragging)
            {
                Vector2 delta = (Vector2)Input.mousePosition - _lastMousePosition;
                _currentPreviewRoot.transform.Rotate(Vector3.up, delta.x * _rotationSpeed * Time.deltaTime);
                _lastMousePosition = Input.mousePosition;
            }
        }

        private void OnDestroy()
        {
            DestroyCurrentPreview();
            if (_renderTexture != null)
                _renderTexture.Release();
        }
    }
}
```

#### 3.2 `GaragePageController.cs`에 3D 프리뷰 연결
```csharp
// 필드 추가
[Required, SerializeField] private GarageUnitPreviewView _unitPreviewView;

// Initialize()에 추가
_unitPreviewView.Initialize();

// Render()에 추가
var selectedSlot = _presenter.BuildSlotViewModels(_state)[_state.SelectedSlotIndex];
_unitPreviewView.Render(selectedSlot, _catalog);
```

#### 3.3 `CodexLobbyScene.unity`에 추가할 GameObject 구조
```
GaragePageRoot
└── RightPanel
    ├── RosterStatusText
    ├── ValidationText
    ├── StatsText
    ├── SaveButton (신규)
    ├── ToastPanel (신규, 비활성화)
    │   └── ToastText
    └── UnitPreviewViewport (신규)
        ├── PreviewCamera (신규)
        └── PreviewRawImage (신규, UI)
```

---

## Assumptions

- 이번 개선은 Garage Presentation 구조 안정화 이후에 진행한다.
- 도메인 규칙(6슬롯, 3기 Ready, 자동 저장)은 변경하지 않는다.
- 3D 프리뷰 Step 3-A는 기본 도형으로 플레이스홀더 역할을 하며, Step 3-B에서 실제 FBX 모델로 교체한다.
- FBX 모델 이관은 외부 아트 작업에 의존하므로 별도 일정으로 관리한다.
