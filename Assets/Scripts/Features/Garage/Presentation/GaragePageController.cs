using System.Collections.Generic;
using Features.Garage.Domain;
using Shared.Attributes;
using Shared.Kernel;
using UnityEngine;
using UnityEngine.InputSystem;
using ComposedUnit = Features.Unit.Domain.Unit;

namespace Features.Garage.Presentation
{
    public sealed class GaragePageController : MonoBehaviour
    {
        [Header("Subviews")]
        [Required, SerializeField] private GarageRosterListView _rosterListView;
        [Required, SerializeField] private GarageUnitEditorView _unitEditorView;
        [Required, SerializeField] private GarageResultPanelView _resultPanelView;

        [Header("Preview")]
        [SerializeField] private GarageUnitPreviewView _unitPreviewView;

        [Header("Account (optional — 분리 가능)")]
        [SerializeField] private UnityEngine.Component _accountSettingsView;
        [SerializeField] private GameObject _accountPanelRoot;

        private GarageSetup _setup;
        private GaragePanelCatalog _catalog;
        private GaragePageState _state;
        private GaragePagePresenter _presenter;
        private bool _callbacksHooked;
        private bool _isInitialized;

        public void Initialize(GarageSetup setup, GaragePanelCatalog catalog)
        {
            _setup = setup;
            _catalog = catalog;
            _presenter = new GaragePagePresenter(_catalog);
            _state ??= new GaragePageState();

            // AccountSettingsView를 GaragePageRoot 바깥으로 분리 (레이아웃 겹침 방지)
            SeparateAccountPanel();

            HookCallbacks();

            if (_isInitialized)
            {
                Render();
                return;
            }

            _isInitialized = true;
            _state.Initialize(_setup.InitializeGarage.Execute() ?? new GarageRoster());

            _ = SaveRosterAsync(_state.CommittedRoster);

            Render();
        }

        /// <summary>
        /// AccountSettingsView를 GaragePageRoot에서 분리하여 우측 패널과 겹치지 않게 배치.
        /// </summary>
        private void SeparateAccountPanel()
        {
            if (_accountSettingsView == null) return;
            var accountTransform = _accountSettingsView.transform;

            if (_accountPanelRoot != null && accountTransform.parent == _accountPanelRoot.transform)
                return;

            if (_accountPanelRoot != null)
            {
                accountTransform.SetParent(_accountPanelRoot.transform, false);
                accountTransform.localPosition = Vector3.zero;
                accountTransform.localRotation = Quaternion.identity;
                accountTransform.localScale = Vector3.one;
            }
        }

        private async System.Threading.Tasks.Task SaveRosterAsync(GarageRoster roster)
        {
            var result = await _setup.SaveRoster.Execute(roster);
            if (!result.IsSuccess)
                _state.SetValidationOverride(result.Error);
            Render();
        }

        private void HookCallbacks()
        {
            if (_callbacksHooked)
                return;

            _callbacksHooked = true;

            _rosterListView.Bind();
            _unitEditorView.Bind();

            _rosterListView.SlotSelected += SelectSlot;
            _unitEditorView.FrameCycleRequested += CycleFrame;
            _unitEditorView.FirepowerCycleRequested += CycleFirepower;
            _unitEditorView.MobilityCycleRequested += CycleMobility;
            _unitEditorView.ClearRequested += ClearSelectedSlot;
            _resultPanelView.SaveClicked += OnSaveClicked;

            // 부품 비교 툴팁
            _unitEditorView.PartHoverRequested += ShowPartHoverTooltip;
        }

        /// <summary>
        /// 키보드 단축키 처리.
        /// A/D 또는 ←/→: 현재 편집 중인 파트 순환
        /// 1~6: 슬롯 전환
        /// Ctrl+S: 저장
        /// </summary>
        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            // Ctrl+S: 저장
            if (keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed)
            {
                if (keyboard.sKey.wasPressedThisFrame)
                {
                    OnSaveClicked();
                    return;
                }
            }

