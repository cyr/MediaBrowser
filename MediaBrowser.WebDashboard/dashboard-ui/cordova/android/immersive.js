﻿(function () {

    function onSuccess() {
        Logger.log('Immersive mode succeeded');
    }

    function onError() {
        Logger.log('Immersive mode failed');
    }

    //// Is this plugin supported?
    //AndroidFullScreen.isSupported();

    //// Is immersive mode supported?
    //AndroidFullScreen.isImmersiveModeSupported(onSuccess, onError);

    //// The width of the screen in immersive mode
    //AndroidFullScreen.immersiveWidth(trace, onError);

    //// The height of the screen in immersive mode
    //AndroidFullScreen.immersiveHeight(trace, onError);

    //// Hide system UI until user interacts
    //AndroidFullScreen.leanMode(onSuccess, onError);

    //// Show system UI
    //AndroidFullScreen.showSystemUI(onSuccess, onError);

    //// Extend your app underneath the system UI (Android 4.4+ only)
    //AndroidFullScreen.showUnderSystemUI(onSuccess, onError);

    //// Hide system UI and keep it hidden (Android 4.4+ only)
    //AndroidFullScreen.immersiveMode(onSuccess, onError);

    function updateFromSetting(leaveFullScreen) {

        if (AppSettings.enableFullScreen()) {
            AndroidFullScreen.immersiveMode(onSuccess, onError);
        }
        else if (leaveFullScreen) {
            AndroidFullScreen.showSystemUI(onSuccess, onError);
        }
    }

    Dashboard.ready(function () {

        Logger.log('binding fullscreen to MediaController');

        updateFromSetting(false);

        $(AppSettings).on('settingupdated', function (e, key) {

            if (key == 'enableFullScreen') {
                updateFromSetting(true);
            }
        });
    });

})();