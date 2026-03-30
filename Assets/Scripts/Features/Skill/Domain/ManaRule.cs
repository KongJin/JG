using Shared.Kernel;

namespace Features.Skill.Domain
{
    public static class ManaRule
    {
        public static Result CanCast(float manaCost, float currentMana)
        {
            if (manaCost <= 0f)
                return Result.Success();

            if (currentMana < manaCost)
                return Result.Failure("Not enough mana.");

            return Result.Success();
        }
    }
}
