namespace Features.Combat.Domain
{
    public static class RelationshipRule
    {
        public static float GetDamageMultiplier(RelationshipType rel) => rel switch
        {
            RelationshipType.Self => 0f,
            RelationshipType.Ally => 0.5f,
            _ => 1f
        };
    }
}
