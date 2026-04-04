namespace Features.Skill.Application.Events
{
    /// <summary>
    /// 뽑기 더미에 다음 카드가 있을 때 HUD 미리보기용. NextSkillId가 null이면 표시 숨김.
    /// </summary>
    public readonly struct DeckNextDrawPreviewEvent
    {
        public DeckNextDrawPreviewEvent(string nextSkillIdOrNull)
        {
            NextSkillId = nextSkillIdOrNull;
        }

        public string NextSkillId { get; }
    }
}
