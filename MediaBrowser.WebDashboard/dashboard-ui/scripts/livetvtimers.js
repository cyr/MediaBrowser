﻿(function ($, document) {

    function deleteTimer(page, id) {

        Dashboard.confirm(Globalize.translate('MessageConfirmRecordingCancellation'), Globalize.translate('HeaderConfirmRecordingCancellation'), function (result) {

            if (result) {

                Dashboard.showLoadingMsg();

                ApiClient.cancelLiveTvTimer(id).done(function () {

                    Dashboard.alert(Globalize.translate('MessageRecordingCancelled'));

                    reload(page);
                });
            }

        });
    }

    function renderTimers(page, timers) {

        var html = LiveTvHelpers.getTimersHtml(timers);

        var elem = $('#items', page).html(html);

        $('.btnDeleteTimer', elem).on('click', function () {

            var id = this.getAttribute('data-timerid');

            deleteTimer(page, id);
        });

        Dashboard.hideLoadingMsg();
    }

    function reload(page) {

        Dashboard.showLoadingMsg();

        ApiClient.getLiveTvTimers().done(function (result) {

            renderTimers(page, result.Items);
        });
    }

    window.LiveTvPage.renderTimersTab = function (page, tabContent) {

        if (LibraryBrowser.needsRefresh(tabContent)) {
            reload(tabContent);
        }
    };

})(jQuery, document);