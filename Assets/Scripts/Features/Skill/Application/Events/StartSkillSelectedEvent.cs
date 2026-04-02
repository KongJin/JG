namespace Features.Skill.Application.Events
{
    public readonly struct StartSkillSelectedEvent
    {
        public string[] ChosenSkillIds { get; }

        public StartSkillSelectedEvent(string[] chosenSkillIds)
        {
            ChosenSkillIds = chosenSkillIds;
        }
    }
}
