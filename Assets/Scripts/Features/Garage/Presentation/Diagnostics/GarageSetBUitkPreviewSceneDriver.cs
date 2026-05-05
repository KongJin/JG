using System.Collections;
using Features.Garage;
using Features.Garage.Domain;
using Features.Garage.Infrastructure;
using Features.Unit.Domain;
using Features.Unit.Infrastructure;
using Shared.Attributes;
using Shared.Runtime;
using UnityEngine;

namespace Features.Garage.Presentation
{
    /// <summary>
    /// 프리뷰 씬에서 Garage UI를 테스트하기 위한 드라이버입니다.
    /// NOTE: 디버깅/프리뷰 전용으로 Infrastructure 계층에 직접 의존합니다.
    /// 프로덕션 코드에서는 이 패턴을 따르지 마세요.
    /// </summary>
    public sealed class GarageSetBUitkPreviewSceneDriver : MonoBehaviour
    {
        [Required, SerializeField] private GarageSetBUitkRuntimeAdapter _adapter;
        [Required, SerializeField] private ModuleCatalog _moduleCatalog;
        [Required, SerializeField] private NovaPartVisualCatalog _novaPartVisualCatalog;
        [Required, SerializeField] private NovaPartAlignmentCatalog _novaPartAlignmentCatalog;

        private GaragePageState _state;
        private GaragePageViewModelBuilders _viewModelBuilders;
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
            // csharp-guardrails: allow-null-defense
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
// csharp-guardrails: allow-null-defense
            if (_adapter == null || _moduleCatalog == null)
                return false;

            if (!_adapter.Bind())
                return false;

            HookCallbacks();
// csharp-guardrails: allow-null-defense
            _catalog ??= new GaragePanelCatalogFactory().Build(
                _moduleCatalog,
                _novaPartVisualCatalog,
                _novaPartAlignmentCatalog);

            if (!TryPickFrame(_catalog, out var frame) ||
                !TryPickFirepower(_catalog, frame, out var firepower) ||
                !TryPickMobility(_catalog, out var mobility))
                return false;

// csharp-guardrails: allow-null-defense
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

// csharp-guardrails: allow-null-defense
            _viewModelBuilders ??= new GaragePageViewModelBuilders(_catalog);
            Render();
            return true;
        }

        private void HookCallbacks()
        {
// csharp-guardrails: allow-null-defense
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
// csharp-guardrails: allow-null-defense
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
// csharp-guardrails: allow-null-defense
            if (_state == null)
                return;

            _focusedPart = GarageEditorFocusMapping.ToEditorFocus(selection.Slot);
            switch (selection.Slot)
            {
                case GarageNovaPartPanelSlot.Frame:
                    _state.SetEditingFrameId(selection.PartId);
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
// csharp-guardrails: allow-null-defense
                   ":frame=" + (_state?.EditingFrameId ?? "") +
// csharp-guardrails: allow-null-defense
                   ";fire=" + (_state?.EditingFirepowerId ?? "") +
// csharp-guardrails: allow-null-defense
                   ";mob=" + (_state?.EditingMobilityId ?? "") +
                   ";focus=" + _focusedPart +
                   ";search=" + _partSearchText;
        }

        private void Render()
        {
// csharp-guardrails: allow-null-defense
            if (_adapter == null || _state == null || _viewModelBuilders == null || _catalog == null)
                return;

            _adapter.Render(
                _viewModelBuilders.BuildSlotViewModels(_state),
                GarageNovaPartsPanelViewModelFactory.Build(
                    _catalog,
                    new GarageNovaPartsDraftSelection(
                        _state.EditingFrameId,
                        _state.EditingFirepowerId,
                        _state.EditingMobilityId),
                    _focusedPart,
                    _partSearchText),
                _viewModelBuilders.BuildEditorViewModel(_state),
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
// csharp-guardrails: allow-null-defense
            if (_state == null || _viewModelBuilders == null || _catalog == null)
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
                // csharp-guardrails: allow-null-defense
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
                // csharp-guardrails: allow-null-defense
                option => option.PreviewPrefab != null &&
                          frame != null &&
                          UnitPartCompatibility.AreAssemblyFormsCompatible(frame.AssemblyForm, option.AssemblyForm),
                out firepower);
        }

        private static bool TryPickMobility(GaragePanelCatalog catalog, out GaragePanelCatalog.MobilityOption mobility)
        {
            return SampleOptionPicker.TryPickPreferredOrFirst(
                catalog?.Mobility,
                // csharp-guardrails: allow-null-defense
                option => option.Id == PreferredPreviewMobilityId && option.PreviewPrefab != null,
                // csharp-guardrails: allow-null-defense
                option => option.PreviewPrefab != null,
                out mobility);
        }
    }
}
