﻿(function () {

    function showPlayerSelectionMenu(item, url, mimeType) {

        window.plugins.launcher.launch({
            uri: url,
            dataType: mimeType

        }, function () {

            Logger.log('plugin launch success');
            ExternalPlayer.onPlaybackStart();

        }, function () {

            Logger.log('plugin launch error');
            ExternalPlayer.onPlaybackStart();
        });
    }

    window.ExternalPlayer.showPlayerSelectionMenu = showPlayerSelectionMenu;

})();