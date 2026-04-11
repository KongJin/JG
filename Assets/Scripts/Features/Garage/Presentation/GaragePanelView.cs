using Features.Garage.Application;
using Features.Garage.Domain;
using Shared.Attributes;
using Shared.EventBus;
using Shared.Kernel;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Features.Garage.Presentation
{
    public sealed class GaragePanelView : MonoBehaviour
    {
        [Header("Slots")]
        [Required, SerializeField] private GarageSlotItemView[] _slotViews;

        [Header("Frame Selector")]
        [Required, SerializeField] private Button _framePrevButton;
        [Required, SerializeField] private Button _frameNextButton;
        [Required, SerializeField] private TMP_Text _frameValueText;
        [SerializeField] private TMP_Text _frameHintText;

        [Header("Firepower Selector")]
        [Required, SerializeField] private Button _firepowerPrevButton;
        [Required, SerializeField] private Button _firepowerNextButton;
        [Required, SerializeField] private TMP_Text _firepowerValueText;
        [SerializeField] private TMP_Text _firepowerHintText;

        [Header("Mobility Selector")]
        [Required, SerializeField] private Button _mobilityPrevButton;
        [Required, SerializeField] private Button _mobilityNextButton;
        [Required, SerializeField] private TMP_Text _mobilityValueText;
        [SerializeField] private TMP_Text _mobilityHintText;

        [Header("Summary")]
        [SerializeField] private TMP_Text _selectionTitleText;
        [SerializeField] private TMP_Text _selectionSubtitleText;
        [SerializeField] private TMP_Text _rosterStatusText;
        [SerializeField] private TMP_Text _validationText;
        [SerializeField] private TMP_Text _statsText;
        [Required, SerializeField] private Button _clearButton;

        private GarageSetup _setup;
        private GaragePanelCatalog _catalog;
        private GarageRoster _roster;
        private int _selectedSlotIndex;
        private string _editingFrameId;
        private string _editingFirepowerId;
        private string _editingMobilityId;
        private bool _callbacksHooked;

        public void Initialize(IEventPublisher eventPublisher, GarageSetup setup, GaragePanelCatalog catalog)
        {
            _setup = setup;
            _catalog = catalog;

            HookCallbacks();

            _roster = _setup.InitializeGarage.Execute() ?? new GarageRoster();
            _roster.Normalize();
            SaveRosterState();

            SelectSlot(0);
        }

        private void HookCallbacks()
        {
            if (_callbacksHooked)
                return;

            _callbacksHooked = true;

            for (int i = 0; i < _slotViews.Length; i++)
            {
                int slotIndex = i;
                _slotViews[i].Button.onClick.AddListener(() => SelectSlot(slotIndex));
            }

            _framePrevButton.onClick.AddListener(() => CycleFrame(-1));
            _frameNextButton.onClick.AddListener(() => CycleFrame(1));
            _firepowerPrevButton.onClick.AddListener(() => CycleFirepower(-1));
            _firepowerNextButton.onClick.AddListener(() => CycleFirepower(1));
            _mobilityPrevButton.onClick.AddListener(() => CycleMobility(-1));
            _mobilityNextButton.onClick.AddListener(() => CycleMobility(1));
            _clearButton.onClick.AddListener(ClearSelectedSlot);
        }

        private void SelectSlot(int slotIndex)
        {
            _selectedSlotIndex = Mathf.Clamp(slotIndex, 0, GarageRoster.MaxSlots - 1);

            var slot = _roster.GetSlot(_selectedSlotIndex);
            _editingFrameId = slot.frameId;
            _editingFirepowerId = slot.firepowerModuleId;
            _editingMobilityId = slot.mobilityModuleId;

            RenderValidationMessage();
            Render();
        }

        private void CycleFrame(int delta)
        {
            _editingFrameId = CycleId(_editingFrameId, _catalog?.Frames, delta, frame => frame.Id);
            TryCommitEditingSelection();
            Render();
        }

        private void CycleFirepower(int delta)
        {
            _editingFirepowerId = CycleId(_editingFirepowerId, _catalog?.Firepower, delta, module => module.Id);
            TryCommitEditingSelection();
            Render();
        }

        private void CycleMobility(int delta)
        {
            _editingMobilityId = CycleId(_editingMobilityId, _catalog?.Mobility, delta, module => module.Id);
            TryCommitEditingSelection();
            Render();
        }

        private void ClearSelectedSlot()
        {
            _roster.ClearSlot(_selectedSlotIndex);
            _editingFrameId = null;
            _editingFirepowerId = null;
            _editingMobilityId = null;

            SaveRosterState();
            RenderValidationMessage();
            Render();
        }

        private void TryCommitEditingSelection()
        {
            if (!HasCatalogData())
                return;

            if (!IsEditingSelectionComplete())
            {
                if (_validationText != null)
                    _validationText.text = "Select frame, firepower, and mobility to save this slot.";
                return;
            }

            var candidate = new GarageRoster.UnitLoadout(_editingFrameId, _editingFirepowerId, _editingMobilityId);
            var composeResult = _setup.ComposeUnit.Execute(
                DomainEntityId.New(),
                candidate.frameId,
                candidate.firepowerModuleId,
                candidate.mobilityModuleId);

            if (!composeResult.IsSuccess)
            {
                if (_validationText != null)
                    _validationText.text = composeResult.Error;
                return;
            }

            var updatedRoster = _roster.Clone();
            updatedRoster.SetSlot(_selectedSlotIndex, candidate);

            if (!_setup.SaveRoster.Execute(updatedRoster, out var errorMessage).IsSuccess)
            {
                if (_validationText != null)
                    _validationText.text = errorMessage;
                return;
            }

            _roster = updatedRoster;
            RenderValidationMessage(savedNow: true);
        }

        private void SaveRosterState()
        {
            _roster.Normalize();
            _setup.SaveRoster.Execute(_roster, out _);
        }

        private void Render()
        {
            RenderSlots();
            RenderSelectorTexts();
            RenderSelectionSummary();
            RenderRosterStatus();
            RenderStats();
        }

        private void RenderSlots()
        {
            if (_slotViews == null)
                return;

            for (int i = 0; i < _slotViews.Length; i++)
                _slotViews[i].Render(i, _roster.GetSlot(i), _catalog, i == _selectedSlotIndex);
        }

        private void RenderSelectorTexts()
        {
            RenderFrameSelector();
            RenderFirepowerSelector();
            RenderMobilitySelector();
        }

        private void RenderFrameSelector()
        {
            var frame = _catalog?.FindFrame(_editingFrameId);
            if (_frameValueText != null)
                _frameValueText.text = frame != null ? frame.DisplayName : "< Select Frame >";
            if (_frameHintText != null)
                _frameHintText.text = frame != null
                    ? $"Base HP {frame.BaseHp:0}  |  Base ASPD {frame.BaseAttackSpeed:0.00}"
                    : "Choose the chassis that defines the unit's passive and base profile.";
        }

        private void RenderFirepowerSelector()
        {
            var module = _catalog?.FindFirepower(_editingFirepowerId);
            if (_firepowerValueText != null)
                _firepowerValueText.text = module != null ? module.DisplayName : "< Select Firepower >";
            if (_firepowerHintText != null)
                _firepowerHintText.text = module != null
                    ? $"DMG {module.AttackDamage:0}  |  ASPD {module.AttackSpeed:0.00}  |  Range {module.Range:0.0}"
                    : "Choose the weapon module that sets range and damage tempo.";
        }

        private void RenderMobilitySelector()
        {
            var module = _catalog?.FindMobility(_editingMobilityId);
            if (_mobilityValueText != null)
                _mobilityValueText.text = module != null ? module.DisplayName : "< Select Mobility >";
            if (_mobilityHintText != null)
                _mobilityHintText.text = module != null
                    ? $"HP +{module.HpBonus:0}  |  Move {module.MoveRange:0.0}  |  Anchor {module.AnchorRange:0.0}"
                    : "Choose the mobility frame for survivability and anchor range.";
        }

        private void RenderSelectionSummary()
        {
            var slot = _roster.GetSlot(_selectedSlotIndex);
            bool hasCommittedUnit = slot.IsComplete;

            if (_selectionTitleText != null)
                _selectionTitleText.text = hasCommittedUnit ? $"Slot {_selectedSlotIndex + 1} Loadout" : $"Slot {_selectedSlotIndex + 1} Empty";

            if (_selectionSubtitleText != null)
            {
                _selectionSubtitleText.text = hasCommittedUnit
                    ? "Saved loadout. Adjust selectors to overwrite this slot automatically."
                    : "Build a loadout. Valid combinations save immediately.";
            }
        }

        private void RenderRosterStatus()
        {
            if (_rosterStatusText == null)
                return;

            int missingUnits = Mathf.Max(0, 3 - _roster.Count);
            _rosterStatusText.text = _roster.IsValid
                ? $"Roster ready: {_roster.Count}/6 saved units. Lobby Ready can stay enabled."
                : $"Roster incomplete: {_roster.Count}/6 saved units. Add {missingUnits} more for Ready.";
        }

        private void RenderValidationMessage(bool savedNow = false)
        {
            if (_validationText == null)
                return;

            if (savedNow)
            {
                _validationText.text = _roster.IsValid
                    ? "Saved. Valid roster is synced to local storage and Photon."
                    : "Saved. Slot committed, but Ready still needs at least 3 saved units.";
                return;
            }

            if (!IsEditingSelectionComplete())
            {
                _validationText.text = "Select frame, firepower, and mobility to save this slot.";
                return;
            }

            _validationText.text = _roster.IsValid
                ? "Roster is valid."
                : "Roster is saved, but not yet eligible for Ready.";
        }

        private void RenderStats()
        {
            if (_statsText == null)
                return;

            if (!IsEditingSelectionComplete())
            {
                _statsText.text = "Pick all three parts to see composed HP, damage, role, and summon cost.";
                return;
            }

            var result = _setup.ComposeUnit.Execute(
                DomainEntityId.New(),
                _editingFrameId,
                _editingFirepowerId,
                _editingMobilityId);

            if (!result.IsSuccess)
            {
                _statsText.text = result.Error;
                return;
            }

            var unit = result.Value;
            _statsText.text =
                $"Cost {unit.SummonCost}  |  Trait Bonus {unit.PassiveTraitCostBonus}\n" +
                $"HP {unit.FinalHp:0}  |  DMG {unit.FinalAttackDamage:0}  |  ASPD {unit.FinalAttackSpeed:0.00}\n" +
                $"Range {unit.FinalRange:0.0}  |  Move {unit.FinalMoveRange:0.0}  |  Anchor {unit.FinalAnchorRange:0.0}";
        }

        private bool HasCatalogData()
        {
            return _catalog != null &&
                   _catalog.Frames.Count > 0 &&
                   _catalog.Firepower.Count > 0 &&
                   _catalog.Mobility.Count > 0;
        }

        private bool IsEditingSelectionComplete()
        {
            return !string.IsNullOrWhiteSpace(_editingFrameId) &&
                   !string.IsNullOrWhiteSpace(_editingFirepowerId) &&
                   !string.IsNullOrWhiteSpace(_editingMobilityId);
        }

        private static string CycleId<T>(
            string currentId,
            System.Collections.Generic.IReadOnlyList<T> items,
            int delta,
            System.Func<T, string> getId)
        {
            if (items == null || items.Count == 0)
                return null;

            int currentIndex = -1;
            for (int i = 0; i < items.Count; i++)
            {
                if (getId(items[i]) == currentId)
                {
                    currentIndex = i;
                    break;
                }
            }

            int nextIndex = currentIndex + delta;
            if (nextIndex < 0)
                nextIndex = items.Count - 1;
            if (nextIndex >= items.Count)
                nextIndex = 0;

            return getId(items[nextIndex]);
        }
    }
}
