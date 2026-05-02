using System.Collections;
using Features.Garage;
using Features.Garage.Domain;
using Features.Garage.Infrastructure;
using Features.Unit.Infrastructure;
using Shared.Runtime;
using UnityEngine;

namespace Features.Garage.Presentation
{
    public sealed class GarageSetBUitkPreviewSceneDriver : MonoBehaviour
    {
        [SerializeField] private GarageSetBUitkRuntimeAdapter _adapter;
        [SerializeField] private ModuleCatalog _moduleCatalog;
        [SerializeField] private NovaPartVisualCatalog _novaPartVisualCatalog;
        [SerializeField] private NovaPartAlignmentCatalog _novaPartAlignmentCatalog;

        private GaragePageState _state;
        private GaragePagePresenter _presenter;
        private GaragePanelCatalog _catalog;
        private Coroutine _renderCoroutine;
        private GarageEditorFocus _focusedPart = GarageEditorFocus.Mobility;
        private string _partSearchText = string.Empty;
        private bool _callbacksHooked;
        private const string PreferredPreviewMobilityId = "nova_mob_legs1_rdrn";


        private void OnEnable()
        {
            if (!UnityEngine.Application.isPlaying)
                RenderPreviewLoadout();
        }

        private void Start()
        {
            if (UnityEngine.Application.isPlaying)
                _renderCoroutine = StartCoroutine(RenderWhenReady());
        }

        private void OnDisable()
        {
            if (_renderCoroutine != null)
            {
                StopCoroutine(_renderCoroutine);
                _renderCoroutine = null;
            }
        }

        private IEnumerator RenderWhenReady()
        {
            const int maxAttempts = 30;
            for (int i = 0; i < maxAttempts; i++)
            {
                if (RenderPreviewLoadout())
                    yield break;

                yield return null;
            }
        }

        private bool RenderPreviewLoadout()
        {
            if (_adapter == null || _moduleCatalog == null)
                return false;

            if (!_adapter.Bind())
                return false;

            HookCallbacks();
            _catalog ??= new GaragePanelCatalogFactory().Build(
                _moduleCatalog,
                _novaPartVisualCatalog,
                _novaPartAlignmentCatalog);

            if (!TryPickFrame(_catalog, out var frame) ||
                !TryPickFirepower(_catalog, frame, out var firepower) ||
                !TryPickMobility(_catalog, out var mobility))
                return false;

            if (_state == null)
            {
                _state = new GaragePageState();
                var roster = new GarageRoster();
                roster.SetSlot(0, new GarageRoster.UnitLoadout(
                    frame.Id,
                    firepower.Id,
                    mobility.Id));
                _state.Initialize(roster);
            }

            _presenter ??= new GaragePagePresenter(_catalog);
            Render();
            return true;
        }

        private void HookCallbacks()
        {
            if (_callbacksHooked || _adapter == null)
                return;

            _callbacksHooked = true;
            _adapter.SlotSelected += SelectSlot;
            _adapter.PartFocusSelected += SetFocusedPart;
            _adapter.PartSearchChanged += SetPartSearchText;
            _adapter.PartOptionSelected += SelectPartOption;
        }

        private void SelectSlot(int slotIndex)
        {
            if (_state == null)
                return;

            _state.SelectSlot(slotIndex);
            _focusedPart = GarageEditorFocus.Mobility;
            _partSearchText = string.Empty;
            Render();
        }

        private void SetFocusedPart(GarageEditorFocus focus)
        {
            if (_focusedPart != focus)
                _partSearchText = string.Empty;

            _focusedPart = focus;
            Render();
        }

        private void SetPartSearchText(string value)
        {
            var next = value ?? string.Empty;
            if (_partSearchText == next)
                return;

            _partSearchText = next;
            Render();
        }

        private void SelectPartOption(GarageNovaPartSelection selection)
        {
            if (_state == null)
                return;

            _focusedPart = GarageNovaPartsPanelViewModelFactory.ToEditorFocus(selection.Slot);
            switch (selection.Slot)
            {
                case GarageNovaPartPanelSlot.Frame:
                    _state.SetEditingFrameId(selection.PartId);
                    _state.ClearIncompatibleFirepower(_catalog);
                    break;
                case GarageNovaPartPanelSlot.Firepower:
                    _state.SetEditingFirepowerId(selection.PartId);
                    break;
                case GarageNovaPartPanelSlot.Mobility:
                    _state.SetEditingMobilityId(selection.PartId);
                    break;
            }

            _state.ClearValidationOverride();
            Render();
        }