            // 1~6: 슬롯 전환
            if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame) { SelectSlot(0); return; }
            if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame) { SelectSlot(1); return; }
            if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame) { SelectSlot(2); return; }
            if (keyboard.digit4Key.wasPressedThisFrame || keyboard.numpad4Key.wasPressedThisFrame) { SelectSlot(3); return; }
            if (keyboard.digit5Key.wasPressedThisFrame || keyboard.numpad5Key.wasPressedThisFrame) { SelectSlot(4); return; }
            if (keyboard.digit6Key.wasPressedThisFrame || keyboard.numpad6Key.wasPressedThisFrame) { SelectSlot(5); return; }

            // A/D 또는 ←/→: 현재 선택된 파트 순환
            int delta = 0;
            if (keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame) delta = -1;
            else if (keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame) delta = 1;

            if (delta != 0)
            {
                // 현재 포커스된 파트를 결정 (편집 중인 파트이 있으면 해당 파트, 없으면 Frame 우선)
                if (!string.IsNullOrEmpty(_state.EditingFrameId)) { CycleFrame(delta); return; }
                if (!string.IsNullOrEmpty(_state.EditingFirepowerId)) { CycleFirepower(delta); return; }
                if (!string.IsNullOrEmpty(_state.EditingMobilityId)) { CycleMobility(delta); return; }
                // 아무것도 편집 중이 아니면 Frame부터 시작
                CycleFrame(delta);
            }
        }

        private void SelectSlot(int slotIndex)
        {
            _state.SelectSlot(slotIndex);
            Render();
        }

        private void CycleFrame(int delta)
        {
            _state.SetEditingFrameId(CycleId(_state.EditingFrameId, _catalog?.Frames, delta, frame => frame.Id));
            var frameName = _catalog?.FindFrame(_state.EditingFrameId)?.DisplayName ?? _state.EditingFrameId;
            _resultPanelView.ShowToast($"Frame → {frameName}");
            TryCommitEditingDraft();
            Render();
        }

        private void CycleFirepower(int delta)
        {
            _state.SetEditingFirepowerId(CycleId(_state.EditingFirepowerId, _catalog?.Firepower, delta, module => module.Id));
            var wpName = _catalog?.FindFirepower(_state.EditingFirepowerId)?.DisplayName ?? _state.EditingFirepowerId;
            _resultPanelView.ShowToast($"Firepower → {wpName}");
            TryCommitEditingDraft();
            Render();
        }

        private void CycleMobility(int delta)
        {
            _state.SetEditingMobilityId(CycleId(_state.EditingMobilityId, _catalog?.Mobility, delta, module => module.Id));
            var mobName = _catalog?.FindMobility(_state.EditingMobilityId)?.DisplayName ?? _state.EditingMobilityId;
            _resultPanelView.ShowToast($"Mobility → {mobName}");
            TryCommitEditingDraft();
            Render();
        }

        /// <summary>
        /// 부품 버튼 호버 시 다음 부품 이름 토스트로 표시 (비교 툴팁).
        /// </summary>
        private void ShowPartHoverTooltip(string partType, int delta)
        {
            string currentId = partType switch
            {
                "frame" => _state.EditingFrameId,
                "firepower" => _state.EditingFirepowerId,
                "mobility" => _state.EditingMobilityId,
                _ => null
            };

            string nextId = partType switch
            {
                "frame" => CycleId(currentId, _catalog?.Frames, delta, m => m.Id),
                "firepower" => CycleId(currentId, _catalog?.Firepower, delta, m => m.Id),
                "mobility" => CycleId(currentId, _catalog?.Mobility, delta, m => m.Id),
                _ => null
            };

            string currentName = partType switch
            {
                "frame" => _catalog?.FindFrame(currentId)?.DisplayName ?? "—",
                "firepower" => _catalog?.FindFirepower(currentId)?.DisplayName ?? "—",
                "mobility" => _catalog?.FindMobility(currentId)?.DisplayName ?? "—",
                _ => "—"
            };

            string nextName = partType switch
            {
                "frame" => _catalog?.FindFrame(nextId)?.DisplayName ?? "—",
                "firepower" => _catalog?.FindFirepower(nextId)?.DisplayName ?? "—",
                "mobility" => _catalog?.FindMobility(nextId)?.DisplayName ?? "—",
                _ => "—"
            };

            _resultPanelView.ShowToast($"{currentName} → {nextName}");
        }

        private async void ClearSelectedSlot()
        {
            var updatedRoster = _state.CommittedRoster.Clone();
            updatedRoster.ClearSlot(_state.SelectedSlotIndex);

            _resultPanelView.ShowLoading(true);
            var result = await _setup.SaveRoster.Execute(updatedRoster);
            _resultPanelView.ShowLoading(false);

            if (!result.IsSuccess)
            {
                _state.SetValidationOverride(result.Error);
                Render();
                return;
            }

            _state.SetCommittedRoster(updatedRoster);
            _state.ClearSelectedSlotDraft();

            Render();
        }

        private async void OnSaveClicked()
        {
            // 저장 중 버튼 비활성화 + 로딩 표시 — 중복 클릭 방지
            _resultPanelView.ShowLoading(true);

            var result = await _setup.SaveRoster.Execute(_state.CommittedRoster);

            _resultPanelView.ShowLoading(false);

            if (result.IsSuccess)
            {
                _resultPanelView.ShowToast("Roster saved!");
            }
            else
            {
                _resultPanelView.ShowToast(result.Error, isError: true);
            }
        }

        private async void TryCommitEditingDraft()
        {
            _state.ClearValidationOverride();

            var evaluation = EvaluateDraft();
            if (!evaluation.HasCatalogData || !evaluation.HasCompleteDraft)
                return;

            if (!evaluation.HasComposedUnit)
            {
                _state.SetValidationOverride(evaluation.ComposeResult.Error);
                return;
            }

            var updatedRoster = _state.CommittedRoster.Clone();
            updatedRoster.SetSlot(_state.SelectedSlotIndex, new GarageRoster.UnitLoadout(
                _state.EditingFrameId,
                _state.EditingFirepowerId,
                _state.EditingMobilityId));

            var result = await _setup.SaveRoster.Execute(updatedRoster);
            if (!result.IsSuccess)
            {
                _state.SetValidationOverride(result.Error);
                return;
            }

            _state.SetCommittedRoster(updatedRoster);
        }

        private void Render()
        {
            var evaluation = EvaluateDraft();
            var slotViewModels = _presenter.BuildSlotViewModels(_state);

            _rosterListView.Render(slotViewModels);
            _unitEditorView.Render(_presenter.BuildEditorViewModel(_state));
            _resultPanelView.Render(_presenter.BuildResultViewModel(_state, evaluation));

            if (_unitPreviewView != null && _catalog != null)
            {
                var selectedSlot = slotViewModels[_state.SelectedSlotIndex];
                _unitPreviewView.Render(selectedSlot, _catalog);
            }
        }

        private GarageDraftEvaluation EvaluateDraft()
        {
            bool hasCatalogData = _catalog != null &&
                                  _catalog.Frames.Count > 0 &&
                                  _catalog.Firepower.Count > 0 &&
                                  _catalog.Mobility.Count > 0;

            Result<ComposedUnit> composeResult = Result<ComposedUnit>.Failure("Draft composition was not evaluated.");
            if (hasCatalogData && _state.HasCompleteDraft())
            {
                composeResult = _setup.ComposeUnit.Execute(
                    DomainEntityId.New(),
                    _state.EditingFrameId,
                    _state.EditingFirepowerId,
                    _state.EditingMobilityId);
            }

            return GarageDraftEvaluation.Create(_state, hasCatalogData, composeResult);
        }

        private static string CycleId<T>(
            string currentId,
            IReadOnlyList<T> items,
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
