using System.Collections.Generic;
using Features.Player.Application.Events;
using Features.Unit.Application;
using Features.Unit.Application.Events;
using Features.Unit.Application.Ports;
using Shared.EventBus;
using Shared.Kernel;
using Shared.Math;
using UnityEngine;
using UnitSpec = Features.Unit.Domain.Unit;

namespace Features.Unit.Presentation
{
    public sealed class SummonCommandController : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private Camera _worldCamera;
        [SerializeField] private float _screenToPlaneY = 0f;

        private IEventSubscriber _eventBus;
        private SummonUnitUseCase _summonUseCase;
        private PlacementArea _placementArea;
        private PlacementAreaView _placementAreaView;
        private PlacementErrorView _feedbackView;
        private IReadOnlyList<UnitSlotView> _slotViews = System.Array.Empty<UnitSlotView>();
        private int _selectionActivatedFrame = -1;
        private bool _isSelectionActive;
        private UnitSpec _selectedUnit;
        private DomainEntityId _selectionOwnerId;
        private int _selectedSlotIndex = -1;
        private PlacementCommandPreviewPresenter _previewPresenter;
        private SummonDockLayoutBinder _dockLayoutBinder;
        public string CurrentFeedbackMessage { get; private set; } = string.Empty;

        private void Awake()
        {
            ApplyDockLayout();
        }

        public void Initialize(
            IEventSubscriber eventBus,
            SummonUnitUseCase summonUseCase,
            DomainEntityId ownerId,
            PlacementArea placementArea,
            PlacementAreaView placementAreaView,
            PlacementErrorView feedbackView,
            Camera worldCamera)
        {
            _eventBus?.UnsubscribeAll(this);

            _eventBus = eventBus;
            _summonUseCase = summonUseCase;
            _placementArea = placementArea;
            _placementAreaView = placementAreaView;
            _feedbackView = feedbackView;
            _worldCamera = worldCamera;

            _selectionOwnerId = ownerId;
            ClearSelectionState();
            ApplyDockLayout();
            _previewPresenter = new PlacementCommandPreviewPresenter(
                _worldCamera,
                _screenToPlaneY,
                _placementArea,
                _placementAreaView);

            _eventBus.Subscribe(this, new System.Action<UnitSummonCompletedEvent>(OnSummonCompleted));
            _eventBus.Subscribe(this, new System.Action<UnitSummonFailedEvent>(OnSummonFailed));
            _eventBus.Subscribe(this, new System.Action<PlayerEnergyChangedEvent>(OnEnergyChanged));

            ClearSelectionVisuals();
        }

        public void SetSlotViews(IReadOnlyList<UnitSlotView> slotViews)
        {
            _slotViews = slotViews ?? System.Array.Empty<UnitSlotView>();
            RefreshSlotSelection();
            _dockLayoutBinder?.RebuildSlotRow();
        }

        public bool TrySelectSlot(UnitSlotView slotView)
        {
            if (slotView == null)
                return false;

            return TrySelectUnit(slotView.UnitSpec, slotView.SlotIndex, slotView.CanAfford);
        }

        public bool TrySelectUnit(UnitSpec unitSpec, int slotIndex, bool canAfford)
        {
            if (unitSpec == null)
                return false;

            if (_isSelectionActive && _selectedSlotIndex == slotIndex)
            {
                CancelSelection();
                return false;
            }

            if (!canAfford)
            {
                ShowError("Need Energy");
                return false;
            }

            ActivateSelection(unitSpec, slotIndex);
            _selectionActivatedFrame = Time.frameCount;
            CurrentFeedbackMessage = "배치 구역을 탭하세요";

            ShowPlacementPreview(ResolveDefaultPlacementPoint());
            _feedbackView?.ShowInfo(CurrentFeedbackMessage);
            RefreshSlotSelection();
            return true;
        }

        public bool TrySummonImmediate(UnitSpec unitSpec, int slotIndex, Float3 spawnPosition)
        {
            if (unitSpec == null)
                return false;

            var success = _summonUseCase.Execute(_selectionOwnerId, unitSpec, spawnPosition);
            if (success)
            {
                ClearSelectionVisuals();
            }
            else
            {
                ShowError("Need Energy");
            }

            return success;
        }

        public bool TryConfirmPlacementWorld(Vector3 worldPosition)
        {
            if (!_isSelectionActive || _selectedUnit == null)
                return false;

            if (_placementArea != null && !_placementArea.Contains(worldPosition))
            {
                ShowPlacementPreview(worldPosition);
                _placementAreaView?.ShowInvalidPlacementFeedback();
                ShowError("배치 영역 밖");
                return false;
            }

            var summonPosition = _placementArea != null
                ? _placementArea.ClampToBounds(worldPosition)
                : worldPosition;
            ShowPlacementPreview(summonPosition);

            var success = _summonUseCase.Execute(
                _selectionOwnerId,
                _selectedUnit,
                new Float3(summonPosition.x, summonPosition.y, summonPosition.z));

            if (success)
            {
                ClearSelectionVisuals();
            }

            return success;
        }

