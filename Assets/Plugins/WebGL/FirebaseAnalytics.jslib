var FirebaseAnalyticsPlugin = {

    FirebaseAnalytics_Init: function () {
        if (window._firebaseInitialized) return;

        var config = {
            apiKey: "AIzaSyDh9nMMyymwChqBgeeBPonlU3vt-jw8syY",
            authDomain: "projectsd-51439.firebaseapp.com",
            projectId: "projectsd-51439",
            storageBucket: "projectsd-51439.firebasestorage.app",
            messagingSenderId: "375816907073",
            appId: "1:375816907073:web:b8537d55451a4459db59c0",
            measurementId: "G-5QHE4HB7BL"
        };

        var isQa = window.location.hostname.indexOf("--") !== -1;
        if (isQa) {
            window.dataLayer = window.dataLayer || [];
            window.gtag = window.gtag || function(){ window.dataLayer.push(arguments); };
            window.gtag('set', { debug_mode: true });
        }

        firebase.initializeApp(config);
        window._firebaseAnalytics = firebase.analytics();
        window._firebaseIsQa = isQa;
        window._firebaseInitialized = true;
    },

    FirebaseAnalytics_LogEvent: function (eventNamePtr, jsonParamsPtr) {
        if (!window._firebaseAnalytics) return;

        var eventName = UTF8ToString(eventNamePtr);
        var jsonStr = UTF8ToString(jsonParamsPtr);
        var params = jsonStr ? JSON.parse(jsonStr) : {};

        if (window._firebaseIsQa) {
            console.log('[Analytics]', eventName, params);
        }
        window._firebaseAnalytics.logEvent(eventName, params);
    }
};

mergeInto(LibraryManager.library, FirebaseAnalyticsPlugin);
