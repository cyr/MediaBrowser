﻿(function () {

    function load(page, device, capabilities) {

        if (capabilities.SupportsContentUploading) {
            $('#fldCameraUploadPath', page).show();
        } else {
            $('#fldCameraUploadPath', page).hide();
        }

        $('#txtCustomName', page).val(device.CustomName || '');
        $('#txtUploadPath', page).val(device.CameraUploadPath || '');
        $('.reportedName', page).html(device.ReportedName || '');
    }

    function loadData(page) {

        Dashboard.showLoadingMsg();

        var id = getParameterByName('id');

        var promise1 = ApiClient.getJSON(ApiClient.getUrl('Devices/Info', { Id: id }));
        var promise2 = ApiClient.getJSON(ApiClient.getUrl('Devices/Capabilities', { Id: id }));

        $.when(promise1, promise2).done(function (response1, response2) {

            load(page, response1[0], response2[0]);

            Dashboard.hideLoadingMsg();
        });
    }

    function save(page) {

        var id = getParameterByName('id');

        ApiClient.ajax({

            url: ApiClient.getUrl('Devices/Options', { Id: id }),
            type: 'POST',
            data: JSON.stringify({

                CustomName: $('#txtCustomName', page).val(),
                CameraUploadPath: $('#txtUploadPath', page).val()

            }),
            contentType: "application/json"

        }).done(Dashboard.processServerConfigurationUpdateResult);
    }

    function onSubmit() {
        var form = this;
        var page = $(form).parents('.page');

        save(page);

        return false;
    }

    $(document).on('pageinit', "#devicePage", function () {

        var page = this;

        $('#btnSelectUploadPath', page).on("click.selectDirectory", function () {

            require(['directorybrowser'], function (directoryBrowser) {

                var picker = new directoryBrowser();

                picker.show({

                    callback: function (path) {

                        if (path) {
                            $('#txtUploadPath', page).val(path);
                        }
                        picker.close();
                    },

                    header: Globalize.translate('HeaderSelectUploadPath')
                });
            });
        });

        $('.deviceForm').off('submit', onSubmit).on('submit', onSubmit);

    }).on('pageshow', "#devicePage", function () {

        var page = this;

        loadData(page);
    });

})();