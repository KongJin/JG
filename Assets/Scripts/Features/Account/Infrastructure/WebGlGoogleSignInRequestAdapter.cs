using System;
using System.Runtime.InteropServices;
using Features.Account.Application.Ports;

namespace Features.Account.Infrastructure
{
    public sealed class WebGlGoogleSignInRequestAdapter : IGoogleSignInRequestPort
    {
        private readonly string _clientId;

        public WebGlGoogleSignInRequestAdapter(string clientId)
        {
            _clientId = clientId;
        }

        public bool HasClientId => !string.IsNullOrWhiteSpace(_clientId);

#if UNITY_WEBGL && !UNITY_EDITOR
        public bool IsAvailable => true;

        [DllImport("__Internal")]
        private static extern void AccountGoogleSignIn_RequestIdToken(
            string clientId,
            string callbackObjectName,
            string successMethodName,
            string errorMethodName);
#else
        public bool IsAvailable => false;
#endif

        public void RequestIdToken(
            string callbackObjectName,
            string successMethodName,
            string errorMethodName)
        {
            if (!HasClientId)
                throw new InvalidOperationException("googleWebClientId is empty.");

            if (!IsAvailable)
                throw new PlatformNotSupportedException("Google sign-in is only available in WebGL builds.");

#if UNITY_WEBGL && !UNITY_EDITOR
            AccountGoogleSignIn_RequestIdToken(
                _clientId,
                callbackObjectName,
                successMethodName,
                errorMethodName);
#endif
        }
    }
}
