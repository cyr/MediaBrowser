﻿(function ($, window, document, FileReader) {

    var currentFile;

    function reloadUser(page) {

        var userId = getParameterByName("userId");

        Dashboard.showLoadingMsg();

        ApiClient.getUser(userId).done(function (user) {

            $('.username', page).html(user.Name);
            Events.trigger($('#uploadUserImage', page).val('')[0], 'change');

            Dashboard.setPageTitle(user.Name);

            var imageUrl;

            if (user.PrimaryImageTag) {

                imageUrl = ApiClient.getUserImageUrl(user.Id, {
                    height: 200,
                    tag: user.PrimaryImageTag,
                    type: "Primary"
                });

            } else {
                imageUrl = "css/images/logindefault.png";
            }

            $('#fldImage', page).show().html('').html("<img width='140px' src='" + imageUrl + "' />");


            if (user.ConnectLinkType == 'Guest') {

                $('.newImageForm', page).hide();
                $('#btnDeleteImage', page).hide();
                $('.connectMessage', page).show();
            }
            else if (user.PrimaryImageTag) {

                $('#btnDeleteImage', page).show();
                $('#headerUploadNewImage', page).show();
                $('.newImageForm', page).show();
                $('.connectMessage', page).hide();

            } else {
                $('.newImageForm', page).show();
                $('#btnDeleteImage', page).hide();
                $('#headerUploadNewImage', page).show();
                $('.connectMessage', page).hide();
            }

            Dashboard.hideLoadingMsg();
        });

    }

    function processImageChangeResult() {

        Dashboard.hideLoadingMsg();

        var page = $($.mobile.activePage)[0];

        reloadUser(page);
    }

    function onFileReaderError(evt) {

        Dashboard.hideLoadingMsg();

        switch (evt.target.error.code) {
            case evt.target.error.NOT_FOUND_ERR:
                Dashboard.showError(Globalize.translate('FileNotFound'));
                break;
            case evt.target.error.NOT_READABLE_ERR:
                Dashboard.showError(Globalize.translate('FileReadError'));
                break;
            case evt.target.error.ABORT_ERR:
                break; // noop
            default:
                Dashboard.showError(Globalize.translate('FileReadError'));
        };
    }

    function onFileReaderOnloadStart(evt) {

        $('#fldUpload', $.mobile.activePage).hide();
    }

    function onFileReaderAbort(evt) {

        Dashboard.hideLoadingMsg();
        Dashboard.showError(Globalize.translate('FileReadCancelled'));
    }

    function setFiles(page, files) {

        var file = files[0];

        if (!file || !file.type.match('image.*')) {
            $('#userImageOutput', page).html('');
            $('#fldUpload', page).hide();
            currentFile = null;
            return;
        }

        currentFile = file;

        var reader = new FileReader();

        reader.onerror = onFileReaderError;
        reader.onloadstart = onFileReaderOnloadStart;
        reader.onabort = onFileReaderAbort;

        // Closure to capture the file information.
        reader.onload = (function (theFile) {
            return function (e) {

                // Render thumbnail.
                var html = ['<img style="max-width:500px;max-height:200px;" src="', e.target.result, '" title="', escape(theFile.name), '"/>'].join('');

                $('#userImageOutput', page).html(html);
                $('#fldUpload', page).show();
            };
        })(file);

        // Read in the image file as a data URL.
        reader.readAsDataURL(file);
    }

    function onImageDrop(e) {

        e.preventDefault();

        setFiles($.mobile.activePage, e.originalEvent.dataTransfer.files);

        return false;
    }

    function onImageDragOver(e) {

        e.preventDefault();

        e.originalEvent.dataTransfer.dropEffect = 'Copy';

        return false;
    }

    function myProfilePage() {

        var self = this;

        self.onImageSubmit = function () {

            var file = currentFile;

            if (!file) {
                return false;
            }

            if (file.type != "image/png" && file.type != "image/jpeg" && file.type != "image/jpeg") {
                return false;
            }

            Dashboard.showLoadingMsg();

            var userId = getParameterByName("userId");

            ApiClient.uploadUserImage(userId, 'Primary', file).done(processImageChangeResult);

            return false;
        };

        self.onFileUploadChange = function (fileUpload) {

            setFiles($.mobile.activePage, fileUpload.files);
        };
    }

    window.MyProfilePage = new myProfilePage();

    $(document).on('pageinit', "#userImagePage", function () {

        var page = this;

        reloadUser(page);

        $("#userImageDropZone", page).on('dragover', onImageDragOver).on('drop', onImageDrop);

        $('#btnDeleteImage', page).on('click', function () {

            Dashboard.confirm(Globalize.translate('DeleteImageConfirmation'), Globalize.translate('DeleteImage'), function (result) {

                if (result) {

                    Dashboard.showLoadingMsg();

                    var userId = getParameterByName("userId");

                    ApiClient.deleteUserImage(userId, "primary").done(processImageChangeResult);
                }

            });
        });

        $('.newImageForm').off('submit', MyProfilePage.onImageSubmit).on('submit', MyProfilePage.onImageSubmit);

    });


})(jQuery, window, document, window.FileReader);

