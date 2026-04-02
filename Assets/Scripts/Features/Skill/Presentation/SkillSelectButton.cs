using Shared.Attributes;
using UnityEngine;
using UnityEngine.UI;

namespace Features.Skill.Presentation
{
    public sealed class SkillSelectButton : MonoBehaviour
    {
        [Required, SerializeField] private Button button;
        [Required, SerializeField] private Image iconImage;
        [Required, SerializeField] private Text nameLabel;
        [Required, SerializeField] private GameObject selectedFrame;

        private bool _selected;

        public Button Button => button;
        public bool IsSelected => _selected;

        public void Setup(string displayName, Sprite icon)
        {
            nameLabel.text = displayName;
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
            SetSelected(false);
            gameObject.SetActive(true);
        }

        public void SetSelected(bool selected)
        {
            _selected = selected;
            selectedFrame.SetActive(selected);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
