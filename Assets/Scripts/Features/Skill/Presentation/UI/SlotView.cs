using UnityEngine;

namespace Features.Skill.Presentation
{
    public sealed class SlotView : MonoBehaviour
    {
        private System.Action _onClick;

        public string KeyLabel { get; private set; } = string.Empty;
        public Sprite SkillIcon { get; private set; }

        public void SetKeyLabel(string label)
        {
            KeyLabel = label ?? string.Empty;
        }

        public void SetSkill(Sprite skillIcon)
        {
            SkillIcon = skillIcon;
        }

        public void ClearSkill()
        {
            SkillIcon = null;
        }

        public void SetClickHandler(System.Action onClick)
        {
            _onClick = onClick;
        }

        public void Click()
        {
            _onClick?.Invoke();
        }
    }
}
