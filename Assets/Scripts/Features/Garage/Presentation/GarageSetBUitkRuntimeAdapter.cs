using System;
using System.Collections.Generic;
using Shared.Runtime;
using Shared.Runtime.Pooling;
using Shared.Ui;
using UnityEngine;
using UnityEngine.UIElements;

namespace Features.Garage.Presentation
{
    public sealed class GarageSetBUitkRuntimeAdapter : MonoBehaviour
    {
        [SerializeField]
        private UIDocument _document;

        [SerializeField]
        private GarageSetBUitkPreviewRenderer _previewRenderer;

        [SerializeField]
        private GarageSetBUitkPreviewRenderer _partPreviewRenderer;

        private VisualElement _surfaceRoot;
        private VisualElement _hostScreenRoot;
        private Label _commandStatusLabel;
        private Button _settingsButton;
        private GarageSetBSlotSurface _slotSurface;
        private GarageSetBPartListSurface _partListSurface;
        private Label _focusedPartBadgeLabel;
        private Label _focusedPartTitleLabel;
        private Label _focusedPartDescriptionLabel;
        private VisualElement _focusedPartIconGlyph;
        private Label _previewTitleLabel;
        private VisualElement _unitPreviewHost;
        private Label _unitPreviewLabel;
        private Image _unitPreviewImage;
        private Button _saveButton;
        private bool _isHostBound;
        private IReadOnlyList<GarageSlotViewModel> _lastSlots;
        private GarageNovaPartsPanelViewModel _lastPartList;
        private GarageEditorViewModel _lastEditor;
        private GarageResultViewModel _lastResult;
        private GarageEditorFocus _lastFocusedPart;
        private bool _lastIsSaving;
        private bool _hasLastRender;

        public event Action<int> SlotSelected;
        public event Action<GarageEditorFocus> PartFocusSelected;
        public event Action<string> PartSearchChanged;
        public event Action<GarageNovaPartSelection> PartOptionSelected;
        public event Action SaveRequested;
        public event Action SettingsRequested;

        public bool Bind()
        {
            if (_surfaceRoot != null && _slotSurface != null && _partListSurface != null)
                return true;

            if (_document == null)
                return false;

            var root = _document.rootVisualElement;
            if (root == null)
                return false;

            _isHostBound = false;
            _hostScreenRoot = null;
            return Bind(root);
        }

        public bool BindToHost(VisualElement host)
        {
            if (host == null)
                return false;

            if (host.Q<VisualElement>("GarageSetBScreen") == null)
            {
                var source = _document != null ? _document.visualTreeAsset : null;
                if (source == null)
                    return false;

                host.Clear();
                source.CloneTree(host);
            }

            var screenRoot = host.Q<VisualElement>("GarageSetBScreen");
            if (screenRoot == null)
                return false;

            _isHostBound = true;
            _hostScreenRoot = screenRoot;
            _hostScreenRoot.style.display = DisplayStyle.Flex;
            HideStandaloneDocumentRoot();
            return Bind(host);
        }

        public bool SetDocumentRootVisible(bool isVisible)
        {
            if (_isHostBound)
            {
                HideStandaloneDocumentRoot();
                if (_hostScreenRoot != null)
                    _hostScreenRoot.style.display = DisplayStyle.Flex;
                return true;
            }

            if (_document == null)
                return false;

            if (!_document.gameObject.activeSelf)
                _document.gameObject.SetActive(true);

            _document.sortingOrder = 10;
            var root = _document.rootVisualElement;
            if (root != null)
                root.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;

            return true;
        }

        private void HideStandaloneDocumentRoot()
        {
            if (_document == null)
                return;

            if (!_document.gameObject.activeSelf)
                _document.gameObject.SetActive(true);

            _document.sortingOrder = 10;
            var root = _document.rootVisualElement;
            if (root != null)
                root.style.display = DisplayStyle.None;
        }

        private bool Bind(VisualElement root)
        {
            if (root == null)
                return false;

            if (_surfaceRoot == root && _slotSurface != null && _partListSurface != null)
                return true;

            _commandStatusLabel = GarageSetBUitkElements.Required<Label>(root, "CommandStatusLabel");
            _settingsButton = GarageSetBUitkElements.Required<Button>(root, "SettingsButton");
            _slotSurface = new GarageSetBSlotSurface(root);
            _partListSurface = new GarageSetBPartListSurface(root);
            _focusedPartBadgeLabel = GarageSetBUitkElements.Required<Label>(root, "FocusedPartBadgeLabel");
            _focusedPartTitleLabel = GarageSetBUitkElements.Required<Label>(root, "FocusedPartTitleLabel");
            _focusedPartDescriptionLabel = GarageSetBUitkElements.Required<Label>(root, "FocusedPartDescriptionLabel");
            _focusedPartIconGlyph = GarageSetBUitkElements.Required<VisualElement>(root, "FocusedPartIconGlyph");
            _previewTitleLabel = GarageSetBUitkElements.Required<Label>(root, "PreviewTitleLabel");
            _unitPreviewHost = GarageSetBUitkElements.Required<VisualElement>(root, "UnitPreviewHost");
            _unitPreviewLabel = GarageSetBUitkElements.Required<Label>(root, "UnitPreviewLabel");
            _unitPreviewImage = GarageSetBUitkElements.CreatePreviewImage();
            _unitPreviewHost.Insert(0, _unitPreviewImage);
            _saveButton = GarageSetBUitkElements.Required<Button>(root, "SaveButton");
            _surfaceRoot = root;

            BindCallbacks();
            SetPreviewTexture(null, false);
            SetPartPreviewTexture(null, false);

            if (_hasLastRender)
                RenderToSurface();

            return true;
        }

