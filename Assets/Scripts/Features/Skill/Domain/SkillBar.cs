namespace Features.Skill.Domain
{
    public sealed class SkillBar
    {
        public const int SlotCount = 2;

        private readonly global::Features.Skill.Domain.Skill[] _slots = new global::Features.Skill.Domain.Skill[SlotCount];

        public bool Equip(int slotIndex, global::Features.Skill.Domain.Skill skill)
        {
            if (slotIndex < 0 || slotIndex >= SlotCount) return false;
            _slots[slotIndex] = skill;
            return true;
        }

        public global::Features.Skill.Domain.Skill GetSkill(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= SlotCount) return null;
            return _slots[slotIndex];
        }
    }
}

