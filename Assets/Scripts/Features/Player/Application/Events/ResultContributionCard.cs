using Shared.Kernel;

namespace Features.Player.Application.Events
{
    public enum ResultContributionKind
    {
        KeepCoreAlive = 0,
        ClearPressure = 1,
        HoldPosition = 2,
        DeployUnits = 3
    }

    public readonly struct ResultContributionCard
    {
        public ResultContributionCard(
            ResultContributionKind kind,
            string title,
            string body,
            float primaryValue,
            DomainEntityId ownerId = default,
            DomainEntityId unitId = default,
            bool isTeamCard = true,
            string loadoutKey = null)
        {
            Kind = kind;
            Title = title ?? string.Empty;
            Body = body ?? string.Empty;
            PrimaryValue = primaryValue;
            OwnerId = ownerId;
            UnitId = unitId;
            IsTeamCard = isTeamCard;
            LoadoutKey = loadoutKey ?? string.Empty;
        }

        public ResultContributionKind Kind { get; }
        public string Title { get; }
        public string Body { get; }
        public float PrimaryValue { get; }
        public DomainEntityId OwnerId { get; }
        public DomainEntityId UnitId { get; }
        public bool IsTeamCard { get; }
        public string LoadoutKey { get; }
    }
}
