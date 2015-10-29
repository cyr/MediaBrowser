﻿(function () {

    function load(page, config, allCultures, allCountries) {
        if (!config || !allCultures || !allCountries) {
            return;
        }

        $('#chkEnableInternetProviders', page).checked(config.EnableInternetProviders).checkboxradio("refresh");
        $('#chkSaveLocal', page).checked(config.SaveLocalMeta).checkboxradio("refresh");
        $('#selectLanguage', page).val(config.PreferredMetadataLanguage);
        $('#selectCountry', page).val(config.MetadataCountryCode);

        $('#selectImageSavingConvention', page).val(config.ImageSavingConvention);

        Dashboard.hideLoadingMsg();
    }

    function onSubmit() {
        var form = this;

        Dashboard.showLoadingMsg();

        ApiClient.getServerConfiguration().done(function (config) {

            config.ImageSavingConvention = $('#selectImageSavingConvention', form).val();

            config.EnableInternetProviders = $('#chkEnableInternetProviders', form).checked();
            config.SaveLocalMeta = $('#chkSaveLocal', form).checked();
            config.PreferredMetadataLanguage = $('#selectLanguage', form).val();
            config.MetadataCountryCode = $('#selectCountry', form).val();

            ApiClient.updateServerConfiguration(config).done(Dashboard.processServerConfigurationUpdateResult);
        });

        // Disable default form submission
        return false;
    }

    $(document).on('pageinit', "#metadataConfigurationPage", function () {

        Dashboard.showLoadingMsg();

        $('.metadataConfigurationForm').off('submit', onSubmit).on('submit', onSubmit);

    }).on('pageshow', "#metadataConfigurationPage", function () {

        Dashboard.showLoadingMsg();

        var page = this;

        var config;
        var allCultures;
        var allCountries;

        ApiClient.getServerConfiguration().done(function (result) {

            config = result;
            load(page, config, allCultures, allCountries);
        });

        ApiClient.getCultures().done(function (result) {

            Dashboard.populateLanguages($('#selectLanguage', page), result);

            allCultures = result;
            load(page, config, allCultures, allCountries);
        });

        ApiClient.getCountries().done(function (result) {

            Dashboard.populateCountries($('#selectCountry', page), result);

            allCountries = result;
            load(page, config, allCultures, allCountries);
        });
    });

})();