        // MCP smoke uses a direct controller hook because injected GameView clicks
        // do not reliably surface through UnityEngine.Input in the editor.
        public void ConfirmPlacementAtDefaultPoint()
        {
            if (!_isSelectionActive)
                return;

            TryConfirmPlacementWorld(ResolveDefaultPlacementPoint());
        }

        public void ConfirmPlacementAtPlacementCenter()
        {
            if (!_isSelectionActive)
                return;

            TryConfirmPlacementWorld(_placementArea != null ? _placementArea.Center : ResolveDefaultPlacementPoint());
        }

        public void CancelSelection()
        {
            ClearSelectionVisuals();
        }

        private void Update()
        {
            if (!_isSelectionActive || _worldCamera == null)
                return;

            if (Time.frameCount == _selectionActivatedFrame)
                return;

            try
            {
                _previewPresenter?.UpdateFromPointer(_selectedUnit);

                TryHandlePlacementInput();
            }
            catch (System.InvalidOperationException)
            {
                // Input API mismatch in some editor/runtime configurations should not spam the console
                // or break the command dock flow. MCP smoke uses custom placement hooks instead.
            }
        }

        private void OnSummonCompleted(UnitSummonCompletedEvent e)
        {
            if (!_selectionOwnerId.Equals(e.PlayerId))
                return;

            ClearSelectionVisuals();
        }

        private void OnSummonFailed(UnitSummonFailedEvent e)
        {
            if (!_selectionOwnerId.Equals(e.PlayerId))
                return;

            ShowError(TranslateFailureReason(e.Reason));
            _placementAreaView?.ShowInvalidPlacementFeedback();
        }

        private void OnEnergyChanged(PlayerEnergyChangedEvent e)
        {
            if (_isSelectionActive && !_selectionOwnerId.Equals(e.PlayerId))
                return;

            RefreshSlotSelection();
        }

        private void ClearSelectionVisuals()
        {
            ClearSelectionState();
            _selectionActivatedFrame = -1;
            CurrentFeedbackMessage = string.Empty;

            _placementAreaView?.HideUnitPreview();
            _feedbackView?.Hide();
            RefreshSlotSelection();
        }

        private void RefreshSlotSelection()
        {
            foreach (var slot in _slotViews)
            {
                if (slot == null)
                    continue;

                slot.SetSelected(_isSelectionActive && slot.SlotIndex == _selectedSlotIndex);
            }
        }

        private void ActivateSelection(UnitSpec unitSpec, int slotIndex)
        {
            _selectedUnit = unitSpec;
            _selectedSlotIndex = slotIndex;
            _isSelectionActive = unitSpec != null;
        }

        private void ClearSelectionState()
        {
            _selectedUnit = null;
            _selectedSlotIndex = -1;
            _isSelectionActive = false;
        }

        private void ShowError(string message)
        {
            CurrentFeedbackMessage = message;
            _feedbackView?.ShowError(message);
            RefreshSlotSelection();
        }

        private void ShowPlacementPreview(Vector3 worldPosition)
        {
            _previewPresenter?.Show(_selectedUnit, worldPosition);
        }

        private void TryHandlePlacementInput()
        {
            if (_previewPresenter == null ||
                !_previewPresenter.TryConsumePlacementPress(out var worldPosition, out var shouldConfirm))
            {
                return;
            }

            if (!shouldConfirm)
                return;

            TryConfirmPlacementWorld(worldPosition);
        }

        private static string TranslateFailureReason(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                return "배치 실패";

            if (reason.Contains("Not enough energy", System.StringComparison.OrdinalIgnoreCase))
                return "Need Energy";

            return "배치 실패";
        }

        private Vector3 ResolveDefaultPlacementPoint()
        {
            if (_placementArea != null)
            {
                var center = _placementArea.Center;
                var depthOffset = Mathf.Max(0.35f, _placementArea.Depth * 0.12f);
                return _placementArea.ClampToBounds(center + new Vector3(0f, 0f, -depthOffset));
            }

            return new Vector3(0f, _screenToPlaneY, 1.5f);
        }

        private void ApplyDockLayout()
        {
            _dockLayoutBinder ??= new SummonDockLayoutBinder(transform);
            _dockLayoutBinder.Apply(ref _worldCamera);
        }

        private void OnDestroy()
        {
            _eventBus?.UnsubscribeAll(this);
        }
    }
}