        public string PreviewSelectFocus(string focus)
        {
            if (!TryParseFocus(focus, out var parsedFocus))
                return "invalid-focus";

            EnsurePreviewReady();
            SetFocusedPart(parsedFocus);
            return BuildPreviewDebugState("focus");
        }

        public string PreviewSelectPart(string slot, string partId)
        {
            if (!TryParsePartSlot(slot, out var parsedSlot) || string.IsNullOrWhiteSpace(partId))
                return "invalid-part:slot=" + (slot ?? "") + ";part=" + (partId ?? "");

            EnsurePreviewReady();
            SelectPartOption(new GarageNovaPartSelection(parsedSlot, partId));
            return BuildPreviewDebugState("part");
        }

        public string PreviewSearchParts(string value)
        {
            EnsurePreviewReady();
            SetPartSearchText(value);
            return BuildPreviewDebugState("search");
        }

        private string BuildPreviewDebugState(string action)
        {
            return action +
                   ":frame=" + (_state?.EditingFrameId ?? "") +
                   ";fire=" + (_state?.EditingFirepowerId ?? "") +
                   ";mob=" + (_state?.EditingMobilityId ?? "") +
                   ";focus=" + _focusedPart +
                   ";search=" + _partSearchText;
        }

        private void Render()
        {
            if (_adapter == null || _state == null || _presenter == null || _catalog == null)
                return;

            _adapter.Render(
                _presenter.BuildSlotViewModels(_state),
                GarageNovaPartsEnergyDetails.Apply(
                    _catalog,
                    GarageNovaPartsPanelViewModelFactory.Build(
                        _catalog,
                        new GarageNovaPartsDraftSelection(
                            _state.EditingFrameId,
                            _state.EditingFirepowerId,
                            _state.EditingMobilityId),
                        _focusedPart,
                        _partSearchText)),
                _presenter.BuildEditorViewModel(_state),
                new GarageResultViewModel(
                    "UITK PREVIEW: 실제 Garage catalog 샘플",
                    "Preview scene driver에서 실제 ModuleCatalog 조합을 선택할 수 있습니다.",
                    "저장 동작은 preview scene에서 비활성입니다.",
                    isReady: false,
                    isDirty: false,
                    canSave: false,
                    primaryActionLabel: "Preview Only"),
                _focusedPart,
                isSaving: false);
        }

        private void EnsurePreviewReady()
        {
            if (_state == null || _presenter == null || _catalog == null)
                RenderPreviewLoadout();
        }

        private static bool TryParseFocus(string value, out GarageEditorFocus focus)
        {
            if (System.Enum.TryParse(value, ignoreCase: true, out focus))
                return true;

            focus = GarageEditorFocus.Mobility;
            return false;
        }

        private static bool TryParsePartSlot(string value, out GarageNovaPartPanelSlot slot)
        {
            if (System.Enum.TryParse(value, ignoreCase: true, out slot))
                return true;

            slot = GarageNovaPartPanelSlot.Mobility;
            return false;
        }

        private static bool TryPickFrame(GaragePanelCatalog catalog, out GaragePanelCatalog.FrameOption frame)
        {
            return SampleOptionPicker.TryPickFirst(
                catalog?.Frames,
                option => option.PreviewPrefab != null,
                out frame);
        }

        private static bool TryPickFirepower(
            GaragePanelCatalog catalog,
            GaragePanelCatalog.FrameOption frame,
            out GaragePanelCatalog.FirepowerOption firepower)
        {
            return SampleOptionPicker.TryPickFirst(
                catalog?.Firepower,
                option => option.PreviewPrefab != null &&
                          frame != null &&
                          UnitPartCompatibility.AreAssemblyFormsCompatible(frame.AssemblyForm, option.AssemblyForm),
                out firepower);
        }

        private static bool TryPickMobility(GaragePanelCatalog catalog, out GaragePanelCatalog.MobilityOption mobility)
        {
            return SampleOptionPicker.TryPickPreferredOrFirst(
                catalog?.Mobility,
                option => option.Id == PreferredPreviewMobilityId && option.PreviewPrefab != null,
                option => option.PreviewPrefab != null,
                out mobility);
        }
    }
}
