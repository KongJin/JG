# Lobby UI Design Review
## NOVA1492 Lobby Screen - Core Selection

**Review Date:** 2026-05-04
**Surface:** `lobby-core-screen`
**Screenshot:** `스크린샷 2026-05-04 205400.png`

---

## 📊 Summary

| Aspect | Rating | Notes |
|--------|--------|-------|
| Functionality | ⭐⭐⭐⭐ | All necessary features present |
| Visual Hierarchy | ⭐⭐⭐ | Clear but could be stronger |
| Visual Impact | ⭐⭐ | Preview area needs enhancement |
| Information Density | ⭐⭐ | Can feel cluttered |
| Interaction Design | ⭐⭐⭐ | Functional but lacks feedback |

**Overall:** 7/10 - Functional foundation, needs visual polish

---

## 🎯 Strengths

1. **Clear Navigation Structure**
   - Four main tabs (GARAGE, LOBBY, BATTLE, MYPAGE) provide clear navigation
   - Active tab is visually indicated

2. **Consistent Design Language**
   - Blue color theme is maintained throughout
   - Consistent typography and spacing

3. **Information Visibility**
   - All key information visible without navigation
   - Core list, detail, and preview all on one screen

4. **Functional Layout**
   - List-detail pattern is a proven pattern for item selection
   - Stats are clearly labeled

---

## ⚠️ Weaknesses

### 1. Low Visual Hierarchy
```
PROBLEM: Header blends with content
SOLUTION: Add background color/gradient separation
```

### 2. Preview Area Too Small
```
PROBLEM: 3D preview gets lost in the layout
SOLUTION: Make preview 40-50% of screen with background effects
```

### 3. High Information Density
```
PROBLEM: List + Detail + Preview all compete for attention
SOLUTION: Consider progressive disclosure or larger layout
```

### 4. Subtle Selection Feedback
```
PROBLEM: Selected item highlight could be stronger
SOLUTION: Add glow, border, or background highlight
```

### 5. Low Contrast
```
PROBLEM: UI elements blend with background
SOLUTION: Enhance contrast with darker/lighter shades
```

---

## 🎨 Design Improvements

### Priority 1: Enhance Preview Area

**Before:**
```
┌─────────────┐  ┌─────────────┐
│  Small      │  │   Detail    │
│  Preview    │  │   Panel     │
│  (~15%)     │  │             │
└─────────────┘  └─────────────┘
```

**After:**
```
┌─────────────────────────────┐
│                             │
│     LARGE 3D PREVIEW        │
│     (50% of screen)         │
│     With rotation hint      │
│     and background fx       │
│                             │
└─────────────────────────────┘
```

### Priority 2: Visual Stat Representation

**Before:**
```
Energy: 300/300
Armor: 250/250
```

**After:**
```
Energy  ████████████████████  300/300
Armor   ██████████████████    250/250
```

### Priority 3: Interactive Feedback

Add to all interactive elements:
- Hover state (brightness/color shift)
- Active state (scale down, darken)
- Selection state (border, glow)
- Transition animation (100-200ms)

---

## 📐 Layout Alternatives

### Option A: Preview-First (Recommended)
```
┌─────────────────────────────────────┐
│           HEADER                    │
├─────────────────────────────────────┤
│                                     │
│         LARGE PREVIEW               │
│      with overlay controls          │
│                                     │
├─────────────────────────────────────┤
│  List (scrollable)  │  Detail      │
└─────────────────────────────────────┘
```

### Option B: Tabbed Detail
```
┌─────────────────────────────────────┐
│           HEADER                    │
├─────────────────────────────────────┤
│  ┌─────────┐  ┌─────────────────┐  │
│  │ Preview │  │    Detail      │  │
│  │         │  │                 │  │
│  └─────────┘  └─────────────────┘  │
├─────────────────────────────────────┤
│  CORE LIST (Full width)             │
│  [CORE-01] [CORE-02] [CORE-03]...   │
└─────────────────────────────────────┘
```

### Option C: Card Grid
```
┌─────────────────────────────────────┐
│           HEADER                    │
├─────────────────────────────────────┤
│  ┌─────────┐  ┌─────────┐          │
│  │ CORE-01 │  │ CORE-02 │          │
│  │  Lv.50  │  │  Lv.50  │          │
│  └─────────┘  └─────────┘          │
│  ┌─────────┐  ┌─────────┐          │
│  │ CORE-03 │  │ CORE-04 │          │
│  │  Lv.50  │  │  Lv.50  │          │
│  └─────────┘  └─────────┘          │
└─────────────────────────────────────┘
```

---

## 🎨 Suggested Color Palette

```css
:root {
  /* Primary Blue Theme */
  --color-primary: #2196F3;
  --color-primary-dark: #1976D2;
  --color-primary-light: #64B5F6;

  /* Accent */
  --color-accent: #FFC107;

  /* Backgrounds */
  --color-bg-dark: #0D1B2A;
  --color-bg-surface: #1B263B;
  --color-bg-elevated: #263547;

  /* Text */
  --color-text-primary: #FFFFFF;
  --color-text-secondary: #B0BEC5;
  --color-text-muted: #607D8B;

  /* Stats by Type */
  --color-stat-energy: #4FC3F7;
  --color-stat-armor: #FFB74D;
  --color-stat-attack: #EF5350;
  --color-stat-speed: #66BB6A;

  /* States */
  --color-selected-bg: rgba(33, 150, 243, 0.2);
  --color-selected-border: #2196F3;
  --color-hover-bg: rgba(255, 255, 255, 0.05);
}
```