        public void Render(
            IReadOnlyList<GarageSlotViewModel> slots,
            GarageNovaPartsPanelViewModel partList,
            GarageEditorViewModel editor,
            GarageResultViewModel result,
            GarageEditorFocus focusedPart,
            bool isSaving)
        {
            _lastSlots = slots;
            _lastPartList = partList;
            _lastEditor = editor;
            _lastResult = result;
            _lastFocusedPart = focusedPart;
            _lastIsSaving = isSaving;
            _hasLastRender = true;

            if (!Bind())
                return;

            RenderToSurface();
        }

        private void OnEnable()
        {
            Bind();
        }

        private void Reset()
        {
            if (_document == null)
                _document = ComponentAccess.Get<UIDocument>(gameObject);
        }

        private void RenderPreview(IReadOnlyList<GarageSlotViewModel> slots)
        {
            var selectedSlot = FindSelectedSlot(slots);
            if (_previewRenderer == null)
            {
                SetPreviewTexture(null, false);
            }
            else
            {
                bool hasPreview = _previewRenderer.Render(selectedSlot);
                SetPreviewTexture(_previewRenderer.PreviewTexture, hasPreview);
            }

            RenderPartPreview(selectedSlot);
        }

        private void RenderPartPreview(GarageSlotViewModel selectedSlot)
        {
            if (_partPreviewRenderer == null)
            {
                SetPartPreviewTexture(null, false);
                return;
            }

            bool hasPreview =
                selectedSlot != null && _partPreviewRenderer.Render(selectedSlot) ||
                _partPreviewRenderer.RenderPart(_lastPartList);
            SetPartPreviewTexture(_partPreviewRenderer.PreviewTexture, hasPreview);
        }

        private static GarageSlotViewModel FindSelectedSlot(IReadOnlyList<GarageSlotViewModel> slots)
        {
            if (slots == null)
                return null;

            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].IsSelected)
                    return slots[i];
            }

            return slots.Count > 0 ? slots[0] : null;
        }

        private void RenderToSurface()
        {
            if (_slotSurface == null || _partListSurface == null)
                return;

            _slotSurface.Render(_lastSlots);
            _partListSurface.Render(_lastPartList, _lastFocusedPart);
            RenderFocusedPart(_lastEditor, _lastFocusedPart);
            RenderResult(_lastResult, _lastIsSaving);
            RenderPreview(_lastSlots);
        }

        private void BindCallbacks()
        {
            _slotSurface.SlotSelected += slotIndex => SlotSelected?.Invoke(slotIndex);
            _partListSurface.FocusSelected += focus => PartFocusSelected?.Invoke(focus);
            _partListSurface.SearchChanged += value => PartSearchChanged?.Invoke(value);
            _partListSurface.OptionSelected += selection => PartOptionSelected?.Invoke(selection);
            _saveButton.clicked += () => SaveRequested?.Invoke();
            _settingsButton.clicked += () => SettingsRequested?.Invoke();
        }

        private void SetPreviewTexture(Texture texture, bool isVisible)
        {
            if (_unitPreviewImage == null || _unitPreviewLabel == null)
                return;

            _unitPreviewImage.image = isVisible ? texture : null;
            _unitPreviewImage.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
            _unitPreviewLabel.style.display = isVisible ? DisplayStyle.None : DisplayStyle.Flex;
            _previewTitleLabel.text = isVisible ? "UNIT PREVIEW" : "BLUEPRINT VIEW";
        }

        private void SetPartPreviewTexture(Texture texture, bool isVisible)
        {
            _partListSurface?.SetPreviewTexture(texture, isVisible);
        }

        private void RenderFocusedPart(GarageEditorViewModel editor, GarageEditorFocus focusedPart)
        {
            var part = FocusedPartText.From(editor, focusedPart);
            _focusedPartBadgeLabel.text = part.Badge;
            _focusedPartTitleLabel.text = part.Title;
            _focusedPartDescriptionLabel.text = part.Description;
            UitkIconRegistry.Apply(_focusedPartIconGlyph, part.IconId);
        }

        private void RenderResult(GarageResultViewModel result, bool isSaving)
        {
            _commandStatusLabel.text = result?.RosterStatusText ?? "COMMAND_STATUS: 대기";
            _saveButton.text = isSaving ? "저장 중..." : result?.PrimaryActionLabel ?? "저장 및 배치";
            _saveButton.SetEnabled(!isSaving && result?.CanSave == true);
        }

        private readonly struct FocusedPartText
        {
            public FocusedPartText(string badge, string title, string description, string iconId)
            {
                Badge = badge;
                Title = title;
                Description = description;
                IconId = iconId;
            }

            public string Badge { get; }
            public string Title { get; }
            public string Description { get; }
            public string IconId { get; }

            public static FocusedPartText From(GarageEditorViewModel editor, GarageEditorFocus focusedPart)
            {
                if (editor == null)
                    return new FocusedPartText("편성", "Garage", "런타임 데이터 대기", "garage");

                return focusedPart switch
                {
                    GarageEditorFocus.Firepower => new FocusedPartText(
                        "주무장",
                        editor.FirepowerValueText,
                        editor.FirepowerHintText,
                        "swords"),
                    GarageEditorFocus.Mobility => new FocusedPartText(
                        "기동",
                        editor.MobilityValueText,
                        editor.MobilityHintText,
                        "speed"),
                    _ => new FocusedPartText(
                        "프레임",
                        editor.FrameValueText,
                        editor.FrameHintText,
                        "security"),
                };
            }
        }
    }
}
