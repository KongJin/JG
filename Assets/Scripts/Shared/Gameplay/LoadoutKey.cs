namespace Shared.Gameplay
{
    public static class LoadoutKey
    {
        public const string MissingPartToken = "-";

        public static string Build(string frameId, string firepowerId, string mobilityId)
        {
            return $"{NormalizePart(frameId)}|{NormalizePart(firepowerId)}|{NormalizePart(mobilityId)}";
        }

        public static string NormalizePart(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? MissingPartToken : value.Trim();
        }
    }
}