---

## 🧩 UI Toolkit Implementation Notes

### Suggested UXML Structure

```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements" ...>
  <ui:VisualElement name="LobbyRoot" class="lobby-root">
    <!-- Header -->
    <ui:VisualElement name="HeaderContainer" class="header-container">
      <ui:TextElement name="TitleLabel" class="title-label">NOVA1492</ui:TextElement>
      <ui:TextElement name="SubtitleLabel" class="subtitle-label">
        당신만의 로봇을 커스터마이즈하세요
      </ui:TextElement>
      <ui:VisualElement name="UtilityButtons" class="utility-buttons">
        <ui:Button name="SearchButton" class="icon-button">🔍</ui:Button>
        <ui:Button name="SettingsButton" class="icon-button">⚙️</ui:Button>
      </ui:VisualElement>
    </ui:VisualElement>

    <!-- Content -->
    <ui:VisualElement name="ContentContainer" class="content-container">
      <!-- Preview -->
      <ui:VisualElement name="PreviewContainer" class="preview-container">
        <ui:VisualElement name="RenderTexture" class="render-texture" />
        <ui:TextElement name="RotationHint" class="rotation-hint">
          ← → 드래그하여 회전
        </ui:TextElement>
      </ui:VisualElement>

      <!-- Detail Panel -->
      <ui:VisualElement name="DetailContainer" class="detail-container">
        <ui:TextElement name="ItemName" class="item-name">CORE-01</ui:TextElement>
        <ui:VisualElement name="StatBars" class="stat-bars">
          <ui:VisualElement name="EnergyBar" class="stat-bar">
            <ui:TextElement class="stat-label">Energy</ui:TextElement>
            <ui:VisualElement class="bar-fill" style="width: 100%;" />
            <ui:TextElement class="stat-value">300/300</ui:TextElement>
          </ui:VisualElement>
          <!-- More stats... -->
        </ui:VisualElement>
        <ui:VisualElement name="ActionButtons" class="action-buttons">
          <ui:Button name="EquipButton" class="action-button primary">장착</ui:Button>
          <ui:Button name="CompareButton" class="action-button">비교</ui:Button>
        </ui:VisualElement>
      </ui:VisualElement>

      <!-- List -->
      <ui:ScrollView name="ListContainer" class="list-container">
        <ui:VisualElement name="CoreItem" class="core-item selected">
          <ui:TextElement class="item-name">CORE-01</ui:TextElement>
          <ui:TextElement class="item-level">Lv.50</ui:TextElement>
          <ui:VisualElement class="mini-stats">...</ui:VisualElement>
        </ui:VisualElement>
        <!-- More items... -->
      </ui:ScrollView>
    </ui:VisualElement>

    <!-- Navigation -->
    <ui:VisualElement name="NavigationContainer" class="navigation-container">
      <ui:Button name="GarageTab" class="nav-tab">🚗 GARAGE</ui:Button>
      <ui:Button name="LobbyTab" class="nav-tab active">🎯 LOBBY</ui:Button>
      <ui:Button name="BattleTab" class="nav-tab">⚔️ BATTLE</ui:Button>
      <ui:Button name="MypageTab" class="nav-tab">👤 MYPAGE</ui:Button>
    </ui:VisualElement>
  </ui:VisualElement>
</ui:UXML>
```

### Key USS Suggestions

```css
/* Preview Enhancement */
.preview-container {
  background: linear-gradient(135deg, #1B263B 0%, #0D1B2A 100%);
  border-radius: 12px;
  box-shadow: 0 8px 32px rgba(0, 0, 0, 0.3);
  flex-grow: 2; /* Takes more space */
}

/* Selected Item */
.core-item.selected {
  background-color: var(--color-selected-bg);
  border-left: 4px solid var(--color-selected-border);
}

/* Stat Bar Animation */
.stat-bar .bar-fill {
  transition: width 0.3s ease-out;
}

/* Button Feedback */
.action-button:hover {
  transform: scale(1.02);
  filter: brightness(1.1);
}

.action-button:active {
  transform: scale(0.98);
}

/* Active Tab */
.nav-tab.active {
  color: var(--color-primary);
  border-bottom: 3px solid var(--color-primary);
}

/* Smooth Transitions */
* {
  transition: background-color 0.15s ease, border-color 0.15s ease;
}
```

---

## ✅ Acceptance Criteria

- [ ] Preview area is prominent (at least 30% of screen space)
- [ ] Selected item has clear visual distinction
- [ ] Stats have visual representation (bars, not just text)
- [ ] All interactive elements have hover/active states
- [ ] Navigation clearly indicates current tab
- [ ] Color contrast meets WCAG AA standards
- [ ] Transitions feel smooth (100-200ms)
- [ ] 3D preview supports rotation interaction
- [ ] Layout is responsive for target resolutions

---

## 📋 Next Steps

1. **Define component structure**
   - Create CoreListItem, StatBar, TabButton components

2. **Implement color system**
   - Add USS variables for color palette

3. **Build preview integration**
   - Connect RenderTexture to 3D preview
   - Add rotation controls

4. **Add animations**
   - Transition effects for state changes
   - Stat bar fill animations

5. **Test on devices**
   - Verify responsive layout
   - Check touch target sizes
   - Validate contrast ratios

---

*Generated via Stitch Unity Design Review*
*Artifact: `artifacts/stitch/lobby-ui-design-review.md`