(function ($, document, window) {

    function loadUser(page) {

        var userid = getParameterByName("userId");

        ApiClient.getUser(userid).done(function (user) {

            Dashboard.setPageTitle(user.Name);

            if (user.ConnectLinkType == 'Guest') {
                $('.localAccessSection', page).hide();
                $('.passwordSection', page).hide();
            }
            else if (user.HasConfiguredPassword) {
                $('#btnResetPassword', page).show();
                $('#fldCurrentPassword', page).show();
                $('.localAccessSection', page).show();
                $('.passwordSection', page).show();
            } else {
                $('#btnResetPassword', page).hide();
                $('#fldCurrentPassword', page).hide();
                $('.localAccessSection', page).hide();
                $('.passwordSection', page).show();
            }

            if (user.HasConfiguredEasyPassword) {
                $('#txtEasyPassword', page).val('').attr('placeholder', '******');
                $('#btnResetEasyPassword', page).show();
            } else {
                $('#txtEasyPassword', page).val('').attr('placeholder', '');
                $('#btnResetEasyPassword', page).hide();
            }

            page.querySelector('.chkEnableLocalEasyPassword').checked = user.Configuration.EnableLocalPassword;
        });

        $('#txtCurrentPassword', page).val('');
        $('#txtNewPassword', page).val('');
        $('#txtNewPasswordConfirm', page).val('');
    }

    function saveEasyPassword(page) {

        var userId = getParameterByName("userId");

        var easyPassword = $('#txtEasyPassword', page).val();

        if (easyPassword) {

            ApiClient.updateEasyPassword(userId, easyPassword).done(function () {

                onEasyPasswordSaved(page, userId);

            });

        } else {
            onEasyPasswordSaved(page, userId);
        }
    }

    function onEasyPasswordSaved(page, userId) {

        ApiClient.getUser(userId).done(function (user) {

            user.Configuration.EnableLocalPassword = page.querySelector('.chkEnableLocalEasyPassword').checked;

            ApiClient.updateUserConfiguration(user.Id, user.Configuration).done(function () {

                Dashboard.hideLoadingMsg();

                Dashboard.alert(Globalize.translate('MessageSettingsSaved'));
                loadUser(page);
            });
        });
    }

    function savePassword(page) {

        var userId = getParameterByName("userId");

        var currentPassword = $('#txtCurrentPassword', page).val();
        var newPassword = $('#txtNewPassword', page).val();

        ApiClient.updateUserPassword(userId, currentPassword, newPassword).done(function () {

            Dashboard.hideLoadingMsg();

            Dashboard.alert(Globalize.translate('PasswordSaved'));
            loadUser(page);

        });

    }

    function updatePasswordPage() {

        var self = this;

        self.onSubmit = function () {

            var page = $($.mobile.activePage)[0];

            if ($('#txtNewPassword', page).val() != $('#txtNewPasswordConfirm', page).val()) {

                Dashboard.showError(Globalize.translate('PasswordMatchError'));
            } else {

                Dashboard.showLoadingMsg();
                savePassword(page);
            }


            // Disable default form submission
            return false;

        };

        self.onLocalAccessSubmit = function () {

            var page = $($.mobile.activePage)[0];

            Dashboard.showLoadingMsg();

            saveEasyPassword(page);

            // Disable default form submission
            return false;

        };

        self.resetPassword = function () {

            var msg = Globalize.translate('PasswordResetConfirmation');

            var page = $($.mobile.activePage)[0];

            Dashboard.confirm(msg, Globalize.translate('PasswordResetHeader'), function (result) {

                if (result) {
                    var userId = getParameterByName("userId");

                    Dashboard.showLoadingMsg();

                    ApiClient.resetUserPassword(userId).done(function () {

                        Dashboard.hideLoadingMsg();

                        Dashboard.alert({
                            message: Globalize.translate('PasswordResetComplete'),
                            title: Globalize.translate('PasswordResetHeader')
                        });

                        loadUser(page);

                    });
                }
            });

        };

        self.resetEasyPassword = function () {

            var msg = Globalize.translate('PinCodeResetConfirmation');

            var page = $($.mobile.activePage)[0];

            Dashboard.confirm(msg, Globalize.translate('HeaderPinCodeReset'), function (result) {

                if (result) {
                    var userId = getParameterByName("userId");

                    Dashboard.showLoadingMsg();

                    ApiClient.resetEasyPassword(userId).done(function () {

                        Dashboard.hideLoadingMsg();

                        Dashboard.alert({
                            message: Globalize.translate('PinCodeResetComplete'),
                            title: Globalize.translate('HeaderPinCodeReset')
                        });

                        loadUser(page);

                    });
                }
            });

        };
    }

    window.UpdatePasswordPage = new updatePasswordPage();

    $(document).on('pageinit', ".userPasswordPage", function () {

        var page = this;

        $('.updatePasswordForm').off('submit', UpdatePasswordPage.onSubmit).on('submit', UpdatePasswordPage.onSubmit);
        $('.localAccessForm').off('submit', UpdatePasswordPage.onLocalAccessSubmit).on('submit', UpdatePasswordPage.onLocalAccessSubmit);

    }).on('pageshow', ".userPasswordPage", function () {

        var page = this;

        loadUser(page);

    });

})(jQuery, document, window);