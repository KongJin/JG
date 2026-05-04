namespace Features.Garage.Presentation
{
    internal sealed class GarageEditorViewModelBuilder
    {
        private readonly GaragePanelCatalog _catalog;

        public GarageEditorViewModelBuilder(GaragePanelCatalog catalog)
        {
            _catalog = catalog;
        }

        public GarageEditorViewModel Build(GaragePageState state)
        {
            bool hasCommittedUnit = state.SelectedSlotHasCommittedLoadout();
            bool hasAnyDraftSelection = state.HasAnyDraftSelection();
            bool hasDraftChanges = state.SelectedSlotHasDraftChanges();
            string title;
            string subtitle;

            var frame = _catalog?.FindFrame(state.EditingFrameId);
            var firepower = _catalog?.FindFirepower(state.EditingFirepowerId);
            var mobility = _catalog?.FindMobility(state.EditingMobilityId);

            if (frame != null)
            {
                title = $"{GarageUnitIdentityFormatter.BuildCallsign(state.SelectedSlotIndex)} {frame.DisplayName}";
                subtitle = hasCommittedUnit && !hasDraftChanges
                    ? GarageUnitIdentityFormatter.BuildServiceTagText(GarageUnitServiceTag.Pending())
                    : hasCommittedUnit
                        ? "저장 가능 | 전적 유지"
                        : "프레임부터 조립 시작";
            }
            else if (!hasCommittedUnit && !hasAnyDraftSelection)
            {
                title = GarageUnitIdentityFormatter.BuildSlotLabel(state.SelectedSlotIndex, hasLoadout: false);
                subtitle = "프레임부터 조립 시작";
            }
            else if (hasCommittedUnit && !hasDraftChanges)
            {
                title = GarageUnitIdentityFormatter.BuildCallsign(state.SelectedSlotIndex);
                subtitle = GarageUnitIdentityFormatter.BuildServiceTagText(GarageUnitServiceTag.Pending());
            }
            else if (hasCommittedUnit)
            {
                title = GarageUnitIdentityFormatter.BuildCallsign(state.SelectedSlotIndex);
                subtitle = "저장 가능 | 전적 유지";
            }
            else
            {
                title = GarageUnitIdentityFormatter.BuildSlotLabel(state.SelectedSlotIndex, hasLoadout: false);
                subtitle = "세 파츠를 완성하면 저장 가능";
            }

            return new GarageEditorViewModel(
                title,
                subtitle,
                frame != null ? frame.DisplayName : "< 프레임 >",
                frame != null
                    ? $"EN {frame.EnergyCost}  |  HP {frame.BaseHp:0}  |  DEF {frame.Defense:0}"
                    : "차체를 선택하세요",
                firepower != null ? firepower.DisplayName : "< 무장 >",
                firepower != null
                    ? $"EN {firepower.EnergyCost}  |  ATK {firepower.AttackDamage:0}  |  RNG {firepower.Range:0.0}"
                    : "주 무장을 선택하세요",
                mobility != null ? mobility.DisplayName : "< 기동 >",
                mobility != null
                    ? $"EN {mobility.EnergyCost}  |  SPD {mobility.MoveSpeed:0.0}  |  MOV {mobility.MoveRange:0.0}"
                    : "기동 키트를 선택하세요",
                hasCommittedUnit || hasAnyDraftSelection);
        }
    }
}
