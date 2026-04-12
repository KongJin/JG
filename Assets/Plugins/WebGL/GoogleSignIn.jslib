var AccountGoogleSignInPlugin = {

    AccountGoogleSignIn_RequestIdToken: function (clientIdPtr, callbackObjectPtr, successMethodPtr, errorMethodPtr) {
        var clientId = UTF8ToString(clientIdPtr);
        var callbackObjectName = UTF8ToString(callbackObjectPtr);
        var successMethodName = UTF8ToString(successMethodPtr);
        var errorMethodName = UTF8ToString(errorMethodPtr);

        function sendUnityMessage(methodName, payload) {
            if (!window.unityInstance || !window.unityInstance.SendMessage) {
                console.error("[GoogleSignIn] Unity instance is not ready.");
                return;
            }

            window.unityInstance.SendMessage(callbackObjectName, methodName, payload || "");
        }

        if (!clientId) {
            sendUnityMessage(errorMethodName, "Google Web Client ID is empty.");
            return;
        }

        if (!window.google || !window.google.accounts || !window.google.accounts.id) {
            sendUnityMessage(errorMethodName, "Google Identity Services SDK is not loaded.");
            return;
        }

        var handled = false;

        function fail(message) {
            if (handled) return;
            handled = true;
            sendUnityMessage(errorMethodName, message);
        }

        google.accounts.id.initialize({
            client_id: clientId,
            callback: function (response) {
                if (handled) return;

                if (!response || !response.credential) {
                    fail("Google credential response is empty.");
                    return;
                }

                handled = true;
                sendUnityMessage(successMethodName, response.credential);
            }
        });

        google.accounts.id.prompt(function (notification) {
            if (handled || !notification) {
                return;
            }

            if (notification.isNotDisplayed && notification.isNotDisplayed()) {
                fail(notification.getNotDisplayedReason ? notification.getNotDisplayedReason() : "prompt_not_displayed");
                return;
            }

            if (notification.isSkippedMoment && notification.isSkippedMoment()) {
                fail(notification.getSkippedReason ? notification.getSkippedReason() : "prompt_skipped");
            }
        });
    }
};

mergeInto(LibraryManager.library, AccountGoogleSignInPlugin);
