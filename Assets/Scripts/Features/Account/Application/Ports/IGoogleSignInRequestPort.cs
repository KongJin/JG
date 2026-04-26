namespace Features.Account.Application.Ports
{
    public interface IGoogleSignInRequestPort
    {
        bool IsAvailable { get; }
        bool HasClientId { get; }

        void RequestIdToken(
            string callbackObjectName,
            string successMethodName,
            string errorMethodName);
    }
}
