using Shared.Kernel;

namespace Features.Skill.Domain
{
    public static class CooldownRule
    {
        public static Result CanCast(Skill skill, float currentTime, CooldownTracker tracker)
        {
            return CanCast(skill, currentTime, tracker, skill.Spec.Cooldown);
        }

        public static Result CanCast(Skill skill, float currentTime, CooldownTracker tracker, float effectiveCooldown)
        {
            var lastCastTime = tracker.GetLastCastTime(skill.Id);
            var elapsed = currentTime - lastCastTime;
            if (elapsed < effectiveCooldown)
            {
                return Result.Failure("Skill is on cooldown.");
            }

            return Result.Success();
        }
    }
}
