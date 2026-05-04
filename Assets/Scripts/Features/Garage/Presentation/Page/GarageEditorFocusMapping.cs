namespace Features.Garage.Presentation
{
    /// <summary>
    /// 에디터 포커스(enum)와 Nova 파츠 패널 슬롯 간 변환. Page·Uitk·Diagnostics가 동일 규칙을 쓴다.
    /// </summary>
    public static class GarageEditorFocusMapping
    {
        public static GarageNovaPartPanelSlot ToPanelSlot(GarageEditorFocus focus)
        {
            return focus switch
            {
                GarageEditorFocus.Firepower => GarageNovaPartPanelSlot.Firepower,
                GarageEditorFocus.Mobility => GarageNovaPartPanelSlot.Mobility,
                _ => GarageNovaPartPanelSlot.Frame,
            };
        }

        public static GarageEditorFocus ToEditorFocus(GarageNovaPartPanelSlot slot)
        {
            return slot switch
            {
                GarageNovaPartPanelSlot.Firepower => GarageEditorFocus.Firepower,
                GarageNovaPartPanelSlot.Mobility => GarageEditorFocus.Mobility,
                _ => GarageEditorFocus.Frame,
            };
        }
    }
}
