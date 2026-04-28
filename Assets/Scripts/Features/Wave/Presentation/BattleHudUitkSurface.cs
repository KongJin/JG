using Shared.Runtime;
using UnityEngine;
using UnityEngine.UIElements;

namespace Features.Wave.Presentation
{
    public sealed class BattleHudUitkSurface : MonoBehaviour
    {
        [SerializeField] private UIDocument _document;
        [SerializeField] private VisualTreeAsset _visualTree;
        [SerializeField] private StyleSheet _styleSheet;

        private Label _waveLabel;
        private Label _countdownLabel;
        private Label _statusLabel;
        private Label _energyLabel;
        private Label _coreHpLabel;
        private VisualElement _resultOverlay;
        private Label _resultLabel;
        private Label _resultStatsLabel;

        public void Initialize()
        {
            _document ??= ComponentAccess.Get<UIDocument>(gameObject);
            if (_document == null)
                return;

            var root = _document.rootVisualElement;
            root.Clear();
            if (_visualTree != null)
                _visualTree.CloneTree(root);

            if (_styleSheet != null && !root.styleSheets.Contains(_styleSheet))
                root.styleSheets.Add(_styleSheet);

            _waveLabel = root.Q<Label>("WaveLabel");
            _countdownLabel = root.Q<Label>("CountdownLabel");
            _statusLabel = root.Q<Label>("StatusLabel");
            _energyLabel = root.Q<Label>("EnergyLabel");
            _coreHpLabel = root.Q<Label>("CoreHpLabel");
            _resultOverlay = root.Q<VisualElement>("ResultOverlay");
            _resultLabel = root.Q<Label>("ResultLabel");
            _resultStatsLabel = root.Q<Label>("ResultStatsLabel");
            SetResultVisible(false);
        }

        public void RenderWave(WaveHudView waveHud)
        {
            if (waveHud == null)
                return;

            SetText(_waveLabel, waveHud.WaveText);
            SetText(_countdownLabel, waveHud.IsCountdownVisible ? waveHud.CountdownText : string.Empty);
            SetText(_statusLabel, waveHud.IsStatusVisible ? waveHud.StatusText : waveHud.FirstWaveDeckHintText);
        }

        public void RenderEnergy(string energyText)
        {
            SetText(_energyLabel, energyText);
        }

        public void RenderCoreHp(string coreHpText)
        {
            SetText(_coreHpLabel, coreHpText);
        }

        public void RenderResult(WaveEndView result)
        {
            if (result == null)
                return;

            SetResultVisible(result.IsPanelVisible);
            SetText(_resultLabel, result.ResultText);
            SetText(_resultStatsLabel, result.StatsText);
        }

        private void SetResultVisible(bool isVisible)
        {
            if (_resultOverlay != null)
                _resultOverlay.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private static void SetText(Label label, string text)
        {
            if (label != null)
                label.text = text ?? string.Empty;
        }
    }
}
