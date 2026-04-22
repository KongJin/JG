using System.Collections.Generic;
using Features.Player.Application.Events;
using Features.Unit.Application;
using Features.Unit.Application.Events;
using Features.Unit.Application.Ports;
using Shared.EventBus;
using Shared.Kernel;
using Shared.Math;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnitSpec = Features.Unit.Domain.Unit;

namespace Features.Unit.Presentation
{
    public sealed class SummonCommandController : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private Camera _worldCamera;
        [SerializeField] private float _screenToPlaneY = 0f;

        [Header("Dock Layout")]
        [SerializeField] private RectTransform _dockRoot;
        [SerializeField] private RectTransform _slotRowRect;
        [SerializeField] private RectTransform _energyBarRect;
        [SerializeField] private RectTransform _feedbackRect;
        [SerializeField] private HorizontalLayoutGroup _slotRowLayout;
        [SerializeField] private Image _dockBackgroundImage;
        [SerializeField] private Color _dockBackgroundColor = new(0.03f, 0.07f, 0.11f, 0.94f);

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

            _eventBus.Subscribe(this, new System.Action<UnitSummonCompletedEvent>(OnSummonCompleted));
            _eventBus.Subscribe(this, new System.Action<UnitSummonFailedEvent>(OnSummonFailed));
            _eventBus.Subscribe(this, new System.Action<PlayerEnergyChangedEvent>(OnEnergyChanged));

            ClearSelectionVisuals();
        }

        public void SetSlotViews(IReadOnlyList<UnitSlotView> slotViews)
        {
            _slotViews = slotViews ?? System.Array.Empty<UnitSlotView>();
            RefreshSlotSelection();

            if (_slotRowRect != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(_slotRowRect);
            }
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

            _placementAreaView?.SetSelectionActive(true);
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
                _placementAreaView?.ShowInvalidPlacementFeedback();
                ShowError("배치 영역 밖");
                return false;
            }

            var summonPosition = _placementArea != null
                ? _placementArea.ClampToBounds(worldPosition)
                : worldPosition;

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
                if (TryHandleTouchInput())
                {
                    return;
                }

                if (TryHandleMouseInput())
                {
                    return;
                }
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

            _placementAreaView?.SetSelectionActive(false);
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

        private bool IsPointerOverUi(int pointerId = -1)
        {
            if (EventSystem.current == null)
                return false;

            return pointerId >= 0
                ? EventSystem.current.IsPointerOverGameObject(pointerId)
                : EventSystem.current.IsPointerOverGameObject();
        }

        private Vector3 ScreenToWorldPosition(Vector2 screenPosition)
        {
            var ray = _worldCamera.ScreenPointToRay(new Vector3(screenPosition.x, screenPosition.y, 0f));
            var plane = new Plane(Vector3.up, new Vector3(0f, _screenToPlaneY, 0f));
            return plane.Raycast(ray, out var enter) ? ray.GetPoint(enter) : Vector3.zero;
        }

        private bool TryHandleTouchInput()
        {
            var touchscreen = Touchscreen.current;
            if (touchscreen == null)
                return false;

            var touch = touchscreen.primaryTouch;
            if (!touch.press.wasPressedThisFrame)
                return false;

            var touchId = touch.touchId.ReadValue();
            if (IsPointerOverUi(touchId))
                return true;

            TryConfirmPlacementWorld(ScreenToWorldPosition(touch.position.ReadValue()));
            return true;
        }

        private bool TryHandleMouseInput()
        {
            var mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
                return false;

            if (IsPointerOverUi())
                return true;

            TryConfirmPlacementWorld(ScreenToWorldPosition(mouse.position.ReadValue()));
            return true;
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
            AutoBindLayoutReferences();

            if (_dockBackgroundImage != null)
            {
                _dockBackgroundImage.color = _dockBackgroundColor;
                _dockBackgroundImage.raycastTarget = false;
            }
        }

        private void AutoBindLayoutReferences()
        {
            if (_dockRoot == null)
            {
                _dockRoot = transform as RectTransform;
            }

            if (_dockBackgroundImage == null)
            {
                _dockBackgroundImage = GetComponent<Image>();
            }

            if (_slotRowRect == null)
            {
                var child = transform.Find("SlotRow");
                _slotRowRect = child as RectTransform;
            }

            if (_energyBarRect == null)
            {
                var child = transform.Find("EnergyBar");
                _energyBarRect = child as RectTransform;
            }

            if (_feedbackRect == null)
            {
                var child = transform.Find("PlacementErrorView");
                _feedbackRect = child as RectTransform;
            }

            if (_slotRowLayout == null && _slotRowRect != null)
            {
                _slotRowLayout = _slotRowRect.GetComponent<HorizontalLayoutGroup>();
            }

            if (_worldCamera == null)
            {
                _worldCamera = Camera.main;
            }
        }

        private void OnDestroy()
        {
            _eventBus?.UnsubscribeAll(this);
        }
    }
}
