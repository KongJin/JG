using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Features.Garage.Presentation
{
    /// <summary>
    /// Garage Set B UI 어댑터 인터페이스.
    /// 테스트 가능성을 위해 MonoBehaviour 의존성을 제거.
    /// </summary>
    public interface IGarageSetBUitkAdapter : System.IDisposable
    {
        event Action<int> SlotSelected;
        event Action<GarageEditorFocus> PartFocusSelected;
        event Action<string> PartSearchChanged;
        event Action<GarageNovaPartSelection> PartOptionSelected;
        event Action SaveRequested;
        event Action SettingsRequested;

        bool Bind(VisualElement root);
        void Render(
            IReadOnlyList<GarageSlotViewModel> slots,
            GarageNovaPartsPanelViewModel partList,
            GarageEditorViewModel editor,
            GarageResultViewModel result,
            GarageEditorFocus focusedPart,
            bool isSaving);
    }
}
