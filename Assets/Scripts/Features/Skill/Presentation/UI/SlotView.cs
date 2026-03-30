using Shared.Attributes;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Features.Skill.Presentation
{
    public sealed class SlotView : MonoBehaviour, IPointerClickHandler
    {
        [Required, SerializeField] private Image icon;
        [Required, SerializeField] private Text keyLabel;

        private System.Action _onClick;

        public void SetKeyLabel(string label)
        {
            if (keyLabel != null) keyLabel.text = label;
        }

        public void SetSkill(Sprite skillIcon)
        {
            if (icon != null)
            {
                icon.sprite = skillIcon;
                icon.enabled = skillIcon != null;
            }
        }

        public void ClearSkill()
        {
            if (icon != null)
            {
                icon.sprite = null;
                icon.enabled = false;
            }
        }

        public void SetClickHandler(System.Action onClick)
        {
            _onClick = onClick;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData != null && eventData.button != PointerEventData.InputButton.Left)
                return;

            _onClick?.Invoke();
        }
    }
}
