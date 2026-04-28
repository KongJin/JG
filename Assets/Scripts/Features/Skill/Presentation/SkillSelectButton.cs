using UnityEngine;

namespace Features.Skill.Presentation
{
    public sealed class SkillSelectButton : MonoBehaviour
    {
        private bool _selected;

        public bool IsSelected => _selected;
        public string DisplayName { get; private set; } = string.Empty;
        public Sprite Icon { get; private set; }
        public bool IsVisible { get; private set; }

        public void Setup(string displayName, Sprite icon)
        {
            DisplayName = displayName ?? string.Empty;
            Icon = icon;
            SetSelected(false);
            IsVisible = true;
        }

        public void SetSelected(bool selected)
        {
            _selected = selected;
        }

        public void Hide()
        {
            IsVisible = false;
        }
    }
}
